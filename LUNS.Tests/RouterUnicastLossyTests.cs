using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using LUNS.Library;
using LUNS.Tests.Util;

namespace LUNS.Tests
{
    [TestClass]
    public class RouterUnicastLossyTests
    {
        [TestMethod]
        public void SlowRouteSinglePacketTest()
        {
            RouterUnicastLossyTest(bitRate: 300.0, packetLossRate: 0.0, bitErrorRate: 0.0, dataLength: 300, segmentSize: 1000, 
                expectedSendTime: 8.0, maxSendTimeDelta: 0.5, expectedNumLostPackets: 0, expectedNumWrongBits: 0);
        }

        [TestMethod]
        public void SlowRouteMultiPacketTest()
        {
            RouterUnicastLossyTest(bitRate: 10000.0, packetLossRate: 0.0, bitErrorRate: 0.0, dataLength: 10000, segmentSize: 1000,
                expectedSendTime: 8.0, maxSendTimeDelta: 0.5, expectedNumLostPackets: 0, expectedNumWrongBits: 0);
        }
        
        [TestMethod]
        public void PacketLossTest()
        {
            // send 10'000 packts with loss rate of 10% -> 1000 packets should be lost. With the default seed, we get 1002
            RouterUnicastLossyTest(bitRate: 160000.0, packetLossRate: 0.1, bitErrorRate: 0.0, dataLength: 10000*16, segmentSize: 16,
                expectedSendTime: 8.0, maxSendTimeDelta: 0.5, expectedNumLostPackets: 1002, expectedNumWrongBits: 0);
        }

        [TestMethod]
        public void BitErrorTest()
        {
            // Send 80'000 bits with an error rate of 10% -> 8000 bits should be inverted. With the default seed, we get 8058
            RouterUnicastLossyTest(bitRate: 10000.0, packetLossRate: 0.0, bitErrorRate: 0.1, dataLength: 10*1000, segmentSize: 1000,
                expectedSendTime: 8.0, maxSendTimeDelta: 0.5, expectedNumLostPackets: 0, expectedNumWrongBits: 8058);
        }

        [TestMethod]
        public void VeryRareBitErrorTest()
        {
            // Send 80'000'000 bits with an error rate of 0.0001% -> 80 bits should be inverted. With the default seed, we get 84
            RouterUnicastLossyTest(bitRate: 10_000_000.0, packetLossRate: 0.0, bitErrorRate: 0.000001, dataLength: 10 * 1000 * 1000, segmentSize: 1000,
                expectedSendTime: 8.0, maxSendTimeDelta: 0.5, expectedNumLostPackets: 0, expectedNumWrongBits: 84);
        }

        private void RouterUnicastLossyTest(double bitRate, double packetLossRate, double bitErrorRate, 
            uint dataLength, uint segmentSize, double expectedSendTime, double maxSendTimeDelta, int expectedNumLostPackets, int expectedNumWrongBits)
        {
            var net = new Network(31000, 31001);

            ApplicationOnNode node1 = ApplicationOnNode.Create(net, new NodeId(1));
            ApplicationOnNode node2 = ApplicationOnNode.Create(net, new NodeId(2));

            try
            {
                net.AddRoute(node1.NodeAppId.NodeId, node2.NodeAppId.NodeId, new ConstantRouteSettings(bitRate, packetLossRate, bitErrorRate));

                using (var router = new Router(net))
                {
                    router.Start();

                    List<byte[]> data = TestDataCreator.CreateTestData(dataLength, segmentSize);
                    var sw = new Stopwatch();
                    sw.Start();
                    foreach (byte[] segment in data)
                    {
                        node1.Send(segment, net.GetRemoteEndpoint(node1.NodeAppId.NodeId, node2.NodeAppId));
                    }

                    // wait until received packet count is as expected or a timeout occurred
                    int lastReceivedPacketCount = 0;
                    double lastPacketTime = -1.0;
                    while (sw.Elapsed.TotalSeconds < 2 * expectedSendTime && node2.ReceivedUcPackets.Count < (data.Count - expectedNumLostPackets))
                    {
                        if (node2.ReceivedUcPackets.Count > lastReceivedPacketCount)
                        {
                            lastPacketTime = sw.Elapsed.TotalSeconds;
                            lastReceivedPacketCount = node2.ReceivedUcPackets.Count;
                        }
                        Thread.Sleep(1);
                    }
                    if (lastReceivedPacketCount != node2.ReceivedUcPackets.Count)
                    {
                        lastPacketTime = sw.Elapsed.TotalSeconds;
                        lastReceivedPacketCount = node2.ReceivedUcPackets.Count;
                    }
                    sw.Stop();

                    if (lastPacketTime >= 1.5 * expectedSendTime)
                    {
                        Assert.Fail($"Not all packets were received within 1.5x the expected time of {expectedSendTime}s.");
                    }

                    // check send time
                    Assert.AreEqual(lastPacketTime, expectedSendTime, maxSendTimeDelta);

                    // check number of (lost) packets
                    int numLost = data.Count - node2.ReceivedUcPackets.Count;
                    Assert.AreEqual(expectedNumLostPackets, numLost);

                    // dequeue all packets and check for errors
                    var receivedData = new List<byte[]>();
                    while (node2.ReceivedUcPackets.TryDequeue(out TestPacket p))
                    {
                        receivedData.Add(p.Data);
                    }
                    int numEffectiveBitErrors = TestDataCreator.ComputeBitErrors(receivedData);
                    Assert.AreEqual(expectedNumWrongBits, numEffectiveBitErrors);
                }
            }
            finally
            {
                node1.Dispose();
                node2.Dispose();
            }
        }
    }
}
