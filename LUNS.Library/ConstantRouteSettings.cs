namespace LUNS.Library
{
    /// <summary>
    /// Quality settings for a route which are constant over time.
    /// </summary>
    public class ConstantRouteSettings : RouteSettings
    {
        /// <summary>
        /// Creates a new route quality setting for a lossless route with a specific bandwidth.
        /// </summary>
        /// <param name="bitRate">The bandwidth for the route in bits per second.</param>
        public ConstantRouteSettings(double bitRate)
        {
            BitRate = bitRate;
        }

        /// <summary>
        /// Creates a new route quality setting for a custom/lossy route.
        /// </summary>
        /// <param name="bitRate">The bandwidth for the route in bits per second.</param>
        /// <param name="packetLossRate">The relative packet loss rate betwen 0.0 (no packets are lost) and 1.0 (all packets are lost).</param>
        /// <param name="bitErrorRate">The relative bit error rate between 0.0 (not bits are inverted) and 1.0 (all bits are inverted).</param>
        public ConstantRouteSettings(double bitRate, double packetLossRate, double bitErrorRate)
            : this(bitRate)
        {
            PacketLossRate = packetLossRate;
            BitErrorRate = bitErrorRate;
        }

        /// <summary>
        /// Mean number of bits transferred per second.
        /// </summary>
        public double BitRate { get; set; }

        /// <summary>
        /// Rate of lost packets
        /// </summary>
        public double PacketLossRate { get; set; }

        /// <summary>
        /// Rate of which a bit error remains in the packet (the packet is not lost). Bit errors only affect UDP payload
        /// </summary>
        public double BitErrorRate { get; set; }

        internal override double GetBitRate(double relativeTime)
        {
            return BitRate;
        }

        internal override double GetPacketLossRate(double relativeTime)
        {
            return PacketLossRate;
        }

        internal override double GetBitErrorRate(double relativeTime)
        {
            return BitErrorRate;
        }
    }
}