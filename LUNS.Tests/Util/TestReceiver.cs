using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LUNS.Tests.Util
{
    internal class TestReceiver : IDisposable
    {
        protected UdpClient _client;
        private Thread _serverThread;
        private readonly IPEndPoint _localEndPoint;
        private readonly ManualResetEvent _abortSignal = new ManualResetEvent(false);
        private readonly AutoResetEvent _dataReceivedSignal = new AutoResetEvent(false);

        internal event EventHandler<TestPacketReceivedEventArgs> PacketReceived;

        public TestReceiver(IPEndPoint localEndpoint, bool bind = true)
        {
            if (bind)
            {
                _client = new UdpClient(AddressFamily.InterNetwork);
                _client.Client.Bind(localEndpoint);
            }
            else
            {
                _client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            }
            _localEndPoint = (IPEndPoint)_client.Client.LocalEndPoint;
        }

        public void Start()
        {
            _abortSignal.Reset();
            _dataReceivedSignal.Reset();
            _serverThread = new Thread(ReceiveThreadMain);
            _serverThread.Start();
        }

        private void ReceiveThreadMain()
        {
            while (true)
            {
                IAsyncResult res = _client.BeginReceive(OnReceiveCallback, null);
                int index = WaitHandle.WaitAny(new WaitHandle[] { _dataReceivedSignal, _abortSignal }); // wait until either the receiver is stopped or data is received.
                if (index == 1) // abortSignal was triggered
                {
                    break;
                }
                else // _dataReceivedSignal was triggered
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _client.EndReceive(res, ref remoteEP);
                    var packet = new TestPacket(_localEndPoint, remoteEP, data);
                    PacketReceived?.Invoke(this, new TestPacketReceivedEventArgs(packet));
                }
            }
        }

        private void OnReceiveCallback(IAsyncResult _)
        {
            _dataReceivedSignal.Set();
        }

        public void Stop()
        {
            _abortSignal.Set();
            _serverThread.Join();
            _serverThread = null;
        }

        public void Dispose()
        {
            if (_serverThread != null)
            {
                Stop();
            }
            _client.Dispose();
            _client = null;
        }
    }
}
