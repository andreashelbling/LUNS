using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Threading;
using LUNS.Library;
using LUNS.Tests.Util;

namespace LUNS.Tests
{
    [TestClass]
    public class RouterUnicastMultiAppTests
    {
        [TestMethod]
        public void ParallelAppCommunicationTest()
        {
            // application with index i has uc port 31000 + i and mc port 31010 + i
            var net = new Network(new int[] { 31000, 31001 }, new int[] { 31010, 31011 });
            var app1Node1 = ApplicationOnNode.Create(net, new NodeId(1), 0);
            var app1Node2 = ApplicationOnNode.Create(net, new NodeId(2), 0);
            var app2Node1 = ApplicationOnNode.Create(net, new NodeId(1), 1);
            var app2Node2 = ApplicationOnNode.Create(net, new NodeId(2), 1);

            try
            {
                net.AddRoute(new NodeId(1), new NodeId(2));

                using (var router = new Router(net))
                {
                    router.Start();

                    byte[] data1 = Encoding.UTF8.GetBytes("Hello App1!");
                    byte[] data2 = Encoding.UTF8.GetBytes("Hello Application 2!");

                    // ok cases
                    app1Node1.Send(data1, net.GetRemoteEndpoint(app1Node1.NodeAppId.NodeId, app1Node2.NodeAppId));
                    app2Node1.Send(data2, net.GetRemoteEndpoint(app2Node1.NodeAppId.NodeId, app2Node2.NodeAppId));

                    Thread.Sleep(TimeSpan.FromSeconds(1.0)); // assumption that all packets are routed within a second

                    // senders have no packets
                    Assert.AreEqual(0, app1Node1.ReceivedUcPackets.Count);
                    Assert.AreEqual(0, app2Node1.ReceivedUcPackets.Count);
                    Assert.AreEqual(0, app1Node1.ReceivedMcPackets.Count);
                    Assert.AreEqual(0, app2Node1.ReceivedMcPackets.Count);

                    // receivers have only one uc packet
                    Assert.AreEqual(1, app1Node2.ReceivedUcPackets.Count);
                    Assert.AreEqual(1, app2Node2.ReceivedUcPackets.Count);
                    Assert.AreEqual(0, app1Node2.ReceivedMcPackets.Count);
                    Assert.AreEqual(0, app2Node2.ReceivedMcPackets.Count);

                    // check packet for app 1
                    Assert.IsTrue(app1Node2.ReceivedUcPackets.TryDequeue(out TestPacket packet1));
                    Assert.AreEqual(net.RouterAddress, packet1.SourceEndPoint.Address); // the packet always comes form the router
                    Assert.AreEqual(app1Node2.UnicastEndpoint, packet1.TargetEndPoint);
                    Assert.AreEqual(data1.Length, packet1.Data.Length);
                    CollectionAssert.AreEqual(data1, packet1.Data);

                    // check packet for app 2
                    Assert.IsTrue(app2Node2.ReceivedUcPackets.TryDequeue(out TestPacket packet2));
                    Assert.AreEqual(net.RouterAddress, packet1.SourceEndPoint.Address); // the packet always comes form the router
                    Assert.AreEqual(app2Node2.UnicastEndpoint, packet2.TargetEndPoint);
                    Assert.AreEqual(data2.Length, packet2.Data.Length);
                    CollectionAssert.AreEqual(data2, packet2.Data);
                }
            }
            finally
            {
                app1Node1.Dispose();
                app1Node2.Dispose();
                app2Node1.Dispose();
                app2Node2.Dispose();
            }
        }
    }
}
