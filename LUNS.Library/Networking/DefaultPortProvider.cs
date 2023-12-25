using System;

namespace LUNS.Library.Networking
{
    internal class DefaultPortProvider : IPortProvider
    {
        private readonly int[] _ucListenPorts;
        private readonly int[] _mcListenPorts;

        public DefaultPortProvider(int nodeUnicastListenPort, int nodeMulticastListenPort)
        {
            NumApplications = 1;
            _ucListenPorts = new int[] { nodeUnicastListenPort };
            _mcListenPorts = new int[] { nodeMulticastListenPort };
        }

        public DefaultPortProvider(int[] nodeUnicastListenPorts, int[] nodeMulticastListenPorts)
        {
            if (nodeUnicastListenPorts.Length != nodeMulticastListenPorts.Length) throw new NotSupportedException("Each application must have a unicast and multicast port");
            if (nodeUnicastListenPorts.Length > 256) throw new NotSupportedException("More than 256 applications per node are not supported");
            if (nodeMulticastListenPorts.Length > 256) throw new NotSupportedException("More than 256 applications per node are not supported");

            NumApplications = nodeUnicastListenPorts.Length;
            _ucListenPorts = nodeUnicastListenPorts;
            _mcListenPorts = nodeMulticastListenPorts;
        }

        public int NumApplications { get; private set; }

        public int GetMulticastPort(GroupApplicationId groupAppId)
        {
            if (groupAppId.ApplicationIndex >= NumApplications)
            {
                throw new ArgumentOutOfRangeException(nameof(groupAppId), "The index of the application exceeds the available port range");
            }

            return _mcListenPorts[groupAppId.ApplicationIndex];
        }

        public int GetUnicastPort(byte applicationIndex)
        {
            if (applicationIndex >= NumApplications)
            {
                throw new ArgumentOutOfRangeException(nameof(applicationIndex), "The index of the application exceeds the available port range");
            }

            return _ucListenPorts[applicationIndex];
        }
    }
}