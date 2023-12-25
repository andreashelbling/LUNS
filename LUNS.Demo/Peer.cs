using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LUNS.Demo
{
    internal class Peer : IDisposable
    {
        private readonly IPEndPoint _localEndPoint;
        private readonly ManualResetEvent _abortSignal = new ManualResetEvent(false);
        private readonly AutoResetEvent _dataReceivedSignal = new AutoResetEvent(false);

        private UdpClient _client; 
        private Thread _serverThread;

        internal event EventHandler<PacketReceivedEventArgs> PacketReceived;

        public Peer(string name, IPEndPoint localEndpoint)
        {
            Name = name;
            _client = new UdpClient(AddressFamily.InterNetwork);
            _client.Client.Bind(localEndpoint);
            _localEndPoint = (IPEndPoint)_client.Client.LocalEndPoint;
        }

        public IPEndPoint LocalEndPoint => _client.Client.LocalEndPoint as IPEndPoint;

        public string Name { get; }

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
                int index = WaitHandle.WaitAny(new WaitHandle[] { _abortSignal, _dataReceivedSignal }); // wait until either the receiver is stopped or data is received.
                if (index == 0) // abortSignal was triggered
                {
                    break;
                }
                else // _dataReceivedSignal was triggered
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _client.EndReceive(res, ref remoteEP);
                    PacketReceived?.Invoke(this, new PacketReceivedEventArgs(data, remoteEP, _localEndPoint));
                }
            }
        }

        private void OnReceiveCallback(IAsyncResult _)
        {
            _dataReceivedSignal.Set();
        }

        public void Send(byte[] data, IPEndPoint remoteEndPoint)
        {
            _client.Send(data, data.Length, remoteEndPoint);
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
