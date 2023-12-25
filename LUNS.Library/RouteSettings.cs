namespace LUNS.Library
{
    /// <summary>
    /// Base class for the quality settings of a route.
    /// </summary>
    public abstract class RouteSettings
    {
        internal abstract double GetBitRate(double relativeTime);

        internal abstract double GetPacketLossRate(double relativeTime);

        internal abstract double GetBitErrorRate(double relativeTime);

        /// <summary>
        /// Creates a default quality settings instance for an ideal route.
        /// </summary>
        public static RouteSettings Default => new ConstantRouteSettings(1e9, 0.0, 0.0);
    }
}