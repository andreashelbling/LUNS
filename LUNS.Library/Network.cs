using LUNS.Library.Networking;
using LUNS.Library.PacketIO;
using LUNS.Library.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace LUNS.Library
{
    /// <summary>
    /// Defines a network topology for the simulation of a UDP network and provides the UDP endpoints to use.
    /// </summary>
    public class Network
    {
        private readonly Dictionary<NodeId, List<Route>> _routes = new Dictionary<NodeId, List<Route>>(); // source node id ->> routes to target nodes.
        private readonly object _routesLock = new object();

        private readonly Dictionary<GroupId, Group> _groups = new Dictionary<GroupId, Group>();
        private readonly object _groupsLock = new object();

        private readonly object _mapsLock = new object();
        private readonly Dictionary<int, LogicalRoute> _sourcePeerPortToLogicalRoute = new Dictionary<int, LogicalRoute>(); // physical port -> logical route
        private readonly Dictionary<LogicalRoute, Peer> _logicalRouteToSourcePeer = new Dictionary<LogicalRoute, Peer>(); // logical route -> peer
        private readonly Dictionary<int, LogicalRoute> _targetPeerPortToLogicalRoute = new Dictionary<int, LogicalRoute>(); // physical port -> logical route
        private readonly Dictionary<LogicalRoute, Peer> _logicalRouteToTargetPeer = new Dictionary<LogicalRoute, Peer>(); // logical route -> peer

        // provider for the port numbers for nodes to listen on
        private readonly IPortProvider _portProvider;
        private readonly ILogger _logger;

        /// <summary>
        /// Create a new network definition for a "single application and multicast endpoint per node" deployment.
        /// </summary>
        /// <param name="nodeUnicastListenPort">The port the nodes are using to receive unicast packets.</param>
        /// <param name="nodeMulticastListenPort">The port the nodes are using to receive multicast packets.</param>
        /// <param name="logger">An optional logger implementation to use.</param>
        /// <remarks>The specified ports must be available on all addresses.</remarks>
        public Network(int nodeUnicastListenPort, int nodeMulticastListenPort, ILogger logger = null)
        {
            _portProvider = new DefaultPortProvider(nodeUnicastListenPort, nodeMulticastListenPort);
            _logger = logger;
        }

        /// <summary>
        /// Create a new network definition for a "multiple applications per node, each application with a single multicast endpoint" deployment.
        /// </summary>
        /// <param name="nodeUnicastListenPorts">The ports the nodes are using to receive unicast packets, one port per application.</param>
        /// <param name="nodeMulticastListenPorts">The ports the nodes are using to receive multicast packets, one port per application.</param>
        /// <param name="logger">An optional logger implementation to use.</param>
        /// <remarks>The specified ports must be available on all addresses.</remarks>
        public Network(int[] nodeUnicastListenPorts, int[] nodeMulticastListenPorts, ILogger logger = null)
        {
            _portProvider = new DefaultPortProvider(nodeUnicastListenPorts, nodeMulticastListenPorts);
            _logger = logger;
        }

        /// <summary>
        /// Create a new network definition with a custom port provider implementation.
        /// </summary>
        /// <param name="provider">The port provider to use.</param>
        /// <param name="logger">An optional logger implementation to use.</param>
        public Network(IPortProvider provider, ILogger logger = null)
        {
            _portProvider = provider;
            _logger = logger;
        }

        internal int NumApplications => _portProvider.NumApplications;

        /// <summary>
        /// Inserts a node in a group. Traffic directed to the specified group will be routed to application on the specified node.
        /// A node can only send to a group if it has joined the group.
        /// </summary>
        /// <param name="node">Identifies the node.</param>
        /// <param name="groupId">Identifies the group.</param>
        public void JoinMulticastGroup(NodeId node, GroupId groupId)
        {
            lock (_groupsLock)
            {
                if (!_groups.TryGetValue(groupId, out Group group))
                {
                    group = new Group(groupId);
                    _groups.Add(groupId, group);
                }
                group.Members.Add(node);
            }
        }

        /// <summary>
        /// Adds the definition for an ideal directed route in the network.
        /// </summary>
        /// <param name="source">Identifies the source node.</param>
        /// <param name="target">Identifies the target node.</param>
        public void AddRoute(NodeId source, NodeId target)
        {
            AddRoute(source, target, RouteSettings.Default);
        }

        /// <summary>
        /// Adds the definition for a directed route with specific quality settings in the network.
        /// </summary>
        /// <param name="source">Identifies the source node.</param>
        /// <param name="target">Identifies the target node.</param>
        /// <param name="settings">The quality settings to apply to the new route.</param>
        public void AddRoute(NodeId source, NodeId target, RouteSettings settings)
        {
            lock (_routesLock)
            {
                if (TryGetRoute(source, target, out Route route))
                {
                    _logger?.WarnFormat("A route from node {0} to node {1} already exists", source, target);
                    return;
                }

                if (!_routes.TryGetValue(source, out List<Route> routesFromSourceNode))
                {
                    routesFromSourceNode = new List<Route>();
                    _routes.Add(source, routesFromSourceNode);
                }
                routesFromSourceNode.Add(new Route(source, target, settings));
            }
        }

        internal void Send(byte[] data, Route route, LogicalRoute logicalRoute, RoutingDirection dir)
        {
            if (dir == RoutingDirection.Forward)
            {
                // determine target endpoint
                IPEndPoint remoteEP;
                if (logicalRoute.IsMulticast)
                {
                    remoteEP = GetLocalMulticastEndpoint(route.TargetNodeId, new GroupApplicationId(logicalRoute.TargetGroupId, logicalRoute.ApplicationIndex));
                }
                else
                {
                    remoteEP = GetLocalUnicastEndpoint(new NodeApplicationId(logicalRoute.TargetNodeId, logicalRoute.ApplicationIndex));
                }
                lock (_mapsLock)
                {
                    if (_logicalRouteToTargetPeer.TryGetValue(logicalRoute, out Peer peer))
                    {
                        peer.Send(data, remoteEP);
                    }
                    else
                    {
                        if (logicalRoute.IsMulticast)
                        {
                            _logger.WarnFormat("Application with index {0} cannot send data from node {1} to group {2} because no route from node {1} to node {3} exists.",
                                logicalRoute.ApplicationIndex, logicalRoute.SourceNodeId, logicalRoute.TargetGroupId, route.TargetNodeId);
                        }
                        else
                        {
                            _logger.WarnFormat("Application with index {0} cannot send data from node {1} to node {2} because no route exists.",
                                logicalRoute.ApplicationIndex, logicalRoute.SourceNodeId, logicalRoute.TargetNodeId);
                        }
                    }
                }
            }
            else
            {
                // backward routes are simpler since the peer has a return-to endpoint
                lock (_mapsLock)
                {
                    if(_logicalRouteToSourcePeer.TryGetValue(logicalRoute, out Peer peer))
                    { 
                        peer.SendBack(data);
                    }
                }
            }
        }

        private bool TryGetRoute(NodeId source, NodeId target, out Route route)
        {
            route = null;
            if (_routes.TryGetValue(source, out List<Route> routesFromSourceNode))
            {
                route = routesFromSourceNode.FirstOrDefault(r => r.TargetNodeId == target);
            }
            return route != null;
        }

        // The router must be able to detect the following information for an incoming packet:
        //   - target node or group
        //   - application
        //   - source node
        // and we don't want to introduce data in the UDP payload to remain transparent.
        // and we are constrained to the 127.0.0.x address range
        // and we don't want to add an additional protocol layer
        // -> All this information must be encoded into the port number. So there must be a port for each
        //   - sourceNode/targetNode/applicationIndex or route/applicationIndex combination.
        //   - sourceNode/targetGroup/applicationIndex combination.

        /// <summary>
        /// Create a communication peer for senders
        /// </summary>
        /// <param name="logicalRoute">The logical route (forward) for the peer.</param>
        /// <param name="packetReceiveAction">The action to invoke when a packet is received.</param>
        internal void CreateSourcePeer(LogicalRoute logicalRoute, Action<Packet> packetReceiveAction)
        {
            var peer = new Peer(new IPEndPoint(RouterAddress, 0));
            peer.PacketReceived += (s, e) => packetReceiveAction(e.Packet);

            lock (_mapsLock)
            {
                _sourcePeerPortToLogicalRoute.Add(peer.LocalEndPoint.Port, logicalRoute);
                _logicalRouteToSourcePeer.Add(logicalRoute, peer);
            }
            peer.Start();
        }

        /// <summary>
        /// Create a communication peer for receivers
        /// </summary>
        /// <param name="logicalRoute">The logical route (forward) for the peer.</param>
        /// <param name="packetReceiveAction">The action to invoke when a packet is received.</param>
        internal void CreateTargetPeer(LogicalRoute logicalRoute, Action<Packet> packetReceiveAction)
        {
            var peer = new Peer(new IPEndPoint(RouterAddress, 0));
            peer.PacketReceived += (s, e) => packetReceiveAction(e.Packet);

            lock (_mapsLock)
            {
                _targetPeerPortToLogicalRoute.Add(peer.LocalEndPoint.Port, logicalRoute);
                _logicalRouteToTargetPeer.Add(logicalRoute, peer);
            }
            peer.Start();
        }

        internal void DestroyPeers()
        {
            lock (_mapsLock)
            {
                foreach (Peer peer in _logicalRouteToSourcePeer.Values)
                {
                    peer.Dispose();
                }
                foreach (Peer peer in _logicalRouteToTargetPeer.Values)
                {
                    peer.Dispose();
                }
                _logicalRouteToSourcePeer.Clear();
                _sourcePeerPortToLogicalRoute.Clear();
                _logicalRouteToTargetPeer.Clear();
                _targetPeerPortToLogicalRoute.Clear();
            }
        }

        /* Note on endpoints
           -----------------
           Since outgoing IP and port are typically set automatically, we cannot identify a sender from the source endpoint
           when receiving a datagram and assume the IP will be in the 127.0.0.0/8 range. All routing information required in
           encoded in the router's port numbers, see Router.Start() and above Bind() methods.
        
           For diagnostic and analysis purpose, the local endpoints will use a specific localhost IP address for each node.
        */

        internal IPAddress RouterAddress => ConstructRemoteAddress(NodeId.Router);

        /// <summary>
        /// Get a local unicast endpoint for a node.
        /// </summary>
        /// <param name="nodeAppId">The id of the application/node for which to get the local endpoint.</param>
        /// <returns>The local endpoint for the specified node.</returns>
        public IPEndPoint GetLocalUnicastEndpoint(NodeApplicationId nodeAppId)
        {
            int port = _portProvider.GetUnicastPort(nodeAppId.ApplicationIndex);
            return new IPEndPoint(ConstructRemoteAddress(nodeAppId.NodeId), port);
        }

        /// <summary>
        /// Get a local multicast endpoint for a node.
        /// </summary>
        /// <param name="localNode">The node for which the multicast endpoint shall be retrieved.</param>
        /// <param name="groupAppId">The id of the application/group for which to get the local endpoint.</param>
        /// <returns>The local endpoint for the specified application and group on the specified node.</returns>
        public IPEndPoint GetLocalMulticastEndpoint(NodeId localNode, GroupApplicationId groupAppId)
        {
            int port = _portProvider.GetMulticastPort(groupAppId);
            return new IPEndPoint(ConstructRemoteAddress(localNode), port);
        }

        /// <summary>
        /// Get the unicast endpoint for a remote node.
        /// </summary>
        /// <param name="sourceNodeId">The node id of the source node.</param>
        /// <param name="nodeAppId">The target node and application id for which to get the endpoint.</param>
        /// <returns>The endpoint to reach a node/application from the source node via unicast or null if the node cannot be reached (there is no route).</returns>
        public IPEndPoint GetRemoteEndpoint(NodeId sourceNodeId, NodeApplicationId nodeAppId)
        {
            var logicalRoute = new LogicalRoute(sourceNodeId, nodeAppId.NodeId, nodeAppId.ApplicationIndex);
            lock (_mapsLock)
            {
                if (_logicalRouteToSourcePeer.TryGetValue(logicalRoute, out Peer peer))
                {
                    return peer.LocalEndPoint;
                }
                return null;
            }
        }

        /// <summary>
        /// Get the multicast endpoint for a group.
        /// </summary>
        /// <param name="sourceNodeId">The node id of the source node.</param>
        /// <param name="groupAppId">The target group and application id for which to get the endpoint.</param>
        /// <returns>The endpoint to reach a group/application from the source node via multicast or null if the group does not exist.</returns>
        public IPEndPoint GetRemoteEndpoint(NodeId sourceNodeId, GroupApplicationId groupAppId)
        {
            var logicalRoute = new LogicalRoute(sourceNodeId, groupAppId.GroupId, groupAppId.ApplicationIndex);
            lock (_mapsLock)
            {
                if (_logicalRouteToSourcePeer.TryGetValue(logicalRoute, out Peer peer))
                {
                    return peer.LocalEndPoint;
                }
                return null;
            }
        }

        private IPAddress ConstructRemoteAddress(NodeId nodeId)
        {
            // offset by 10 (see note about Address above)
            return IPAddress.Parse("127.0.0." + (nodeId.Index + 10));
        }

        private NodeId DeconstructAddress(IPAddress address)
        {
            byte lsb = address.GetAddressBytes()[3];
            if (lsb <= 10)
            {
                return NodeId.Undefined;
            }

            lsb -= 10; // undo offset
            if (lsb == NodeId.Router.Index)
            {
                return NodeId.Router;
            }

            return new NodeId(lsb);
        }

        /// <summary>
        /// Gets all defined routes.
        /// </summary>
        internal IEnumerable<Route> GetRoutes()
        {
            lock (_routesLock)
            {
                return _routes.SelectMany(kvp => kvp.Value).ToList();
            }
        }

        /// <summary>
        /// Gets all nodes to where there is a route
        /// </summary>
        internal IEnumerable<NodeId> GetNodeIds()
        {
            return _routes.Values.SelectMany(r => r).Select(r => r.TargetNodeId).Distinct();
        }

        /// <summary>
        /// Gets the groups to which nodes have joined.
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<Group> GetGroups()
        {
            lock (_groupsLock)
            {
                return _groups.Values.ToList();
            }
        }

        internal IEnumerable<Route> GetRoutes(RoutingDirection dir, Packet packet, out LogicalRoute logicalRoute)
        {
            var result = new List<Route>();
            logicalRoute = null;

            lock (_mapsLock)
            {
                if (dir == RoutingDirection.Forward)
                {
                    _sourcePeerPortToLogicalRoute.TryGetValue(packet.TargetEndPoint.Port, out logicalRoute);
                }
                else
                {
                    _targetPeerPortToLogicalRoute.TryGetValue(packet.TargetEndPoint.Port, out logicalRoute);
                }
            }

            if (logicalRoute == null)
            {
                _logger.WarnFormat("Packet with size {0}, sent from endpoint {1} to endpoint {2} cannot be routed since no route exists.",
                    packet.Data.Length, packet.SourceEndPoint, packet.TargetEndPoint);
                return result;
            }

            // The logical route is always in FORWARD direction, i.e. from source to target, even when the target sends back a response.
            // -> in the backward direction, we have to check the packets' source endpoint.

            if (logicalRoute.IsMulticast)
            {
                // determine the originator of the packet
                NodeId originatorId;
                if (dir == RoutingDirection.Forward)
                {
                    originatorId = logicalRoute.SourceNodeId;
                }
                else
                {
                    originatorId = DeconstructAddress(packet.SourceEndPoint.Address);
                }

                // get all potentially targeted nodes in the MC group
                lock (_groupsLock)
                {
                    if (_groups.TryGetValue(logicalRoute.TargetGroupId, out Group group))
                    {
                        // get all routes from th source node to the nodes in the MC group
                        lock (_routesLock)
                        {
                            if (_routes.TryGetValue(originatorId, out List<Route> routesFromSource))
                            {
                                // Check which routes from source go to one of the mc group nodes
                                result.AddRange(routesFromSource.Where(r => group.Members.Contains(r.TargetNodeId)));
                            }
                        }
                    }
                }
            }
            else
            {
                // determine the originator and destination of the packet
                NodeId originatorId;
                NodeId destinationId;
                if (dir == RoutingDirection.Forward)
                {
                    originatorId = logicalRoute.SourceNodeId;
                    destinationId = logicalRoute.TargetNodeId;
                }
                else
                {
                    originatorId = DeconstructAddress(packet.SourceEndPoint.Address);
                    destinationId = logicalRoute.SourceNodeId;
                }

                lock (_routesLock)
                {

                    if (_routes.TryGetValue(originatorId, out List<Route> routesFromSource))
                    {
                        result.AddRange(routesFromSource.Where(r => r.TargetNodeId == destinationId));
                    }
                }
            }

            return result;
        }
    }
}