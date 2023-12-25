using System;

namespace LUNS.Library
{
    /// <summary>
    /// A node id is a value which identifies the network node. The valid range is from 1 to 240 inclusive.
    /// </summary>
    public class NodeId
    {
        // For constructing local endpoints (see Network class), we add 10 to the id for the localhost IP.
        // This way, we do not use 127.0.0.1 - 127.0.0.10 which may be in use with other software
        // The router node id should not be 245 because we also do not want to use 127.0.0.252 - 127.0.0.255
        private static NodeId _router = new NodeId(241, true);
        private static NodeId _undefined = new NodeId(0, true);

        internal static NodeId Router => _router;

        internal static NodeId Undefined => _undefined;

        internal byte Index { get; }

        /// <summary>
        /// Compares two <see cref="NodeId"/> instances for value equality./>
        /// </summary>
        /// <param name="n1">The first instance to compare.</param>
        /// <param name="n2">The second instance to compare.</param>
        /// <returns>True if the two instances are equal by value.</returns>
        public static bool operator ==(NodeId n1, NodeId n2)
        {
            if (ReferenceEquals(n1, null) && ReferenceEquals(n2, null)) return true;
            if (ReferenceEquals(n1, null) || ReferenceEquals(n2, null)) return false;
            return n1.Index == n2.Index;
        }

        /// <summary>
        /// Compares two <see cref="NodeId"/> instances for value inequality./>
        /// </summary>
        /// <param name="n1">The first instance to compare.</param>
        /// <param name="n2">The second instance to compare.</param>
        /// <returns>True if the two instances are not equal by value.</returns>
        public static bool operator !=(NodeId n1, NodeId n2)
        {
            if (ReferenceEquals(n1, null) && ReferenceEquals(n2, null)) return false;
            if (ReferenceEquals(n1, null) || ReferenceEquals(n2, null)) return true;
            return n1.Index != n2.Index;
        }

        /// <summary>
        /// Checks if this instance has a value equality to another object.
        /// </summary>
        /// <param name="obj">The object to compare this instance to.</param>
        /// <returns>True if the object and this instance have the same value.</returns>
        public override bool Equals(object obj)
        {
            if (obj is NodeId otherId)
            {
                return otherId.Index == Index;
            }
            return false;
        }

        /// <summary>
        /// Retiurns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }

        private NodeId()
        {
            throw new NotImplementedException();
        }

        private NodeId(byte index, bool _)
        {
            Index = index;
        }

        /// <summary>
        /// Creates a new node identification.
        /// </summary>
        /// <param name="index">The index of the node. Indices must be in the range 1..240. Index 0 and inidces 240 and above are reserved.</param>
        public NodeId(byte index)
        {
            if (index == 0 || index > 240)
            {
                throw new ArgumentException("Invalid node index. Only the range 1..240 is supported", nameof(index));
            }

            Index = index;
        }

        /// <summary>
        /// Returns a string representation of this node identification.
        /// </summary>
        public override string ToString()
        {
            return Index.ToString();
        }
    }
}