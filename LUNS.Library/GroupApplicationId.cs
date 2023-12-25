using System;

namespace LUNS.Library
{
    /// <summary>
    /// A group/application id is a value which identifies an application on a certain group of network nodes.
    /// </summary>
    public class GroupApplicationId
    {
        private ushort _value;

        /// <summary>
        /// Compares two <see cref="GroupApplicationId"/> instances for value equality./>
        /// </summary>
        /// <param name="n1">The first instance to compare.</param>
        /// <param name="n2">The second instance to compare.</param>
        /// <returns>True if the two instances are equal by value.</returns>
        public static bool operator ==(GroupApplicationId n1, GroupApplicationId n2)
        {
            if (ReferenceEquals(n1, null) && ReferenceEquals(n2, null)) return true;
            if (ReferenceEquals(n1, null) || ReferenceEquals(n2, null)) return false;
            return n1._value == n2._value;
        }

        /// <summary>
        /// Compares two <see cref="GroupApplicationId"/> instances for value inequality./>
        /// </summary>
        /// <param name="n1">The first instance to compare.</param>
        /// <param name="n2">The second instance to compare.</param>
        /// <returns>True if the two instances are not equal by value.</returns>
        public static bool operator !=(GroupApplicationId n1, GroupApplicationId n2)
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
            if (obj is GroupApplicationId otherId)
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

        private GroupApplicationId()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a new application/group identification.
        /// </summary>
        /// <param name="groupId">The group identifier of the multicast group.</param>
        /// <param name="applicationIndex">The index of the application.</param>
        /// <remarks>This is typically used if an application uses multicast communication within a group so the traffic for 
        /// this application can be seperated from other applications within the same multicast group.</remarks>
        public GroupApplicationId(GroupId groupId, byte applicationIndex = 0)
        {
            _value = BitConverter.ToUInt16(new byte[] { applicationIndex, groupId.Index }, 0);
        }

        /// <summary>
        /// Gets the group identifier for this instance.
        /// </summary>
        public GroupId GroupId => new GroupId(BitConverter.GetBytes(_value)[1]);

        /// <summary>
        /// Gets the application index for this instance.
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