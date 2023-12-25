using System;

namespace LUNS.Library.Routing
{
    internal class LogicalRoute : IComparable, IEquatable<LogicalRoute>
    {
        public LogicalRoute(NodeId source, NodeId target, byte applicationIndex)
        {
            SourceNodeId = source;
            TargetNodeId = target;
            TargetGroupId = GroupId.None;
            ApplicationIndex = applicationIndex;
            Id = BitConverter.ToUInt32(new byte[] { SourceNodeId.Index, TargetNodeId.Index, 0, ApplicationIndex }, 0);
        }

        public LogicalRoute(NodeId source, GroupId target, byte applicationIndex)
        {
            SourceNodeId = source;
            TargetNodeId = NodeId.Undefined;
            TargetGroupId = target;
            ApplicationIndex = applicationIndex;
            Id = BitConverter.ToUInt32(new byte[] { SourceNodeId.Index, 0, TargetGroupId.Index, ApplicationIndex }, 0);
        }

        public LogicalRoute(uint id)
        {
            byte[] bytes = BitConverter.GetBytes(id);
            SourceNodeId = new NodeId(bytes[0]);

            byte nodeId = bytes[1];
            if (nodeId != NodeId.Undefined.Index)
            {
                TargetNodeId = new NodeId(nodeId);
            }
            else
            {
                TargetNodeId = NodeId.Undefined;
            }

            byte groupId = bytes[2];
            if (groupId != GroupId.None.Index)
            {
                TargetGroupId = new GroupId(groupId);
            }
            else
            {
                TargetGroupId = GroupId.None;
            }    

            ApplicationIndex = bytes[3];
            Id = id;
        }

        public NodeId SourceNodeId { get; }

        public NodeId TargetNodeId { get; }

        public GroupId TargetGroupId { get; }

        public byte ApplicationIndex { get; }

        public bool IsMulticast => TargetGroupId != GroupId.None;

        public uint Id { get; }

        public static bool operator ==(LogicalRoute n1, LogicalRoute n2)
        {
            if (ReferenceEquals(n1, null) && ReferenceEquals(n2, null)) return true;
            if (ReferenceEquals(n1, null) || ReferenceEquals(n2, null)) return false;
            return n1.Id == n2.Id;
        }

        /// <summary>
        /// Compares two <see cref="NodeId"/> instances for value inequality./>
        /// </summary>
        /// <param name="n1">The first instance to compare.</param>
        /// <param name="n2">The second instance to compare.</param>
        /// <returns>True if the two instances are not equal by value.</returns>
        public static bool operator !=(LogicalRoute n1, LogicalRoute n2)
        {
            if (ReferenceEquals(n1, null) && ReferenceEquals(n2, null)) return false;
            if (ReferenceEquals(n1, null) || ReferenceEquals(n2, null)) return true;
            return n1.Id != n2.Id;
        }

        /// <summary>
        /// Checks if this instance has a value equality to another object.
        /// </summary>
        /// <param name="obj">The object to compare this instance to.</param>
        /// <returns>True if the object and this instance have the same value.</returns>
        public override bool Equals(object obj)
        {
            if (obj is LogicalRoute otherRoute)
            {
                return otherRoute.Id == Id;
            }
            return false;
        }

        /// <summary>
        /// Retiurns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            if (obj is LogicalRoute r)
            {
                return Id.CompareTo(r.Id);
            }
            return -1;
        }

        public bool Equals(LogicalRoute other)
        {
            return other.Id == Id;
        }
    }
}