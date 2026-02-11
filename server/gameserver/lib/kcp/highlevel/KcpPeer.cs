// Kcp Peer, similar to UDP Peer but wrapped with reliability, channels,
// timeouts, authentication, state, etc.
//
// still IO agnostic to work with udp, nonalloc, relays, native, etc.
using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace kcp2k
{
    public abstract class KcpPeer
    {
        // kcp reliability algorithm
        internal Kcp kcp;

        // security cookie to prevent UDP spoofing.
        internal uint cookie;

        // state: connected as soon as we create the peer.
        protected KcpState state = KcpState.Connected;

        // If we don't receive anything these many milliseconds
        // then consider us disconnected
        public const int DEFAULT_TIMEOUT = 10000;
        public int timeout;
        uint lastReceiveTime;

        // internal time.
        readonly Stopwatch watch = new Stopwatch();

        // current time property for convenience.
        public uint time => (uint)watch.ElapsedMilliseconds;

        // buffer to receive kcp's processed messages (avoids allocations).
        readonly byte[] kcpMessageBuffer;

        // send buffer for handing user messages to kcp for processing.
        readonly byte[] kcpSendBuffer;

        // raw send buffer is exactly MTU.
        readonly byte[] rawSendBuffer;

        // send a ping occasionally so we don't time out on the other end.
        public const int PING_INTERVAL = 1000;
        uint lastPingTime;
        uint lastPongTime;

        // if we send more than kcp can handle, we will get ever growing
        // send/recv buffers and queues and minutes of latency.
        internal const int QueueDisconnectThreshold = 10000;

        // getters for queue and buffer counts, used for debug info
        public int SendQueueCount     => kcp.snd_queue.Count;
        public int ReceiveQueueCount  => kcp.rcv_queue.Count;
        public int SendBufferCount    => kcp.snd_buf.Count;
        public int ReceiveBufferCount => kcp.rcv_buf.Count;

        // we need to subtract the channel and cookie bytes from every
        // MaxMessageSize calculation.
        public const int CHANNEL_HEADER_SIZE = 1;
        public const int COOKIE_HEADER_SIZE = 4;
        public const int METADATA_SIZE_RELIABLE = CHANNEL_HEADER_SIZE + COOKIE_HEADER_SIZE;
        public const int METADATA_SIZE_UNRELIABLE = CHANNEL_HEADER_SIZE + COOKIE_HEADER_SIZE;

        // reliable channel (= kcp) MaxMessageSize
        static int ReliableMaxMessageSize_Unconstrained(int mtu, uint rcv_wnd) =>
            (mtu - Kcp.OVERHEAD - METADATA_SIZE_RELIABLE) * ((int)rcv_wnd - 1) - 1;

        public static int ReliableMaxMessageSize(int mtu, uint rcv_wnd) =>
            ReliableMaxMessageSize_Unconstrained(mtu, Math.Min(rcv_wnd, Kcp.FRG_MAX));

        // unreliable max message size is simply MTU - channel header - kcp header
        public static int UnreliableMaxMessageSize(int mtu) =>
            mtu - METADATA_SIZE_UNRELIABLE - 1;

        // maximum send rate per second
        public uint MaxSendRate    => kcp.snd_wnd * kcp.mtu * 1000 / kcp.interval;
        public uint MaxReceiveRate => kcp.rcv_wnd * kcp.mtu * 1000 / kcp.interval;

        // calculate max message sizes based on mtu and wnd only once
        public readonly int unreliableMax;
        public readonly int reliableMax;

        // round trip time (RTT) for convenience.
        public uint rttInMilliseconds { get; private set; }

        protected KcpPeer(KcpConfig config, uint cookie)
        {
            Reset(config);
            this.cookie = cookie;
            Log.Info($"[KCP] {GetType()}: created with cookie={cookie}");

            rawSendBuffer = new byte[config.Mtu];

            unreliableMax = UnreliableMaxMessageSize(config.Mtu);
            reliableMax = ReliableMaxMessageSize(config.Mtu, config.ReceiveWindowSize);

            kcpMessageBuffer = new byte[1 + reliableMax];
            kcpSendBuffer    = new byte[1 + reliableMax];
        }

        // Reset all state once.
        protected void Reset(KcpConfig config)
        {
            cookie = 0;
            state = KcpState.Connected;
            lastReceiveTime = 0;
            lastPingTime = 0;
            watch.Restart();

            kcp = new Kcp(0, RawSendReliable);

            kcp.SetNoDelay(config.NoDelay ? 1u : 0u, config.Interval, config.FastResend, !config.CongestionWindow);
            kcp.SetWindowSize(config.SendWindowSize, config.ReceiveWindowSize);
            kcp.SetMtu((uint)config.Mtu - METADATA_SIZE_RELIABLE);
            kcp.dead_link = config.MaxRetransmits;
            timeout = config.Timeout;
        }

        // callbacks ///////////////////////////////////////////////////////////
        protected abstract void OnAuthenticated();
        protected abstract void OnData(ArraySegment<byte> message, KcpChannel channel);
        protected abstract void OnDisconnected();
        protected abstract void OnError(ErrorCode error, string message);
        protected abstract void RawSend(ArraySegment<byte> data);
        ////////////////////////////////////////////////////////////////////////

        void HandleTimeout(uint time)
        {
            if (time >= lastReceiveTime + timeout)
            {
                OnError(ErrorCode.Timeout, $"{GetType()}: Connection timed out after not receiving any message for {timeout}ms. Disconnecting.");
                Disconnect();
            }
        }

        void HandleDeadLink()
        {
            if (kcp.state == -1)
            {
                OnError(ErrorCode.Timeout, $"{GetType()}: dead_link detected: a message was retransmitted {kcp.dead_link} times without ack. Disconnecting.");
                Disconnect();
            }
        }

        void HandlePing(uint time)
        {
            if (time >= lastPingTime + PING_INTERVAL)
            {
                SendPing();
                lastPingTime = time;
            }
        }

        void HandleChoked()
        {
            int total = kcp.rcv_queue.Count + kcp.snd_queue.Count +
                        kcp.rcv_buf.Count   + kcp.snd_buf.Count;
            if (total >= QueueDisconnectThreshold)
            {
                OnError(ErrorCode.Congestion,
                        $"{GetType()}: disconnecting connection because it can't process data fast enough.\n" +
                        $"Queue total {total}>{QueueDisconnectThreshold}. rcv_queue={kcp.rcv_queue.Count} snd_queue={kcp.snd_queue.Count} rcv_buf={kcp.rcv_buf.Count} snd_buf={kcp.snd_buf.Count}\n" +
                        $"* Try to Enable NoDelay, decrease INTERVAL, disable Congestion Window (= enable NOCWND!), increase SEND/RECV WINDOW or compress data.\n" +
                        $"* Or perhaps the network is simply too slow on our end, or on the other end.");

                kcp.snd_queue.Clear();
                Disconnect();
            }
        }

        bool ReceiveNextReliable(out KcpHeaderReliable header, out ArraySegment<byte> message)
        {
            message = default;
            header = KcpHeaderReliable.Ping;

            int msgSize = kcp.PeekSize();
            if (msgSize <= 0) return false;

            if (msgSize > kcpMessageBuffer.Length)
            {
                OnError(ErrorCode.InvalidReceive, $"{GetType()}: possible allocation attack for msgSize {msgSize} > buffer {kcpMessageBuffer.Length}. Disconnecting the connection.");
                Disconnect();
                return false;
            }

            int received = kcp.Receive(kcpMessageBuffer, msgSize);
            if (received < 0)
            {
                OnError(ErrorCode.InvalidReceive, $"{GetType()}: Receive failed with error={received}. closing connection.");
                Disconnect();
                return false;
            }

            byte headerByte = kcpMessageBuffer[0];
            if (!KcpHeader.ParseReliable(headerByte, out header))
            {
                OnError(ErrorCode.InvalidReceive, $"{GetType()}: Receive failed to parse header: {headerByte} is not defined in {typeof(KcpHeaderReliable)}.");
                Disconnect();
                return false;
            }

            message = new ArraySegment<byte>(kcpMessageBuffer, 1, msgSize - 1);
            lastReceiveTime = time;
            return true;
        }

        void TickIncoming_Connected(uint time)
        {
            HandleTimeout(time);
            HandleDeadLink();
            HandlePing(time);
            HandleChoked();

            if (ReceiveNextReliable(out KcpHeaderReliable header, out ArraySegment<byte> message))
            {
                switch (header)
                {
                    case KcpHeaderReliable.Hello:
                    {
                        Log.Info($"[KCP] {GetType()}: received hello with cookie={cookie}");
                        state = KcpState.Authenticated;
                        OnAuthenticated();
                        break;
                    }
                    case KcpHeaderReliable.Ping:
                    case KcpHeaderReliable.Pong:
                    {
                        break;
                    }
                    case KcpHeaderReliable.Data:
                    {
                        OnError(ErrorCode.InvalidReceive, $"[KCP] {GetType()}: received invalid header {header} while Connected. Disconnecting the connection.");
                        Disconnect();
                        break;
                    }
                }
            }
        }

        void TickIncoming_Authenticated(uint time)
        {
            HandleTimeout(time);
            HandleDeadLink();
            HandlePing(time);
            HandleChoked();

            while (ReceiveNextReliable(out KcpHeaderReliable header, out ArraySegment<byte> message))
            {
                switch (header)
                {
                    case KcpHeaderReliable.Hello:
                    {
                        Log.Warning($"{GetType()}: received invalid header {header} while Authenticated. Disconnecting the connection.");
                        Disconnect();
                        break;
                    }
                    case KcpHeaderReliable.Data:
                    {
                        if (message.Count > 0)
                        {
                            OnData(message, KcpChannel.Reliable);
                        }
                        else
                        {
                            OnError(ErrorCode.InvalidReceive, $"{GetType()}: received empty Data message while Authenticated. Disconnecting the connection.");
                            Disconnect();
                        }
                        break;
                    }
                    case KcpHeaderReliable.Ping:
                    {
                        if (message.Count == 4)
                        {
                            if (time >= lastPongTime + PING_INTERVAL)
                            {
                                Utils.Decode32U(message.Array, message.Offset, out uint pingTimestamp);
                                SendPong(pingTimestamp);
                                lastPongTime = time;
                            }
                        }
                        break;
                    }
                    case KcpHeaderReliable.Pong:
                    {
                        if (message.Count == 4)
                        {
                            Utils.Decode32U(message.Array, message.Offset, out uint originalTimestamp);
                            if (time >= originalTimestamp)
                            {
                                rttInMilliseconds = time - originalTimestamp;
                            }
                        }
                        break;
                    }
                }
            }
        }

        public virtual void TickIncoming()
        {
            try
            {
                switch (state)
                {
                    case KcpState.Connected:
                    {
                        TickIncoming_Connected(time);
                        break;
                    }
                    case KcpState.Authenticated:
                    {
                        TickIncoming_Authenticated(time);
                        break;
                    }
                    case KcpState.Disconnected:
                    {
                        break;
                    }
                }
            }
            catch (SocketException exception)
            {
                OnError(ErrorCode.ConnectionClosed, $"{GetType()}: Disconnecting because {exception}. This is fine.");
                Disconnect();
            }
            catch (ObjectDisposedException exception)
            {
                OnError(ErrorCode.ConnectionClosed, $"{GetType()}: Disconnecting because {exception}. This is fine.");
                Disconnect();
            }
            catch (Exception exception)
            {
                OnError(ErrorCode.Unexpected, $"{GetType()}: unexpected Exception: {exception}");
                Disconnect();
            }
        }

        public virtual void TickOutgoing()
        {
            try
            {
                switch (state)
                {
                    case KcpState.Connected:
                    case KcpState.Authenticated:
                    {
                        kcp.Update(time);
                        break;
                    }
                    case KcpState.Disconnected:
                    {
                        break;
                    }
                }
            }
            catch (SocketException exception)
            {
                OnError(ErrorCode.ConnectionClosed, $"{GetType()}: Disconnecting because {exception}. This is fine.");
                Disconnect();
            }
            catch (ObjectDisposedException exception)
            {
                OnError(ErrorCode.ConnectionClosed, $"{GetType()}: Disconnecting because {exception}. This is fine.");
                Disconnect();
            }
            catch (Exception exception)
            {
                OnError(ErrorCode.Unexpected, $"{GetType()}: unexpected exception: {exception}");
                Disconnect();
            }
        }

        protected void OnRawInputReliable(ArraySegment<byte> message)
        {
            int input = kcp.Input(message.Array, message.Offset, message.Count);
            if (input != 0)
            {
                Log.Warning($"[KCP] {GetType()}: Input failed with error={input} for buffer with length={message.Count - 1}");
            }
        }

        protected void OnRawInputUnreliable(ArraySegment<byte> message)
        {
            if (message.Count < 1) return;

            byte headerByte = message.Array[message.Offset + 0];
            if (!KcpHeader.ParseUnreliable(headerByte, out KcpHeaderUnreliable header))
            {
                OnError(ErrorCode.InvalidReceive, $"{GetType()}: Receive failed to parse header: {headerByte} is not defined in {typeof(KcpHeaderUnreliable)}.");
                Disconnect();
                return;
            }

            message = new ArraySegment<byte>(message.Array, message.Offset + 1, message.Count - 1);

            switch (header)
            {
                case KcpHeaderUnreliable.Data:
                {
                    if (state == KcpState.Authenticated)
                    {
                        OnData(message, KcpChannel.Unreliable);
                        lastReceiveTime = time;
                    }
                    break;
                }
                case KcpHeaderUnreliable.Disconnect:
                {
                    Log.Info($"[KCP] {GetType()}: received disconnect message");
                    Disconnect();
                    break;
                }
            }
        }

        // raw send called by kcp
        void RawSendReliable(byte[] data, int length)
        {
            rawSendBuffer[0] = (byte)KcpChannel.Reliable;
            Utils.Encode32U(rawSendBuffer, 1, cookie);
            Buffer.BlockCopy(data, 0, rawSendBuffer, 1+4, length);

            ArraySegment<byte> segment = new ArraySegment<byte>(rawSendBuffer, 0, length + 1+4);
            RawSend(segment);
        }

        void SendReliable(KcpHeaderReliable header, ArraySegment<byte> content)
        {
            if (1 + content.Count > kcpSendBuffer.Length)
            {
                OnError(ErrorCode.InvalidSend, $"{GetType()}: Failed to send reliable message of size {content.Count} because it's larger than ReliableMaxMessageSize={reliableMax}");
                return;
            }

            kcpSendBuffer[0] = (byte)header;

            if (content.Count > 0)
                Buffer.BlockCopy(content.Array, content.Offset, kcpSendBuffer, 1, content.Count);

            int sent = kcp.Send(kcpSendBuffer, 0, 1 + content.Count);
            if (sent < 0)
            {
                OnError(ErrorCode.InvalidSend, $"{GetType()}: Send failed with error={sent} for content with length={content.Count}");
            }
        }

        void SendUnreliable(KcpHeaderUnreliable header, ArraySegment<byte> content)
        {
            if (content.Count > unreliableMax)
            {
                Log.Error($"[KCP] {GetType()}: Failed to send unreliable message of size {content.Count} because it's larger than UnreliableMaxMessageSize={unreliableMax}");
                return;
            }

            rawSendBuffer[0] = (byte)KcpChannel.Unreliable;
            Utils.Encode32U(rawSendBuffer, 1, cookie);
            rawSendBuffer[5] = (byte)header;

            if (content.Count > 0)
                Buffer.BlockCopy(content.Array, content.Offset, rawSendBuffer, 1 + 4 + 1, content.Count);

            ArraySegment<byte> segment = new ArraySegment<byte>(rawSendBuffer, 0, content.Count + 1 + 4 + 1);
            RawSend(segment);
        }

        public void SendHello()
        {
            Log.Info($"[KCP] {GetType()}: sending handshake to other end with cookie={cookie}");
            SendReliable(KcpHeaderReliable.Hello, default);
        }

        public void SendData(ArraySegment<byte> data, KcpChannel channel)
        {
            if (data.Count == 0)
            {
                OnError(ErrorCode.InvalidSend, $"{GetType()}: tried sending empty message. This should never happen. Disconnecting.");
                Disconnect();
                return;
            }

            switch (channel)
            {
                case KcpChannel.Reliable:
                    SendReliable(KcpHeaderReliable.Data, data);
                    break;
                case KcpChannel.Unreliable:
                    SendUnreliable(KcpHeaderUnreliable.Data, data);
                    break;
            }
        }

        readonly byte[] pingData = new byte[4];
        void SendPing()
        {
            Utils.Encode32U(pingData, 0, time);
            SendReliable(KcpHeaderReliable.Ping, pingData);
        }

        void SendPong(uint pingTimestamp)
        {
            Utils.Encode32U(pingData, 0, pingTimestamp);
            SendReliable(KcpHeaderReliable.Pong, pingData);
        }

        void SendDisconnect()
        {
            for (int i = 0; i < 5; ++i)
                SendUnreliable(KcpHeaderUnreliable.Disconnect, default);
        }

        public virtual void Disconnect()
        {
            if (state == KcpState.Disconnected)
                return;

            try
            {
                SendDisconnect();
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }

            Log.Info($"[KCP] {GetType()}: Disconnected.");
            state = KcpState.Disconnected;
            OnDisconnected();
        }
    }
}
