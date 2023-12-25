using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using LUNS.Library;
using LUNS.Tests.Util;

namespace LUNS.Tests
{
    [TestClass]
    public class RouterMulticastLossyTests
    {
        [TestMethod]
        public void SlowRouteSinglePacketTest()
        {
            RouterMulticastLossyTest(
                bitRate2: 300.0, packetLossRate2: 0.0, bitErrorRate2: 0.0,
                bitRate3: 100.0, packetLossRate3: 0.0, bitErrorRate3: 0.0,
                dataLength: 300, segmentSize: 1000,
                expectedSendTime2: 8.0, maxSendTimeDelta2: 0.5, expectedNumLostPackets2: 0, expectedNumWrongBits2: 0,
                expectedSendTime3: 24.0, maxSendTimeDelta3: 1.5, expectedNumLostPackets3: 0, expectedNumWrongBits3: 0);
        }

        [TestMethod]
        public void SlowRouteMultiPacketTest()
        {
            RouterMulticastLossyTest(
                bitRate2: 20000.0, packetLossRate2: 0.0, bitErrorRate2: 0.0,
                bitRate3: 10000.0, packetLossRate3: 0.0, bitErrorRate3: 0.0,
                dataLength: 10000, segmentSize: 1000,
                expectedSendTime2: 4.0, maxSendTimeDelta2: 0.25, expectedNumLostPackets2: 0, expectedNumWrongBits2: 0,
                expectedSendTime3: 8.0, maxSendTimeDelta3: 0.5, expectedNumLostPackets3: 0, expectedNumWrongBits3: 0);
        }

        [TestMethod]
        public void PacketLossTest()
        {
            // Send 10'000 packets with loss rate of 10%/1% -> 1000/100 packets should be lost. With the default seed, we get 1002/108
            RouterMulticastLossyTest(
                bitRate2: 160000.0, packetLossRate2: 0.1, bitErrorRate2: 0.0,
                bitRate3:  80000.0, packetLossRate3: 0.01, bitErrorRate3: 0.0,
                dataLength: 10000 * 16, segmentSize: 16,
                expectedSendTime2: 8.0, maxSendTimeDelta2: 1.0, expectedNumLostPackets2: 1002, expectedNumWrongBits2: 0,
                expectedSendTime3: 16.0, maxSendTimeDelta3: 2.0, expectedNumLostPackets3: 108, expectedNumWrongBits3: 0);
        }

        [TestMethod]
        public void BitErrorTest()
        {
            // Send 80'000 bits with an error rate of 10%/1% -> 8000/800 bits should be inverted. With the default seed, we get 8058/824
            RouterMulticastLossyTest(
                bitRate2: 10000.0, packetLossRate2: 0.0, bitErrorRate2: 0.1,
                bitRate3:  5000.0, packetLossRate3: 0.0, bitErrorRate3: 0.01,
                dataLength: 10 * 1000, segmentSize: 1000,
                expectedSendTime2: 8.0, maxSendTimeDelta2: 1.0, expectedNumLostPackets2: 0, expectedNumWrongBits2: 8058,
                expectedSendTime3: 16.0, maxSendTimeDelta3: 2.0, expectedNumLostPackets3: 0, expectedNumWrongBits3: 824);
        }

