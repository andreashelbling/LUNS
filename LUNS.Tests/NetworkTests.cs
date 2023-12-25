using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using LUNS.Library;
using LUNS.Library.Networking;

namespace LUNS.Tests
{
    [TestClass]
    public class NetworkTests
    {
        [TestMethod]
        public void GetRoutesTest()
        {
            var net = new Network(27100, 27101);
            net.AddRoute(new NodeId(1), new NodeId(3));
            net.AddRoute(new NodeId(2), new NodeId(3));
            net.AddRoute(new NodeId(3), new NodeId(4));
            net.AddRoute(new NodeId(5), new NodeId(4));
            net.AddRoute(new NodeId(6), new NodeId(5));
            net.AddRoute(new NodeId(7), new NodeId(5));
            net.AddRoute(new NodeId(8), new NodeId(5));
            IEnumerable<Route> routes = net.GetRoutes();
            Assert.AreEqual(7, routes.Count());
        }

        [TestMethod]
        public void GetBidirectionalRoutesTest()
        {
            var net = new Network(27100, 27101);
            net.AddRoute(new NodeId(1), new NodeId(2));
            net.AddRoute(new NodeId(1), new NodeId(3));
            net.AddRoute(new NodeId(3), new NodeId(1));
            net.AddRoute(new NodeId(2), new NodeId(1));
            IEnumerable<Route> routes = net.GetRoutes();
            Assert.AreEqual(4, routes.Count());
        }

        [TestMethod]
        public void GetGroupsTest()
        {
            var net = new Network(27100, 27101);
            net.JoinMulticastGroup(new NodeId(1), new GroupId(102));
            net.JoinMulticastGroup(new NodeId(2), new GroupId(102));
            net.JoinMulticastGroup(new NodeId(3), new GroupId(102));
            net.JoinMulticastGroup(new NodeId(3), new GroupId(101));
            net.JoinMulticastGroup(new NodeId(4), new GroupId(101));

            List<Group> groups = net.GetGroups().OrderBy(g => g.Id.Index).ToList();
            Assert.AreEqual(2, groups.Count);

            Assert.AreEqual(new GroupId(101), groups[0].Id);
            Assert.AreEqual(2, groups[0].Members.Count);
            Assert.IsTrue(groups[0].Members.Contains(new NodeId(3)));
            Assert.IsTrue(groups[0].Members.Contains(new NodeId(4)));

            Assert.AreEqual(new GroupId(102), groups[1].Id);
            Assert.AreEqual(3, groups[1].Members.Count);
            Assert.IsTrue(groups[1].Members.Contains(new NodeId(1)));
            Assert.IsTrue(groups[1].Members.Contains(new NodeId(2)));
            Assert.IsTrue(groups[1].Members.Contains(new NodeId(3)));
        }
    }
}
