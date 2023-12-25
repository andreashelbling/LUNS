using System;
using LUNS.Library;

namespace LUNS.Tests.Util
{
    /// <summary>
    /// PortProvider for unit tests.
    /// </summary>
    public class TestPortProvider : IPortProvider
    {
        private int _numApps;
        private int _numGroupsPerApp;
            
        public TestPortProvider(int numApps, int numGroupsPerApp)
        {
            if (numApps > 100) throw new ArgumentOutOfRangeException("Only up to 100 apps are supported");
            if(numGroupsPerApp > 10) throw new ArgumentOutOfRangeException("Only up to 10 groups per app are supported");

            _numApps = numApps;
            _numGroupsPerApp = numGroupsPerApp;
        }

        public int NumApplications => _numApps;

        public int GetMulticastPort(GroupApplicationId groupAppId)
        {
            byte groupIndex = groupAppId.GroupId.Index;
            if (groupIndex > _numGroupsPerApp)
            {
                throw new ArgumentOutOfRangeException($"Group with index {groupIndex} exceed maximum expected groups of {_numGroupsPerApp}.");
            }
            return 31100 + groupAppId.ApplicationIndex * _numGroupsPerApp + groupIndex;
        }

        public int GetUnicastPort(byte applicationIndex)
        {
            return 31000 + applicationIndex;
        }
    }
}
