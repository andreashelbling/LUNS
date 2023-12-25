using LUNS.Library.Networking;
using LUNS.Library.PacketIO;
using LUNS.Library.Routing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace LUNS.Library
{
    /// <summary>
    /// Manages all UDP traffic within the simulated UDP network, according the the <see cref="Network"/> and its routes.
    /// </summary>
    public class Router : IDisposable
    {
        private readonly object _routeQueueLock = new object();
        private readonly Dictionary<ushort, RouteQueue> _routeQueues; // route id -> route queue

        private readonly Network _network;
        private readonly int _seed;

        private readonly Stopwatch _watch;

        /// <summary>
        /// Creates a new rouer.
        /// </summary>
        /// <param name="network">The network in which routing shall be performed.</param>
        /// <param name="seed">An optional random seed. Using the same seed results in the same packet loss and bit error patterns.</param>
        public Router(Network network, int seed = 1947633571)
        {
            if (network == null) 
                throw new ArgumentNullException(nameof(network));

            _network = network;
            _seed = seed;
            _routeQueues = new Dictionary<ushort, RouteQueue>();
            _watch = new Stopwatch();
        }

        /// <summary>
        /// Sztarts routing packets in the UDP network.
        /// </summary>
        public void Start()
        {
            // For each route, create a peer for senders to communicate with and also a peer for communicating with receivers.
            foreach (Route route in _network.GetRoutes())
            {
                for (byte applicationIndex = 0; applicationIndex < _network.NumApplications; applicationIndex++)
                {
                    var logicalRoute = new LogicalRoute(route.SourceNodeId, route.TargetNodeId, applicationIndex);
                    _network.CreateSourcePeer(logicalRoute, RoutePacketForward);
                    _network.CreateTargetPeer(logicalRoute, RoutePacketBackward);
                }
            }

            // For each multicast group, create a peer for senders to communicate with and also a peer for communicating with receivers.
            foreach (Group group in _network.GetGroups())
            {
                foreach (NodeId nodeId in group.Members)
                {
                    for (byte applicationIndex = 0; applicationIndex < _network.NumApplications; applicationIndex++)
                    {
                        var logicalRoute = new LogicalRoute(nodeId, group.Id, applicationIndex);
                        _network.CreateSourcePeer(logicalRoute, RoutePacketForward);
                        _network.CreateTargetPeer(logicalRoute, RoutePacketBackward);
                    }
                }
            }

            _watch.Start();
        }

        // forward route
        internal void RoutePacketForward(Packet packet)
        {
            RoutePacket(packet, RoutingDirection.Forward);
        }

        // backward route
        internal void RoutePacketBackward(Packet packet)
        {
            RoutePacket(packet, RoutingDirection.Backward);
        }

        private void RoutePacket(Packet packet, RoutingDirection dir)
        {
            foreach (Route route in _network.GetRoutes(dir, packet, out LogicalRoute logicalRoute))
            {
                // Queue a copy of the packet for processing (packet data may be modified on each route differently
                var p = new Packet(packet);
                QueuePacket(p, route, logicalRoute, dir);
            }
        }

        private void QueuePacket(Packet packet, Route route, LogicalRoute logicalRoute, RoutingDirection dir)
        {
            lock (_routeQueueLock)
            {
                if (!_routeQueues.TryGetValue(route.UniqueId, out RouteQueue routeQueue))
                {
                    routeQueue = new RouteQueue(route, _seed, _network.RouterAddress, _network);
                    _routeQueues.Add(route.UniqueId, routeQueue);
                }
                routeQueue.Enqueue(packet, logicalRoute, dir, _watch.Elapsed.TotalSeconds);
            }
        }

        /// <summary>
        /// Stops routing packets in the UDP network and frees all allocated resources.
        /// </summary>
        public void Dispose()
        {
            lock (_routeQueueLock)
            {
                foreach (RouteQueue rq in _routeQueues.Values)
                {
                    rq.Dispose();
                }
                _routeQueues.Clear();
            }

            _network.DestroyPeers();
        }
    }
}