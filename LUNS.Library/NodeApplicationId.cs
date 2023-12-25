using System;

namespace LUNS.Library
{
    /// <summary>
    /// A node/application id is a value which identifies an application on a certain network node.
    /// </summary>
    public class NodeApplicationId
    {
        private ushort _value;

        /// <summary>
        /// Compares two <see cref="NodeApplicationId"/> instances for value equality./>
        /// </summary>
        /// <param name="n1">The first instance to compare.</param>
        /// <param name="n2">The second instance to compare.</param>
        /// <returns>True if the two instances are equal by value.</returns>
        public static bool operator ==(NodeApplicationId n1, NodeApplicationId n2)
        {
            if (ReferenceEquals(n1, null) && ReferenceEquals(n2, null)) return true;
            if (ReferenceEquals(n1, null) || ReferenceEquals(n2, null)) return false;
            return n1._value == n2._value;
        }

        /// <summary>
        /// Compares two <see cref="NodeApplicationId"/> instances for value inequality./>
        /// </summary>
        /// <param name="n1">The first instance to compare.</param>
        /// <param name="n2">The second instance to compare.</param>
        /// <returns>True if the two instances are not equal by value.</returns>
        public static bool operator !=(NodeApplicationId n1, NodeApplicationId n2)
        {
            if (ReferenceEquals(n1, null) && ReferenceEquals(n2, null)) return false;
            if (ReferenceEquals(n1, null) || ReferenceEquals(n2, null)) return true;
            return n1._value != n2._value;
        }

        /// <summary>
        /// Checks if this instance has a value equality to another object.
        /// </summary>
        /// <param name="obj">The object to compare this instance to.</param>
        /// <returns>True if the object and this instance have the same value.</returns>
        public override bool Equals(object obj)
        {
            if (obj is NodeApplicationId otherId)
            {
                return otherId._value == _value;
            }
            return false;
        }

        /// <summary>
        /// Retiurns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        private NodeApplicationId()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a new application/node identification.
        /// </summary>
        /// <param name="nodeId">The node identifier of the node.</param>
        /// <param name="applicationIndex">The index of the application.</param>
        public NodeApplicationId(NodeId nodeId, byte applicationIndex = 0)
        {
            _value = BitConverter.ToUInt16(new byte[] { applicationIndex, nodeId.Index }, 0);
        }

        /// <summary>
        /// Gets the node identification.
        /// </summary>
        public NodeId NodeId => new NodeId(BitConverter.GetBytes(_value)[1]);

        /// <summary>
        /// Gets the application index.
        /// </summary>
        public byte ApplicationIndex => BitConverter.GetBytes(_value)[0];

        /// <summary>
        /// Returns a string representation for this instance.
        /// </summary>
        public override string ToString()
        {
            return $"0x{_value:x4}";
        }
    }
}