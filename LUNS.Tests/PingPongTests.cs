using LUNS.Library;
using LUNS.Tests.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LUNS.Tests
{
    [TestClass]
    public class PingPongTests
    {
        private TestPacket _packetOnReceiverSide = null;
        private TestPacket _packetOnSenderSide = null;

        [TestMethod]
        public void UnicastPingPongBindTest()
        {
            UnicastPingPongTest(bindSender: true);
        }

        [TestMethod]
        public void UnicastPingPongNoBindTest()
        {
            UnicastPingPongTest(bindSender: false);
        }

        private void UnicastPingPongTest(bool bindSender)
        { 
            var net = new Network(new TestPortProvider(1, 0));

            NodeId fromId = new NodeId(1);
            NodeId toId = new NodeId(2);
            net.AddRoute(fromId, toId);
            net.AddRoute(toId, fromId);

            using (var router = new Router(net))
            {
                router.Start();

                var receiverPeer = new TestPeer(net.GetLocalUnicastEndpoint(new NodeApplicationId(toId)));
                receiverPeer.PacketReceived += Receiver_PacketReceived;
                receiverPeer.Start();

                var senderPeer = new TestPeer(net.GetLocalUnicastEndpoint(new NodeApplicationId(fromId)), bindSender);
                senderPeer.PacketReceived += Sender_PacketReceived;
                senderPeer.Start();

                try
                {
                    byte[] data = Encoding.UTF8.GetBytes("Ping");
                    senderPeer.Send(data, net.GetRemoteEndpoint(fromId, new NodeApplicationId(toId)));

                    Thread.Sleep(TimeSpan.FromSeconds(1.0)); // assumption that all packets are routed within a second

                    Assert.IsNotNull(_packetOnReceiverSide);
                    Assert.AreEqual("Ping", Encoding.UTF8.GetString(_packetOnReceiverSide.Data));

                    Assert.IsNotNull(_packetOnSenderSide);
                    Assert.AreEqual("Pong", Encoding.UTF8.GetString(_packetOnSenderSide.Data));
                }
                finally
                {
                    receiverPeer.Dispose();
                    senderPeer.Dispose();
                }
            }
        }

        private void Sender_PacketReceived(object sender, TestPacketReceivedEventArgs e)
        {
            Assert.IsNull(_packetOnSenderSide);
            _packetOnSenderSide = e.Packet;
        }

        private void Receiver_PacketReceived(object sender, TestPacketReceivedEventArgs e)
        {
            Assert.IsNull(_packetOnReceiverSide);
            _packetOnReceiverSide = e.Packet;

            var peer = (TestPeer)sender;
            peer.Send(Encoding.UTF8.GetBytes("Pong"), e.Packet.SourceEndPoint);
        }
    }
}
