using System;
using System.Net;
using System.Net.Sockets;

namespace LUNS.Tests.Util
{
    internal class TestSender : IDisposable
    {
        private UdpClient _client;

        public TestSender()
        {
            _client = new UdpClient();
        }

        public void Send(byte[] data, IPEndPoint remoteEndPoint)
        {
            _client.Send(data, data.Length, remoteEndPoint);
        }

        public void Dispose()
        {
            _client.Dispose();
            _client = null;
        }
    }
}
