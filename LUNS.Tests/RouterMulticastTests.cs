using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using LUNS.Library;
using LUNS.Tests.Util;

namespace LUNS.Tests
{
    [TestClass]
    public partial class RouterMulticastTests
    {
        [TestMethod]
        public void From1To2Test()
        {
            RouterMulticastTest(
                allIds: new NodeId[] { new NodeId(1), new NodeId(2)},
                fromId: new NodeId(1),
                toIds:  new NodeId[] { new NodeId(2) });
        }

        [TestMethod]
        public void From1To2And3Test()
        {
            RouterMulticastTest(
                allIds: new NodeId[] { new NodeId(1), new NodeId(2), new NodeId(3) },
                fromId: new NodeId(1),
                toIds:  new NodeId[] { new NodeId(2), new NodeId(3) });
        }

        [TestMethod]
        public void MultipleGroupsTest()
        {
            Network net = new Network(new TestPortProvider(1, 2));

            var group1Id = new GroupId(1);
            var group2Id = new GroupId(2);

            ApplicationOnNode node1 = ApplicationOnNode.Create(net, new NodeId(1));
            ApplicationOnNode node2 = ApplicationOnNode.Create(net, new NodeId(2), new GroupId[] { group1Id });
            ApplicationOnNode node3 = ApplicationOnNode.Create(net, new NodeId(3), new GroupId[] { group1Id, group2Id });

            try
            {
                // add routes from nodes 1->2 and 1->3
                net.AddRoute(node1.NodeAppId.NodeId, node2.NodeAppId.NodeId);
                net.AddRoute(node1.NodeAppId.NodeId, node3.NodeAppId.NodeId);

                // node 1 is in both groups
                net.JoinMulticastGroup(node1.NodeAppId.NodeId, group1Id);
                net.JoinMulticastGroup(node1.NodeAppId.NodeId, group2Id);

                // node 2 and 3 are in group 1
                net.JoinMulticastGroup(node2.NodeAppId.NodeId, group1Id);
                net.JoinMulticastGroup(node3.NodeAppId.NodeId, group1Id);

                // node 3 is also in group 2
                net.JoinMulticastGroup(node3.NodeAppId.NodeId, group2Id);

                using (var router = new Router(net))
                {
                    router.Start();

                    // send to both groups
                    byte[] data1 = Encoding.UTF8.GetBytes("Hello ");
                    node1.Send(data1, net.GetRemoteEndpoint(node1.NodeAppId.NodeId, new GroupApplicationId(group1Id)));
                    Thread.Sleep(TimeSpan.FromSeconds(1.0)); // make sure packets are received in order
                    byte[] data2 = Encoding.UTF8.GetBytes("World!");
                    node1.Send(data2, net.GetRemoteEndpoint(node1.NodeAppId.NodeId, new GroupApplicationId(group2Id)));
                    Thread.Sleep(TimeSpan.FromSeconds(1.0)); // assumption that all packets are routed within a second

                    // check if number of received packets is ok
                    Assert.AreEqual(0, node1.ReceivedUcPackets.Count);
                    Assert.AreEqual(0, node2.ReceivedUcPackets.Count);
                    Assert.AreEqual(0, node3.ReceivedUcPackets.Count);
                    Assert.AreEqual(1, node2.ReceivedMcPackets.Count);
                    Assert.AreEqual(2, node3.ReceivedMcPackets.Count);

                    // check packets
                    Assert.IsTrue(node2.ReceivedMcPackets.TryDequeue(out TestPacket packet2_1));
                    Assert.AreEqual(net.RouterAddress, packet2_1.SourceEndPoint.Address); // the packet always comes form the router
                    Assert.AreEqual(node2.MulticastEndpoints.Single(), packet2_1.TargetEndPoint);
                    Assert.AreEqual(data1.Length, packet2_1.Data.Length);
                    CollectionAssert.AreEqual(data1, packet2_1.Data);

                    Assert.IsTrue(node3.ReceivedMcPackets.TryDequeue(out TestPacket packet3_1));
                    Assert.AreEqual(net.RouterAddress, packet3_1.SourceEndPoint.Address); // the packet always comes form the router
                    Assert.AreEqual(node3.MulticastEndpoints[0], packet3_1.TargetEndPoint);
                    Assert.AreEqual(data1.Length, packet3_1.Data.Length);
                    CollectionAssert.AreEqual(data1, packet3_1.Data);

                    Assert.AreNotEqual(packet2_1.TargetEndPoint, packet3_1.TargetEndPoint);

                    Assert.IsTrue(node3.ReceivedMcPackets.TryDequeue(out TestPacket packet3_2));
                    Assert.AreEqual(net.RouterAddress, packet3_2.SourceEndPoint.Address); // the packet always comes form the router
                    Assert.AreEqual(node3.MulticastEndpoints[1], packet3_2.TargetEndPoint);
                    Assert.AreEqual(data2.Length, packet3_2.Data.Length);
                    CollectionAssert.AreEqual(data2, packet3_2.Data);

                    Assert.AreNotEqual(packet3_2.TargetEndPoint, packet3_1.TargetEndPoint);
                }
            }
            finally
            {
                node1.Dispose();
                node2.Dispose();
                node3.Dispose();
            }
        }

