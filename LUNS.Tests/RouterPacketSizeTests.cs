using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using LUNS.Library;
using LUNS.Tests.Util;

namespace LUNS.Tests
{
    [TestClass]
    public class RouterPacketSizeTests
    {
        [TestMethod]
        public void RouterPacketSizeTestZero()
        {
            RouterPacketSizeTest(new byte[0]);
        }

        [TestMethod]
        public void RouterPacketSizeTestTiny()
        {
            RouterPacketSizeTest(new byte[1] { 0x77 });
        }

        [TestMethod]
        public void RouterPacketSizeTestSmall()
        {
            RouterPacketSizeTest(Encoding.UTF8.GetBytes("Hello World"));
        }

        [TestMethod]
        public void RouterPacketSizeTestLarge()
        {
            // 4 kBytes is the default maximum on Windows
            RouterPacketSizeTest(TestDataCreator.CreateTestData(4 * 1024));
        }

        private void RouterPacketSizeTest(byte[] data)
        {
            var net = new Network(31000, 31001);

            ApplicationOnNode node1 = ApplicationOnNode.Create(net, new NodeId(1));
            ApplicationOnNode node2 = ApplicationOnNode.Create(net, new NodeId(2));
            ApplicationOnNode node3 = ApplicationOnNode.Create(net, new NodeId(3));

            try
            {
                net.AddRoute(new NodeId(1), new NodeId(2));
                net.AddRoute(new NodeId(1), new NodeId(3));

                using (var router = new Router(net))
                {
                    router.Start();

                    node1.Send(data, net.GetRemoteEndpoint(node1.NodeAppId.NodeId, node3.NodeAppId));

                    Assert.IsTrue(node3.WaitNextReceivedPacket(TimeSpan.FromSeconds(10)));
                    Assert.AreEqual(1, node3.ReceivedUcPackets.Count);
                    Assert.IsTrue(node3.ReceivedUcPackets.TryDequeue(out TestPacket packet));
                    Assert.AreEqual(net.RouterAddress, packet.SourceEndPoint.Address); // the packet always comes form the router
                    Assert.AreEqual(node3.UnicastEndpoint, packet.TargetEndPoint);
                    Assert.AreEqual(data.Length, packet.Data.Length);
                    CollectionAssert.AreEqual(data, packet.Data);

                    node2.WaitNextReceivedPacket(TimeSpan.FromSeconds(1.0));
                    Assert.AreEqual(0, node2.ReceivedUcPackets.Count);
                    Assert.AreEqual(0, node2.ReceivedMcPackets.Count);

                    Assert.AreEqual(0, node3.ReceivedMcPackets.Count);
                }
            }
            finally
            {
                node3.Dispose();
                node2.Dispose();
                node1.Dispose();
            }
        }
    }
}
