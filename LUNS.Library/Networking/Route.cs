using System;

namespace LUNS.Library.Networking
{
    /// <summary>
    /// Model for a defined route from one node to another
    /// </summary>
    internal class Route
    {
        public Route(NodeId sourceNodeId, NodeId targetNodeId, RouteSettings settings)
        {
            SourceNodeId = sourceNodeId;
            TargetNodeId = targetNodeId;
            Settings = settings;
        }

        public NodeId SourceNodeId { get; }

        public NodeId TargetNodeId { get; }

        public ushort UniqueId => BitConverter.ToUInt16(new byte[] { SourceNodeId.Index, TargetNodeId.Index }, 0);

        public RouteSettings Settings { get; }
    }
}