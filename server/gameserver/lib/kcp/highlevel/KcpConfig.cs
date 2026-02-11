// common config struct, instead of passing 10 parameters manually every time.
using System;

namespace kcp2k
{
    // 'class' so we can set defaults easily.
    [Serializable]
    public class KcpConfig
    {
        // socket configuration ////////////////////////////////////////////////
        // DualMode uses both IPv6 and IPv4. not all platforms support it.
        public bool DualMode;

        // UDP servers use only one socket.
        // maximize buffer to handle as many connections as possible.
        public int RecvBufferSize;
        public int SendBufferSize;

        // kcp configuration ///////////////////////////////////////////////////
        // configurable MTU in case kcp sits on top of other abstractions like
        // encrypted transports, relays, etc.
        public int Mtu;

        // NoDelay is recommended to reduce latency. This also scales better
        // without buffers getting full.
        public bool NoDelay;

        // KCP internal update interval. 100ms is KCP default, but a lower
        // interval is recommended to minimize latency and to scale to more
        // networked entities.
        public uint Interval;

        // KCP fastresend parameter. Faster resend for the cost of higher
        // bandwidth.
        public int FastResend;

        // KCP congestion window heavily limits messages flushed per update.
        public bool CongestionWindow;

        // KCP window size can be modified to support higher loads.
        public uint SendWindowSize;
        public uint ReceiveWindowSize;

        // timeout in milliseconds
        public int Timeout;

        // maximum retransmission attempts until dead_link
        public uint MaxRetransmits;

        // constructor /////////////////////////////////////////////////////////
        public KcpConfig(
            bool DualMode          = true,
            int RecvBufferSize     = 1024 * 1024 * 7,
            int SendBufferSize     = 1024 * 1024 * 7,
            int Mtu                = Kcp.MTU_DEF,
            bool NoDelay           = true,
            uint Interval          = 10,
            int FastResend         = 0,
            bool CongestionWindow  = false,
            uint SendWindowSize    = Kcp.WND_SND,
            uint ReceiveWindowSize = Kcp.WND_RCV,
            int Timeout            = KcpPeer.DEFAULT_TIMEOUT,
            uint MaxRetransmits    = Kcp.DEADLINK)
        {
            this.DualMode = DualMode;
            this.RecvBufferSize = RecvBufferSize;
            this.SendBufferSize = SendBufferSize;
            this.Mtu = Mtu;
            this.NoDelay = NoDelay;
            this.Interval = Interval;
            this.FastResend = FastResend;
            this.CongestionWindow = CongestionWindow;
            this.SendWindowSize = SendWindowSize;
            this.ReceiveWindowSize = ReceiveWindowSize;
            this.Timeout = Timeout;
            this.MaxRetransmits = MaxRetransmits;
        }
    }
}
