using System.Collections.Generic;

namespace LUNS.Library.Networking
{
    /// <summary>
    /// Model for a defined multicast group with a list of nodes that have joined it.
    /// </summary>
    internal class Group
    {
        public Group(GroupId groupId)
        {
            Id = groupId;
            Members = new List<NodeId>();
        }

        public GroupId Id { get; }

        public List<NodeId> Members { get; }
    }
}