        private void RouterMulticastLossyTest(double bitRate2, double packetLossRate2, double bitErrorRate2,
            double bitRate3, double packetLossRate3, double bitErrorRate3,
            uint dataLength, uint segmentSize,
            double expectedSendTime2, double maxSendTimeDelta2, int expectedNumLostPackets2, int expectedNumWrongBits2,
            double expectedSendTime3, double maxSendTimeDelta3, int expectedNumLostPackets3, int expectedNumWrongBits3)
        {
            var net = new Network(31000, 31001);
            var mcGroupId = new GroupId(1);

            ApplicationOnNode node1 = ApplicationOnNode.Create(net, new NodeId(1));
            ApplicationOnNode node2 = ApplicationOnNode.Create(net, new NodeId(2), mcGroupId);
            ApplicationOnNode node3 = ApplicationOnNode.Create(net, new NodeId(3), mcGroupId);

            GroupApplicationId group = new GroupApplicationId(mcGroupId);

            try
            {
                net.AddRoute(node1.NodeAppId.NodeId, node2.NodeAppId.NodeId, new ConstantRouteSettings(bitRate2, packetLossRate2, bitErrorRate2));
                net.AddRoute(node1.NodeAppId.NodeId, node3.NodeAppId.NodeId, new ConstantRouteSettings(bitRate3, packetLossRate3, bitErrorRate3));

                net.JoinMulticastGroup(node1.NodeAppId.NodeId, mcGroupId);
                net.JoinMulticastGroup(node2.NodeAppId.NodeId, mcGroupId);
                net.JoinMulticastGroup(node3.NodeAppId.NodeId, mcGroupId);

                using (var router = new Router(net))
                {
                    router.Start();

                    List<byte[]> data = TestDataCreator.CreateTestData(dataLength, segmentSize);
                    var sw = new Stopwatch();
                    sw.Start();
                    foreach (byte[] segment in data)
                    {
                        node1.Send(segment, net.GetRemoteEndpoint(node1.NodeAppId.NodeId, group));
                    }

                    // wait until received packet count is as expected or a timeout occurred on both routes
                    int lastReceivedPacketCount2 = 0;
                    int lastReceivedPacketCount3 = 0;
                    double lastPacketTime2 = -1.0;
                    double lastPacketTime3 = -1.0;
                    //while ((sw.Elapsed.TotalSeconds < 2 * expectedSendTime2 && node2.ReceivedMcPackets.Count < (data.Count - expectedNumLostPackets2))
                    //    || (sw.Elapsed.TotalSeconds < 2 * expectedSendTime3 && node3.ReceivedMcPackets.Count < (data.Count - expectedNumLostPackets3)))
                    while (sw.Elapsed.TotalSeconds < 2 * Math.Max(expectedSendTime2, expectedSendTime3))
                    {
                        if (node2.ReceivedMcPackets.Count > lastReceivedPacketCount2)
                        {
                            lastPacketTime2 = sw.Elapsed.TotalSeconds;
                            lastReceivedPacketCount2 = node2.ReceivedMcPackets.Count;
                        }
                        if (node3.ReceivedMcPackets.Count > lastReceivedPacketCount3)
                        {
                            lastPacketTime3 = sw.Elapsed.TotalSeconds;
                            lastReceivedPacketCount3 = node3.ReceivedMcPackets.Count;
                        }
                        Thread.Sleep(1);
                    }
                    if (lastReceivedPacketCount2 != node2.ReceivedMcPackets.Count)
                    {
                        lastPacketTime2 = sw.Elapsed.TotalSeconds;
                        lastReceivedPacketCount2 = node2.ReceivedMcPackets.Count;
                    }
                    if (lastReceivedPacketCount3 != node3.ReceivedMcPackets.Count)
                    {
                        lastPacketTime3 = sw.Elapsed.TotalSeconds;
                        lastReceivedPacketCount3 = node3.ReceivedMcPackets.Count;
                    }
                    sw.Stop();

                    if (lastPacketTime2 >= 1.5 * expectedSendTime2)
                    {
                        Assert.Fail($"Not all packets were received within 1.5x the expected time of {expectedSendTime2}s from node 2.");
                    }
                    if (lastPacketTime3 >= 1.5 * expectedSendTime3)
                    {
                        Assert.Fail($"Not all packets were received within 1.5x the expected time of {expectedSendTime3}s from node 3.");
                    }

                    // check send time(s)
                    Assert.AreEqual(lastPacketTime2, expectedSendTime2, maxSendTimeDelta2);
                    Assert.AreEqual(lastPacketTime3, expectedSendTime3, maxSendTimeDelta3);

                    // check number of (lost) packets
                    int numLost2 = data.Count - node2.ReceivedMcPackets.Count;
                    Assert.AreEqual(expectedNumLostPackets2, numLost2);
                    int numLost3 = data.Count - node3.ReceivedMcPackets.Count;
                    Assert.AreEqual(expectedNumLostPackets3, numLost3);

                    // dequeue all packets and check for errors
                    var receivedData2 = new List<byte[]>();
                    while (node2.ReceivedMcPackets.TryDequeue(out TestPacket p))
                    {
                        receivedData2.Add(p.Data);
                    }
                    int numEffectiveBitErrors2 = TestDataCreator.ComputeBitErrors(receivedData2);
                    Assert.AreEqual(expectedNumWrongBits2, numEffectiveBitErrors2);
                    var receivedData3 = new List<byte[]>();
                    while (node3.ReceivedMcPackets.TryDequeue(out TestPacket p))
                    {
                        receivedData3.Add(p.Data);
                    }
                    int numEffectiveBitErrors3 = TestDataCreator.ComputeBitErrors(receivedData3);
                    Assert.AreEqual(expectedNumWrongBits3, numEffectiveBitErrors3);
                }
            }
            finally
            {
                node1.Dispose();
                node2.Dispose();
                node3.Dispose();
            }
        }
    }
}
