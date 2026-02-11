// kcp server logic abstracted into a class.
// for use in Mirror, DOTSNET, testing, etc.
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace kcp2k
{
    public class KcpServer
    {
        // callbacks
        protected readonly Action<int> OnConnected;
        protected readonly Action<int, ArraySegment<byte>, KcpChannel> OnData;
        protected readonly Action<int> OnDisconnected;
        protected readonly Action<int, ErrorCode, string> OnError;

        // configuration
        protected readonly KcpConfig config;

        // state
        protected Socket socket;
        EndPoint newClientEP;

        // expose local endpoint for users / relays / nat traversal etc.
        public EndPoint LocalEndPoint => socket?.LocalEndPoint;

        // raw receive buffer always needs to be of 'MTU' size
        protected readonly byte[] rawReceiveBuffer;

        // connections <connectionId, connection>
        public Dictionary<int, KcpServerConnection> connections =
            new Dictionary<int, KcpServerConnection>();

        public KcpServer(Action<int> OnConnected,
                         Action<int, ArraySegment<byte>, KcpChannel> OnData,
                         Action<int> OnDisconnected,
                         Action<int, ErrorCode, string> OnError,
                         KcpConfig config)
        {
            this.OnConnected = OnConnected;
            this.OnData = OnData;
            this.OnDisconnected = OnDisconnected;
            this.OnError = OnError;
            this.config = config;

            rawReceiveBuffer = new byte[config.Mtu];

            newClientEP = config.DualMode
                          ? new IPEndPoint(IPAddress.IPv6Any, 0)
                          : new IPEndPoint(IPAddress.Any,     0);
        }

        public virtual bool IsActive() => socket != null;

        static Socket CreateServerSocket(bool DualMode, ushort port)
        {
            if (DualMode)
            {
                Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);

                try
                {
                    socket.DualMode = true;
                }
                catch (NotSupportedException e)
                {
                    Log.Warning($"[KCP] Failed to set Dual Mode, continuing with IPv6 without Dual Mode. Error: {e}");
                }

                // Windows: disable SIO_UDP_CONNRESET to prevent socket exceptions
                // when clients disconnect
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    const uint IOC_IN = 0x80000000U;
                    const uint IOC_VENDOR = 0x18000000U;
                    const int SIO_UDP_CONNRESET = unchecked((int)(IOC_IN | IOC_VENDOR | 12));
                    socket.IOControl(SIO_UDP_CONNRESET, new byte[] { 0x00 }, null);
                }

                socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
                return socket;
            }
            else
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // Windows: disable SIO_UDP_CONNRESET
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    const uint IOC_IN = 0x80000000U;
                    const uint IOC_VENDOR = 0x18000000U;
                    const int SIO_UDP_CONNRESET = unchecked((int)(IOC_IN | IOC_VENDOR | 12));
                    socket.IOControl(SIO_UDP_CONNRESET, new byte[] { 0x00 }, null);
                }

                socket.Bind(new IPEndPoint(IPAddress.Any, port));
                return socket;
            }
        }

        public virtual void Start(ushort port)
        {
            if (socket != null)
            {
                Log.Warning("[KCP] Server: already started!");
                return;
            }

            socket = CreateServerSocket(config.DualMode, port);
            socket.Blocking = false;
            Common.ConfigureSocketBuffers(socket, config.RecvBufferSize, config.SendBufferSize);
        }

        public void Send(int connectionId, ArraySegment<byte> segment, KcpChannel channel)
        {
            if (connections.TryGetValue(connectionId, out KcpServerConnection connection))
            {
                connection.SendData(segment, channel);
            }
        }

        public void Disconnect(int connectionId)
        {
            if (connections.TryGetValue(connectionId, out KcpServerConnection connection))
            {
                connection.Disconnect();
            }
        }

        public IPEndPoint GetClientEndPoint(int connectionId)
        {
            if (connections.TryGetValue(connectionId, out KcpServerConnection connection))
            {
                return connection.remoteEndPoint as IPEndPoint;
            }
            return null;
        }

        // io - input.
        protected virtual bool RawReceiveFrom(out ArraySegment<byte> segment, out int connectionId)
        {
            segment = default;
            connectionId = 0;
            if (socket == null) return false;

            try
            {
                if (socket.ReceiveFromNonBlocking(rawReceiveBuffer, out segment, ref newClientEP))
                {
                    connectionId = Common.ConnectionHash(newClientEP);
                    return true;
                }
            }
            catch (SocketException e)
            {
                Log.Info($"[KCP] Server: ReceiveFrom failed: {e}");
            }

            return false;
        }

        // io - out.
        protected virtual void RawSend(int connectionId, ArraySegment<byte> data)
        {
            if (!connections.TryGetValue(connectionId, out KcpServerConnection connection))
            {
                Log.Warning($"[KCP] Server: RawSend invalid connectionId={connectionId}");
                return;
            }

            try
            {
                socket.SendToNonBlocking(data, connection.remoteEndPoint);
            }
            catch (SocketException e)
            {
                Log.Error($"[KCP] Server: SendTo failed: {e}");
            }
        }

        protected virtual KcpServerConnection CreateConnection(int connectionId)
        {
            uint cookie = Common.GenerateCookie();

            KcpServerConnection connection = new KcpServerConnection(
                OnConnectedCallback,
                (message,  channel) => OnData(connectionId, message, channel),
                OnDisconnectedCallback,
                (error, reason) => OnError(connectionId, error, reason),
                (data) => RawSend(connectionId, data),
                config,
                cookie,
                newClientEP);

            return connection;

            void OnConnectedCallback(KcpServerConnection conn)
            {
                connections.Add(connectionId, conn);
                Log.Info($"[KCP] Server: added connection({connectionId})");
                Log.Info($"[KCP] Server: OnConnected({connectionId})");
                OnConnected(connectionId);
            }

            void OnDisconnectedCallback()
            {
                connectionsToRemove.Add(connectionId);
                Log.Info($"[KCP] Server: OnDisconnected({connectionId})");
                OnDisconnected(connectionId);
            }
        }

        void ProcessMessage(ArraySegment<byte> segment, int connectionId)
        {
            if (!connections.TryGetValue(connectionId, out KcpServerConnection connection))
            {
                connection = CreateConnection(connectionId);
                connection.RawInput(segment);
                connection.TickIncoming();
            }
            else
            {
                connection.RawInput(segment);
            }
        }

        readonly HashSet<int> connectionsToRemove = new HashSet<int>();
        public virtual void TickIncoming()
        {
            while (RawReceiveFrom(out ArraySegment<byte> segment, out int connectionId))
            {
                ProcessMessage(segment, connectionId);
            }

            foreach (KcpServerConnection connection in connections.Values)
            {
                connection.TickIncoming();
            }

            foreach (int connectionId in connectionsToRemove)
            {
                connections.Remove(connectionId);
            }
            connectionsToRemove.Clear();
        }

        public virtual void TickOutgoing()
        {
            foreach (KcpServerConnection connection in connections.Values)
            {
                connection.TickOutgoing();
            }
        }

        public virtual void Tick()
        {
            TickIncoming();
            TickOutgoing();
        }

        public virtual void Stop()
        {
            connections.Clear();
            socket?.Close();
            socket = null;
        }
    }
}
