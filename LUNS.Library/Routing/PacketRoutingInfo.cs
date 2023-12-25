using LUNS.Library.PacketIO;
using System;

namespace LUNS.Library.Routing
{
    internal class PacketRoutingInfo
    {
        public PacketRoutingInfo(Packet packet, LogicalRoute logicalRoute, RoutingDirection dir, TimeSpan relativeForwardTime, bool isLost)
        {
            Packet = packet;
            LogicalRoute = logicalRoute;
            Direction = dir;
            RelativeForwardTime = relativeForwardTime;
            IsLost = isLost;
        }

        public TimeSpan RelativeForwardTime { get; }

        public Packet Packet { get; }

        public LogicalRoute LogicalRoute { get; }

        public RoutingDirection Direction { get; }

        public bool IsLost { get; }
    }
}