using System;

namespace LUNS.Library
{
    /// <summary>
    /// Identifies a multicast group.
    /// </summary>
    public class GroupId
    {
        private static GroupId _none = new GroupId(0, true);
        private static GroupId _reserved = new GroupId(255, true); // for future use, e.g. broadcasting

        internal static GroupId None => _none;

        internal static GroupId Reserved => _reserved;

        /// <summary>
        /// Gets the group index.
        /// </summary>
        public byte Index { get; }

        /// <summary>
        /// Compares two <see cref="GroupId"/> instances for value equality./>
        /// </summary>
        /// <param name="n1">The first instance to compare.</param>
        /// <param name="n2">The second instance to compare.</param>
        /// <returns>True if the two instances are equal by value.</returns>
        public static bool operator ==(GroupId n1, GroupId n2)
        {
            if(ReferenceEquals(n1, null) && ReferenceEquals(n2, null)) return true;
            if (ReferenceEquals(n1, null) || ReferenceEquals(n2, null)) return false;
            return n1.Index == n2.Index;
        }

        /// <summary>
        /// Compares two <see cref="GroupId"/> instances for value inequality./>
        /// </summary>
        /// <param name="n1">The first instance to compare.</param>
        /// <param name="n2">The second instance to compare.</param>
        /// <returns>True if the two instances are not equal by value.</returns>
        public static bool operator !=(GroupId n1, GroupId n2)
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
            if (obj is GroupId otherId)
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

        private GroupId()
        {
            throw new NotImplementedException();
        }

        private GroupId(byte index, bool _)
        {
            Index = index;
        }

        /// <summary>
        /// Creates a new group identification.
        /// </summary>
        /// <param name="index">The index of the group. Indices must be in the range 1..254. Index 0 and 255 are reserved.</param>
        public GroupId(byte index)
        {
            if (index == 0 || index == 255)
            {
                throw new ArgumentException("Invalid group index. Only the range 1..254 is supported", nameof(index));
            }

            Index = index;
        }

        /// <summary>
        /// Returns a string representation of this group identification.
        /// </summary>
        public override string ToString()
        {
            return Index.ToString();
        }
    }
}