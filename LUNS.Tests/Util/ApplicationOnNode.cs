using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using LUNS.Library;

namespace LUNS.Tests.Util
{
    /// <summary>
    /// Represents a node in the network and provides methods to send/receive data from this node
    /// </summary>
    internal class ApplicationOnNode : IDisposable
    {
        private readonly TestReceiver _ucReceiver;
        private readonly List<TestReceiver> _mcReceivers;
        private readonly TestSender _sender;
        private readonly AutoResetEvent _packetReceivedSignal = new AutoResetEvent(false);

        public NodeApplicationId NodeAppId { get; }

        /// <summary>
        /// Local listening endpoint for unicast packets
        /// </summary>
        public IPEndPoint UnicastEndpoint { get; }

        /// <summary>
        /// Local listening endpoints for multicast packets
        /// </summary>
        public IPEndPoint[] MulticastEndpoints { get; }

        public ConcurrentQueue<TestPacket> ReceivedUcPackets { get; }

        public ConcurrentQueue<TestPacket> ReceivedMcPackets { get; }

        public static ApplicationOnNode Create(Network net, NodeId nodeId, byte applicationIndex = 0)
        {
            IPEndPoint ucEp = net.GetLocalUnicastEndpoint(new NodeApplicationId(nodeId, applicationIndex));
            return new ApplicationOnNode(new NodeApplicationId(nodeId, applicationIndex), ucEp);
        }

        public static ApplicationOnNode Create(Network net, NodeId nodeId, GroupId groupId, byte applicationIndex = 0)
        {
            IPEndPoint ucEp = net.GetLocalUnicastEndpoint(new NodeApplicationId(nodeId, applicationIndex));
            IPEndPoint mcEp = net.GetLocalMulticastEndpoint(nodeId, new GroupApplicationId(groupId, applicationIndex));
            return new ApplicationOnNode(new NodeApplicationId(nodeId, applicationIndex), ucEp, mcEp);
        }

        public static ApplicationOnNode Create(Network net, NodeId nodeId, GroupId[] groupIds, byte applicationIndex = 0)
        {
            IPEndPoint ucEp = net.GetLocalUnicastEndpoint(new NodeApplicationId(nodeId, applicationIndex));
            IPEndPoint[] mcEps = groupIds.Select(groupId => net.GetLocalMulticastEndpoint(nodeId, new GroupApplicationId(groupId, applicationIndex))).ToArray();
            return new ApplicationOnNode(new NodeApplicationId(nodeId, applicationIndex), ucEp, mcEps);
        }

        public ApplicationOnNode(NodeApplicationId nodeAppId, IPEndPoint localUcEndpoint, params IPEndPoint[] localMcEndpoints)
        {
            NodeAppId = nodeAppId;
            UnicastEndpoint = localUcEndpoint;
            MulticastEndpoints = localMcEndpoints;

            ReceivedUcPackets = new();

            ReceivedMcPackets = new();

            _ucReceiver = new TestReceiver(localUcEndpoint);
            _ucReceiver.PacketReceived += Receiver_PacketReceived;
            _ucReceiver.Start();

            _mcReceivers = new List<TestReceiver>();
            foreach(IPEndPoint mcEp in localMcEndpoints)
            {
                var mcReceiver = new TestReceiver(mcEp);
                mcReceiver.PacketReceived += Receiver_PacketReceived;
                mcReceiver.Start();
                _mcReceivers.Add(mcReceiver);
            }

            _sender = new TestSender();
        }

        private void Receiver_PacketReceived(object sender, TestPacketReceivedEventArgs e)
        {
            if (sender == _ucReceiver)
                ReceivedUcPackets.Enqueue(e.Packet);
            else
                ReceivedMcPackets.Enqueue(e.Packet);

            _packetReceivedSignal.Set();
        }

        public void Send(byte[] data, IPEndPoint target)
        {
            _sender.Send(data, target);
        }

        public void WaitNextReceivedPacket()
        {
            _packetReceivedSignal.WaitOne();
        }

        public bool WaitNextReceivedPacket(TimeSpan maxDelay)
        {
            return _packetReceivedSignal.WaitOne(maxDelay);
        }

        public void Dispose()
        {
            _ucReceiver.Dispose();
            foreach (TestReceiver mcReceiver in _mcReceivers)
            {
                mcReceiver.Dispose();
            }
            _sender.Dispose();
        }
    }
}
