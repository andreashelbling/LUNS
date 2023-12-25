namespace LUNS.Library
{
    /// <summary>
    /// Interface used by LUNS to output log messages. Implementations are assumed to be thread-safe.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a formatted information.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The formatting arguments.</param>
        void InfoFormat(string format, params object[] args);

        /// <summary>
        /// Logs a formatted warning.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The formatting arguments.</param>
        void WarnFormat(string format, params object[] args);

        /// <summary>
        /// Logs a formatted error.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The formatting arguments.</param>
        void ErrorFormat(string format, params object[] args);
    }
}