// server needs to store a separate KcpPeer for each connection.
// as well as remoteEndPoint so we know where to send data to.
using System;
using System.Net;

namespace kcp2k
{
    public class KcpServerConnection : KcpPeer
    {
        public readonly EndPoint remoteEndPoint;

        // callbacks
        protected readonly Action<KcpServerConnection> OnConnectedCallback;
        protected readonly Action<ArraySegment<byte>, KcpChannel> OnDataCallback;
        protected readonly Action OnDisconnectedCallback;
        protected readonly Action<ErrorCode, string> OnErrorCallback;
        protected readonly Action<ArraySegment<byte>> RawSendCallback;

        public KcpServerConnection(
            Action<KcpServerConnection> OnConnected,
            Action<ArraySegment<byte>, KcpChannel> OnData,
            Action OnDisconnected,
            Action<ErrorCode, string> OnError,
            Action<ArraySegment<byte>> OnRawSend,
            KcpConfig config,
            uint cookie,
            EndPoint remoteEndPoint)
                : base(config, cookie)
        {
            OnConnectedCallback = OnConnected;
            OnDataCallback = OnData;
            OnDisconnectedCallback = OnDisconnected;
            OnErrorCallback = OnError;
            RawSendCallback = OnRawSend;

            this.remoteEndPoint = remoteEndPoint;
        }

        // callbacks ///////////////////////////////////////////////////////////
        protected override void OnAuthenticated()
        {
            // once we receive the first client hello,
            // immediately reply with hello so the client knows the security cookie.
            SendHello();
            OnConnectedCallback(this);
        }

        protected override void OnData(ArraySegment<byte> message, KcpChannel channel) =>
            OnDataCallback(message, channel);

        protected override void OnDisconnected() =>
            OnDisconnectedCallback();

        protected override void OnError(ErrorCode error, string message) =>
            OnErrorCallback(error, message);

        protected override void RawSend(ArraySegment<byte> data) =>
            RawSendCallback(data);
        ////////////////////////////////////////////////////////////////////////

        // insert raw IO. usually from socket.Receive.
        public void RawInput(ArraySegment<byte> segment)
        {
            // ensure valid size: at least 1 byte for channel + 4 bytes for cookie
            if (segment.Count <= 5) return;

            // parse channel
            byte channel = segment.Array[segment.Offset + 0];

            // parse cookie
            Utils.Decode32U(segment.Array, segment.Offset + 1, out uint messageCookie);

            // security: messages after authentication are expected to contain the cookie.
            if (state == KcpState.Authenticated)
            {
                if (messageCookie != cookie)
                {
                    Log.Info($"[KCP] ServerConnection: dropped message with invalid cookie: {messageCookie} from {remoteEndPoint} expected: {cookie} state: {state}.");
                    return;
                }
            }

            // parse message
            ArraySegment<byte> message = new ArraySegment<byte>(segment.Array, segment.Offset + 1+4, segment.Count - 1-4);

            switch (channel)
            {
                case (byte)KcpChannel.Reliable:
                {
                    OnRawInputReliable(message);
                    break;
                }
                case (byte)KcpChannel.Unreliable:
                {
                    OnRawInputUnreliable(message);
                    break;
                }
                default:
                {
                    Log.Warning($"[KCP] ServerConnection: invalid channel header: {channel} from {remoteEndPoint}, likely internet noise");
                    break;
                }
            }
        }
    }
}