        [TestMethod]
        public void NoRouteTest()
        {
            RouterMulticastTest(
                allIds: new NodeId[] { new NodeId(1), new NodeId(2), new NodeId(3) },
                fromId: new NodeId(1),
                toIds: new NodeId[] { new NodeId(2), new NodeId(3) },
                addRoutes: false);
        }

        private void RouterMulticastTest(NodeId[] allIds, NodeId fromId, NodeId[] toIds, bool addRoutes = true)
        {
            var net = new Network(31000, 31001);
            var groupId = new GroupId(101);
            Dictionary<NodeId, ApplicationOnNode> nodes = allIds.ToDictionary(id => id, id =>
            {
                if (id != fromId)
                {
                    return ApplicationOnNode.Create(net, id, groupId);
                }
                else
                {
                    return ApplicationOnNode.Create(net, id);
                }
            });

            try
            {
                if (addRoutes)
                {
                    foreach (NodeId toId in toIds)
                    {
                        net.AddRoute(fromId, toId);
                    }
                }

                foreach (NodeId toId in toIds)
                {
                    net.JoinMulticastGroup(toId, groupId);
                }
                net.JoinMulticastGroup(fromId, groupId);

                using (var router = new Router(net))
                {
                    router.Start();

                    byte[] data = Encoding.UTF8.GetBytes("Hello World!");
                    nodes[fromId].Send(data, net.GetRemoteEndpoint(nodes[fromId].NodeAppId.NodeId, new GroupApplicationId(groupId)));
                    Thread.Sleep(TimeSpan.FromSeconds(1.0)); // assumption that all packets are routed within a second

                    foreach (NodeId toId in toIds)
                    {
                        Assert.AreEqual(0, nodes[toId].ReceivedUcPackets.Count);

                        if (addRoutes)
                        {
                            Assert.AreEqual(1, nodes[toId].ReceivedMcPackets.Count);
                            Assert.IsTrue(nodes[toId].ReceivedMcPackets.TryDequeue(out TestPacket packet));
                            Assert.AreEqual(net.RouterAddress, packet.SourceEndPoint.Address); // the packet always comes form the router
                            Assert.AreEqual(nodes[toId].MulticastEndpoints[0], packet.TargetEndPoint);
                            Assert.AreEqual(data.Length, packet.Data.Length);
                            CollectionAssert.AreEqual(data, packet.Data);
                        }
                        else
                        {
                            Assert.AreEqual(0, nodes[toId].ReceivedMcPackets.Count);
                        }
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(1.0)); // assumption that all packets are routed within a second
                    foreach(NodeId nodeId in allIds)
                    {
                        if(!toIds.Contains(nodeId))
                        {
                            Assert.AreEqual(0, nodes[nodeId].ReceivedUcPackets.Count);
                            Assert.AreEqual(0, nodes[nodeId].ReceivedMcPackets.Count);
                        }
                    }
                }
            }
            finally
            {
                foreach (ApplicationOnNode node in nodes.Values)
                {
                    node.Dispose();
                }
            }
        }
    }
}
