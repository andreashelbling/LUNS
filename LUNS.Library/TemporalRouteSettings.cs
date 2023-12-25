using System.Collections.Generic;
using System.Linq;

namespace LUNS.Library
{
    /// <summary>
    /// Quality settings for a route which can change over time.
    /// </summary>
    public class TemporalRouteSettings : RouteSettings
    {
        private readonly SortedDictionary<double, ConstantRouteSettings> _entries = new SortedDictionary<double, ConstantRouteSettings>();

        /// <summary>
        /// Adds a constant quality setting for a segment of time.
        /// </summary>
        /// <param name="settings">The constant quality setting to add.</param>
        /// <param name="relativeStartTime">The relative time since the <see cref="Router"/> was started after which the <paramref name="settings"/> come into effect.</param>
        public void Add(ConstantRouteSettings settings, double relativeStartTime = 0.0)
        {
            _entries.Add(relativeStartTime, settings);
        }

        internal override double GetBitRate(double relativeTime)
        {
            double nearestStart = _entries.Keys.LastOrDefault(t => relativeTime >= t);
            if (_entries.TryGetValue(nearestStart, out ConstantRouteSettings settings))
            {
                return _entries[nearestStart].BitRate;
            }
            return RouteSettings.Default.GetBitRate(relativeTime);
        }

        internal override double GetPacketLossRate(double relativeTime)
        {
            double nearestStart = _entries.Keys.LastOrDefault(t => relativeTime >= t);
            if (_entries.TryGetValue(nearestStart, out ConstantRouteSettings settings))
            {
                return _entries[nearestStart].PacketLossRate;
            }
            return RouteSettings.Default.GetPacketLossRate(relativeTime);
        }

        internal override double GetBitErrorRate(double relativeTime)
        {
            double nearestStart = _entries.Keys.LastOrDefault(t => relativeTime >= t);
            if (_entries.TryGetValue(nearestStart, out ConstantRouteSettings settings))
            {
                return _entries[nearestStart].BitErrorRate;
            }
            return RouteSettings.Default.GetBitErrorRate(relativeTime);
        }
    }
}