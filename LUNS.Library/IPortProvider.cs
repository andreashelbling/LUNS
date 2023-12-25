namespace LUNS.Library
{
    /// <summary>
    /// Interface used by the <see cref="Network"/> class to get the local ports where the application/s are listening on.
    /// </summary>
    public interface IPortProvider
    {
        /// <summary>
        /// Gets the port number where a specific application is listening for incoming UDP unicast packets.
        /// </summary>
        /// <param name="applicationIndex">The index of the application.</param>
        /// <returns>The port number the application uses.</returns>
        int GetUnicastPort(byte applicationIndex);

        /// <summary>
        /// Gets the port number where a specific application is listening for incoming UDP multicast packets within a specific group.
        /// </summary>
        /// <param name="groupAppId">Identifies the application and the group.</param>
        /// <returns>The port number the application uses.</returns>
        int GetMulticastPort(GroupApplicationId groupAppId);

        /// <summary>
        /// Gets the number of application which are in use.
        /// </summary>
        /// <remarks>Used by the <see cref="Router"/> to create the communication channels required to route all application traffic.</remarks>
        int NumApplications { get; }
    }
}