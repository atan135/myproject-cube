using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace kcp2k
{
    public static class Common
    {
        // helper function to resolve host to IPAddress
        public static bool ResolveHostname(string hostname, out IPAddress[] addresses)
        {
            try
            {
                addresses = Dns.GetHostAddresses(hostname);
                return addresses.Length >= 1;
            }
            catch (SocketException exception)
            {
                Log.Info($"[KCP] Failed to resolve host: {hostname} reason: {exception}");
                addresses = null;
                return false;
            }
        }

        // if connections drop under heavy load, increase to OS limit.
        public static void ConfigureSocketBuffers(Socket socket, int recvBufferSize, int sendBufferSize)
        {
            int initialReceive = socket.ReceiveBufferSize;
            int initialSend    = socket.SendBufferSize;

            try
            {
                socket.ReceiveBufferSize = recvBufferSize;
                socket.SendBufferSize    = sendBufferSize;
            }
            catch (SocketException)
            {
                Log.Warning($"[KCP] failed to set Socket RecvBufSize = {recvBufferSize} SendBufSize = {sendBufferSize}");
            }

            Log.Info($"[KCP] RecvBuf = {initialReceive}=>{socket.ReceiveBufferSize} SendBuf = {initialSend}=>{socket.SendBufferSize}");
        }

        // generate a connection hash from IP+Port.
        public static int ConnectionHash(EndPoint endPoint) =>
            endPoint.GetHashCode();

        // cookies need to be generated with a secure random generator.
        static readonly RNGCryptoServiceProvider cryptoRandom = new RNGCryptoServiceProvider();
        static readonly byte[] cryptoRandomBuffer = new byte[4];
        public static uint GenerateCookie()
        {
            cryptoRandom.GetBytes(cryptoRandomBuffer);
            return BitConverter.ToUInt32(cryptoRandomBuffer, 0);
        }
    }
}
