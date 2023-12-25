using System;
using System.Net;
using System.Text;
using LUNS.Library;

namespace LUNS.Demo
{
    class ApplicationOnNode : IDisposable
    {
        private readonly Peer _ucPeer;
        private readonly Peer _mcPeer;
        private readonly Network _network;

        public NodeApplicationId Id { get; }

        public IPEndPoint UnicastEndpoint { get; }
        
        public IPEndPoint MulticastEndpoint { get; }

        public ApplicationOnNode(Network network, NodeApplicationId id, IPEndPoint localUcEndpoint, IPEndPoint localMcEndpoint)
        {
            _network = network;
            Id = id;
            UnicastEndpoint = localUcEndpoint;
            MulticastEndpoint = localMcEndpoint;

            _ucPeer = new Peer($"Node {Id} UC", localUcEndpoint);
            _ucPeer.PacketReceived += OnPacketReceived;
            _ucPeer.Start();

            if (localMcEndpoint != null)
            {
                _mcPeer = new Peer($"Node {Id} MC", localMcEndpoint);
                _mcPeer.PacketReceived += OnPacketReceived;
                _mcPeer.Start();
            }
        }

        private void OnPacketReceived(object s, PacketReceivedEventArgs e)
        {
            string msg = Encoding.UTF8.GetString(e.Data);
            var peer = (Peer)s;
            Console.WriteLine("{0} {1} -> Received from {2}: '{3}'",
                peer.Name,
                e.Target.ToString(),
                e.Source.ToString(),
                msg
            );

            if (msg != "ACK")
            {
                peer.Send(Encoding.UTF8.GetBytes("ACK"), e.Source);
            }
        }

        public void SendText(string text, NodeId targetNodeId)
        {
            IPEndPoint remoteEP = _network.GetRemoteEndpoint(Id.NodeId, new NodeApplicationId(targetNodeId, Id.ApplicationIndex));
            _ucPeer.Send(Encoding.UTF8.GetBytes(text), remoteEP);
        }

        public void SendText(string text, GroupId targetGroupId)
        {
            IPEndPoint remoteEP = _network.GetRemoteEndpoint(Id.NodeId, new GroupApplicationId(targetGroupId, Id.ApplicationIndex));
            _mcPeer.Send(Encoding.UTF8.GetBytes(text), remoteEP);
        }

        public void Dispose()
        {
            _ucPeer.Dispose();
            if (_mcPeer != null)
            {
                _mcPeer.Dispose();
            }
        }
    }
}
