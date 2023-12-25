using LUNS.Library.Networking;
using LUNS.Library.PacketIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace LUNS.Library.Routing
{
    internal class RouteQueue : IDisposable
    {
        private Route _route;
        private readonly Thread _workerThread;
        private readonly ManualResetEvent _abortSignal;
        private readonly Network _network;
        private readonly Queue<PacketRoutingInfo> _packetQueue; // high performance FIFO for packets
        private TimeSpan _lastPacketRelativeForwardTime; // the relative time when the packet at the end of the queue is forwarded (prevent lookup)
        private readonly object _lock;
        private readonly Stopwatch _stopWatch;
        private readonly Random _random;

#if DEBUG
        private int _numPacketsQueued;    // received     in the router
        private int _numPacketsDropped;   // dropped      "
        private int _numBitsFlipped;      // bits flipped "
        private int _numPacketsForwarded; // sent to the target node(s)
#endif

        public RouteQueue(Route route, int seed, IPAddress routerAddress, Network network)
        {
            _lock = new object();
            _route = route;
            _network = network;
            _random = new Random(seed);
            _packetQueue = new Queue<PacketRoutingInfo>();
            _lastPacketRelativeForwardTime = TimeSpan.Zero;
            _abortSignal = new ManualResetEvent(false);
            _stopWatch = new Stopwatch();
            _stopWatch.Start();
            _workerThread = new Thread(WorkerMethod);
            _workerThread.Start();
        }

        internal void Enqueue(Packet packet, LogicalRoute logicalRoute, RoutingDirection dir, double relativeTime)
        {
            // roll the packet-loss-dice: is the packet lost?
            // enqueue the (lost packet) anyway so the transmit delay is calculated correctly.
            bool isLost = DoesRandomEventOccur(_route.Settings.GetPacketLossRate(relativeTime));

            // roll the bit-error-dice for each bit in the packet -> flip bits
            for (int bit = 0; bit < packet.Data.Length * 8; bit++)
            {
                if (DoesRandomEventOccur(_route.Settings.GetBitErrorRate(relativeTime)))
                {
                    int byteIndex = bit / 8;
                    byte mask = (byte)(1 << (bit % 8));
                    packet.Data[byteIndex] ^= mask;
#if DEBUG
                    _numBitsFlipped++;
#endif
                }
            }

            // enqueue the packet with the exact current relative time
            lock (_lock)
            {
                // Enqueue in Router Thread   ->  Forward in RouteQueue Thread
                // [p1]
                //      [p2]                     ____ relative forward time for p1
                //           [p3]               /                               ___ relative forward time for p2
                // ¦...........delay...........[p1]                            /
                //                                 ¦...........delay...........[p2]
                //                                                                 ¦...........delay...........[p3]
                // --> Time
                //
                // The delay is the simulated packet transmit time, given by the packet length and the bitrate of the route
                // Assume that effective transmit time of a single packet is negligible

                TimeSpan packetTransmitTime = TimeSpan.FromSeconds((packet.Data.Length * 8.0) / _route.Settings.GetBitRate(relativeTime));
                TimeSpan relativeForwardTime;
                if (_packetQueue.Count == 0)
                {
                    relativeForwardTime = _stopWatch.Elapsed + packetTransmitTime;
                }
                else
                {
                    relativeForwardTime = _lastPacketRelativeForwardTime + packetTransmitTime;
                }

                _lastPacketRelativeForwardTime = relativeForwardTime;
                _packetQueue.Enqueue(new PacketRoutingInfo(packet, logicalRoute, dir, relativeForwardTime, isLost));
#if DEBUG
                _numPacketsQueued++;
#endif
            }
        }

        internal bool DoesRandomEventOccur(double targetRate)
        {
            // Random.NextDouble() is in [0..1[, we have to be careful when the rate of the route is 0
            if (targetRate == 0.0) return false;
            if (targetRate == 1.0) return true;

            return _random.NextDouble() <= targetRate;
        }

        private void WorkerMethod()
        {
            while (true)
            {
                if (_abortSignal.WaitOne(1))
                {
                    return;
                }

                lock(_lock)
                {
                    while(_packetQueue.Count > 0)
                    {
                        PacketRoutingInfo info = _packetQueue.Peek();
                        if (info.RelativeForwardTime > _stopWatch.Elapsed)
                        {
                            // packets are in order, so we can abort if we found a packet which is not yet ready to be forwarded
                            break;
                        }
                        // remove the object we peeked on
                        _packetQueue.Dequeue();

                        // forward packet to target node
                        if (!info.IsLost)
                        {
                            _network.Send(info.Packet.Data, _route, info.LogicalRoute, info.Direction);
                        }
#if DEBUG
                        if (!info.IsLost)
                        {
                            _numPacketsForwarded++;
                        }
                        else
                        {
                            _numPacketsDropped++;
                        }
#endif
                    }
                }
            }
        }

        public void Dispose()
        {
            _abortSignal.Set();
            _workerThread.Join();
        }
    }
}