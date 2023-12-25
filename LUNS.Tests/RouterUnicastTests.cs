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
    public class RouterUnicastTests
    {
        [TestMethod]
        public void From1To2Test()
        {
            RouterUnicastTest(
                allIds: new NodeId[] { new NodeId(1), new NodeId(2)},
                fromId: new NodeId(1),
                toId:   new NodeId(2));
        }

        [TestMethod]
        public void From1To2WithBackwardRouteTest()
        {
            RouterUnicastTest(
                allIds: new NodeId[] { new NodeId(1), new NodeId(2) },
                fromId: new NodeId(1),
                toId: new NodeId(2),
                RouteAddingMode.Backward);
        }

        [TestMethod]
        public void From1To2BidirectionalRouteTest()
        {
            RouterUnicastTest(
                allIds: new NodeId[] { new NodeId(1), new NodeId(2) },
                fromId: new NodeId(1),
                toId: new NodeId(2),
                RouteAddingMode.Bidirectional);
        }

        [TestMethod]
        public void From1To3Test()
        {
            RouterUnicastTest(
                allIds: new NodeId[] { new NodeId(1), new NodeId(2), new NodeId(3) },
                fromId: new NodeId(1),
                toId:   new NodeId(3));
        }

        [TestMethod]
        public void NoRouteTest()
        {
            RouterUnicastTest(
                allIds: new NodeId[] { new NodeId(1), new NodeId(2) },
                fromId: new NodeId(1),
                toId: new NodeId(2),
                RouteAddingMode.None);
        }

        private enum RouteAddingMode
        { 
            None, 
            Forward,
            Backward,
            Bidirectional
        }

        private void RouterUnicastTest(NodeId[] allIds, NodeId fromId, NodeId toId, RouteAddingMode mode = RouteAddingMode.Forward)
        {
            var net = new Network(new TestPortProvider(1, 0));
            Dictionary<NodeId, ApplicationOnNode> nodes = allIds.ToDictionary(id => id, id => ApplicationOnNode.Create(net, id));

            try
            {
                if (mode == RouteAddingMode.Forward)
                {
                    net.AddRoute(fromId, toId);
                }
                else if (mode == RouteAddingMode.Backward)
                {
                    net.AddRoute(toId, fromId);
                }
                else if (mode == RouteAddingMode.Bidirectional)
                {
                    net.AddRoute(fromId, toId);
                    net.AddRoute(toId, fromId);
                }

                using (var router = new Router(net))
                {
                    router.Start();


                    if (mode == RouteAddingMode.Forward || mode == RouteAddingMode.Bidirectional)
                    {
                        byte[] data = Encoding.UTF8.GetBytes("Hello World!");
                        nodes[fromId].Send(data, net.GetRemoteEndpoint(nodes[fromId].NodeAppId.NodeId, new NodeApplicationId(toId)));
                        Thread.Sleep(TimeSpan.FromSeconds(1.0)); // assumption that all packets are routed within a second

                        Assert.AreEqual(1, nodes[toId].ReceivedUcPackets.Count);
                        Assert.IsTrue(nodes[toId].ReceivedUcPackets.TryDequeue(out TestPacket packet));
                        Assert.AreEqual(net.RouterAddress, packet.SourceEndPoint.Address); // the packet always comes form the router
                        Assert.AreEqual(nodes[toId].UnicastEndpoint, packet.TargetEndPoint);
                        Assert.AreEqual(data.Length, packet.Data.Length);
                        CollectionAssert.AreEqual(data, packet.Data);
                    }
                    else if (mode == RouteAddingMode.Backward)
                    {
                        Assert.IsNull(net.GetRemoteEndpoint(nodes[fromId].NodeAppId.NodeId, new NodeApplicationId(toId)));
                    }
                    else
                    {
                        Assert.IsNull(net.GetRemoteEndpoint(nodes[fromId].NodeAppId.NodeId, new NodeApplicationId(toId)));
                    }
                    Assert.AreEqual(0, nodes[toId].ReceivedMcPackets.Count);

                    foreach(NodeId nodeId in allIds)
                    {
                        if(nodeId != toId)
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
