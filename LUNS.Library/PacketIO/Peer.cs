using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LUNS.Library.PacketIO
{
    internal class Peer : IDisposable
    {
        private UdpClient _client;
        private IPEndPoint _lastSourceEP;
        private Thread _serverThread;
        private readonly IPEndPoint _localEndPoint;
        private readonly ManualResetEvent _abortSignal = new ManualResetEvent(false);
        private readonly AutoResetEvent _dataReceivedSignal = new AutoResetEvent(false);

        internal event EventHandler<PacketReceivedEventArgs> PacketReceived;

        public Peer(IPEndPoint localEndpoint)
        {
            _client = new UdpClient(AddressFamily.InterNetwork);
            _client.Client.Bind(localEndpoint);
            _client.Client.ReceiveBufferSize = 1024 * 1024 * 16; // the router needs a large buffer for receiveing all the packets or packets will get dropped
            _localEndPoint = (IPEndPoint)_client.Client.LocalEndPoint;
        }

        public IPEndPoint LocalEndPoint => _client.Client.LocalEndPoint as IPEndPoint;

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
                    try
                    {
                        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                        byte[] data = _client.EndReceive(res, ref remoteEP);
                        _lastSourceEP = remoteEP;
                        var packet = new Packet(remoteEP, _localEndPoint, data);
                        PacketReceived?.Invoke(this, new PacketReceivedEventArgs(packet));
                    }
                    catch (Exception)
                    {
                        // todo: log warning?
                        break;
                    }
                }
            }
        }

        private void OnReceiveCallback(IAsyncResult _)
        {
            _dataReceivedSignal.Set();
        }

        public void SendBack(byte[] data)
        {
            _client.Send(data, data.Length, _lastSourceEP);
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
