using LUNS.Library;
using LUNS.Tests.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace LUNS.Tests
{
    [TestClass]
    public class RouterTemporalSettingsTests
    {
        [TestMethod]
        public void VariableSendRateTest()

        {
            RouterTemporalSettingsTest(new ConstantRouteSettings(1000), new ConstantRouteSettings(10000), 30.0,
                20*1000, 1000, 
                new ConstantRouteSettings(996, 0.0, 0.0), new ConstantRouteSettings(9700, 0.0, 0.0));
        }

        /* Manual inspection of the measured packte loss values looks ok. Automated checking requires some sort of simple linear regression
        [TestMethod]
        public void VariablePacketLossTest()
        {
            RouterTemporalSettingsTest(new ConstantRouteSettings(100000, 0.01, 0.0), new ConstantRouteSettings(100000, 0.1, 0.0), 30.0,
                1000 * 1000, 1000,
                new ConstantRouteSettings(97000, 0.01, 0.0), new ConstantRouteSettings(97000, 0.1, 0.0));
        }
        */

        [TestMethod]
        public void VariableBitErrorRateTest()
        {
            RouterTemporalSettingsTest(new ConstantRouteSettings(10000, 0.0, 0.001), new ConstantRouteSettings(10000, 0.0, 0.01), 30.0,
                100 * 1000, 1000,
                new ConstantRouteSettings(9700, 0.0, 0.001), new ConstantRouteSettings(9700, 0.0, 0.01));
        }

        private void RouterTemporalSettingsTest(
            ConstantRouteSettings s1, ConstantRouteSettings s2, double relativeSwitchTime,
            uint dataLength, uint segmentSize,
            ConstantRouteSettings expectedMeasuredSettings1, ConstantRouteSettings expectedMeasuredSettings2)
        {
            var net = new Network(31000, 31001);

            ApplicationOnNode node1 = ApplicationOnNode.Create(net, new NodeId(1));
            ApplicationOnNode node2 = ApplicationOnNode.Create(net, new NodeId(2));

            try
            {
                var temporalSetting = new TemporalRouteSettings();
                temporalSetting.Add(s1);
                temporalSetting.Add(s2, relativeSwitchTime);
                net.AddRoute(node1.NodeAppId.NodeId, node2.NodeAppId.NodeId, temporalSetting);

                /*
                    The route settings have impact when the packet is sent to the router and queued there.
                    It is therefore necessary to send/receive each packet on by one to measure the change in route settings.
                 */
                using (var router = new Router(net))
                {
                    router.Start();

                    List<byte[]> data = TestDataCreator.CreateTestData(dataLength, segmentSize);
                    var sw = new Stopwatch();
                    sw.Start();

                    var measuredSettings = new List<ConstantRouteSettings>();
                    TimeSpan sendTime;
                    TimeSpan maxTransferTime = TimeSpan.FromSeconds(1.5 * 8 * segmentSize / Math.Min(s1.BitRate, s2.BitRate)); // 1.5 -> safety margin due to performance impacts
                    int numReceivedPackets = 0;
                    foreach (byte[] segment in data)
                    {
                        node1.Send(segment, net.GetRemoteEndpoint(node1.NodeAppId.NodeId, node2.NodeAppId));
                        sendTime = sw.Elapsed;

                        TestPacket p = null;
                        // wait long enough
                        while (sw.Elapsed - sendTime < maxTransferTime) 
                        {
                            if (node2.ReceivedUcPackets.TryDequeue(out p))
                            {
                                break;
                            }
                            Thread.Sleep(1);
                        }

                        if (p != null)
                        {
                            // if a packet is received, compute send rate and bit error rate
                            TimeSpan transferTime = sw.Elapsed - sendTime;
                            double bitRate = p.Data.Length * 8 / (transferTime).TotalSeconds;

                            // estimate packet loss.
                            int packetNumber = TestDataCreator.GetPacketNumber(p.Data);
                            //double packetLossRate = 1.0 - ((double)(numReceivedPackets + 1) / (double)(packetNumber + 1));
                            double packetLossRate = packetNumber - numReceivedPackets; // use number of lost packets which is better suited to check the results
                            numReceivedPackets++;
                            // todo: use linear interpolation and check accuracy to break into segments.

                            // estimate bit error rate
                            var receivedData = new List<byte[]>();
                            receivedData.Add(p.Data);
                            double bitErrorRate = (double)TestDataCreator.ComputeBitErrors(receivedData) / (double)(p.Data.Length * 8);

                            var measurement = new ConstantRouteSettings(bitRate, packetLossRate, bitErrorRate);
                            measuredSettings.Add(measurement);
                        }
                    }

                    sw.Stop();

                    // for the mesured stats, see if the data:
                    // - has a bisection with a high enough separation -> Histogram Difference?
                    // - seperates into the two values estimated as expected reult
                    var settingsSegments = new List<ConstantRouteSettings>();
                    var currentSummedSettings = new ConstantRouteSettings(0.0);
                    var currentAverage = new ConstantRouteSettings(0.0);
                    int counter = 0;
                    foreach (ConstantRouteSettings s in measuredSettings)
                    {
                        if (counter > 0)
                        {
                            // detect large relative change
                            if (s.BitRate > currentAverage.BitRate * 2.0 || s.BitRate < currentAverage.BitRate * 0.5
                                || s.PacketLossRate > currentAverage.PacketLossRate * 5.0 || s.PacketLossRate < currentAverage.PacketLossRate * 0.2
                                || s.BitErrorRate > currentAverage.BitErrorRate * 5.0 || s.BitErrorRate < currentAverage.BitErrorRate * 0.2)
                            {
                                settingsSegments.Add(currentAverage);

                                // reset for next segment
                                currentSummedSettings = new ConstantRouteSettings(0.0);
                                currentAverage = new ConstantRouteSettings(0.0);
                                counter = 0;
                            }
                        }

                        counter++;
                        currentSummedSettings.BitRate += s.BitRate;
                        currentSummedSettings.PacketLossRate += s.PacketLossRate;
                        currentSummedSettings.BitErrorRate += s.BitErrorRate;
                        currentAverage = new ConstantRouteSettings(
                            currentSummedSettings.BitRate / (double)counter,
                            currentSummedSettings.PacketLossRate / (double)counter, 
                            currentSummedSettings.BitErrorRate / (double)counter);

                    }
                    settingsSegments.Add(currentAverage);

                    Assert.AreEqual(2, settingsSegments.Count);
                    Assert.AreEqual(expectedMeasuredSettings1.BitRate, settingsSegments[0].BitRate, expectedMeasuredSettings1.BitRate * 0.01);
                    Assert.AreEqual(expectedMeasuredSettings1.PacketLossRate, settingsSegments[0].PacketLossRate, expectedMeasuredSettings1.PacketLossRate * 0.01);
                    Assert.AreEqual(expectedMeasuredSettings1.BitErrorRate, settingsSegments[0].BitErrorRate, expectedMeasuredSettings1.BitErrorRate * 0.1);
                    Assert.AreEqual(expectedMeasuredSettings2.BitRate, settingsSegments[1].BitRate, expectedMeasuredSettings2.BitRate * 0.01);
                    Assert.AreEqual(expectedMeasuredSettings2.PacketLossRate, settingsSegments[1].PacketLossRate, expectedMeasuredSettings2.PacketLossRate);
                    Assert.AreEqual(expectedMeasuredSettings2.BitErrorRate, settingsSegments[1].BitErrorRate, expectedMeasuredSettings2.BitErrorRate * 0.1);
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
