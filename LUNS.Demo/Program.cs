using System.Net;
using System.Threading;
using LUNS.Library;

namespace LUNS.Demo
{
    class Program 
    {
        static void Main(string[] args)
        {
            var net = new Network(31000, 31001, new Logger());
            
            // create nodes
            ApplicationOnNode appOnNode1 = CreateApplicationOnNode(net, new NodeId(1), new GroupId(1));
            ApplicationOnNode appOnNode2 = CreateApplicationOnNode(net, new NodeId(2), new GroupId(1));
            ApplicationOnNode appOnNode3 = CreateApplicationOnNode(net, new NodeId(3), new GroupId(1));

            // setup routes
            net.AddRoute(appOnNode1.Id.NodeId, appOnNode2.Id.NodeId);
            net.AddRoute(appOnNode2.Id.NodeId, appOnNode1.Id.NodeId);

            net.AddRoute(appOnNode1.Id.NodeId, appOnNode3.Id.NodeId);
            net.AddRoute(appOnNode3.Id.NodeId, appOnNode1.Id.NodeId);

            // setup multicast groups
            var group = new GroupId(1);
            net.JoinMulticastGroup(appOnNode1.Id.NodeId, group);
            net.JoinMulticastGroup(appOnNode2.Id.NodeId, group);
            net.JoinMulticastGroup(appOnNode3.Id.NodeId, group);

            using (var router = new Router(net))
            {
                router.Start();

                // send unicast messages
                appOnNode1.SendText("Hello from node 1 to node 2", appOnNode2.Id.NodeId);
                appOnNode1.SendText("Hello from node 1 to node 3", appOnNode3.Id.NodeId);
               
                // send multicast message
                appOnNode1.SendText("Hello from node 1 to group 1", group);

                Thread.Sleep(100);

                appOnNode1.Dispose();
                appOnNode2.Dispose();
                appOnNode3.Dispose();
            }
        }

        static ApplicationOnNode CreateApplicationOnNode(Network net, NodeId nodeId, GroupId groupId = null, byte applicationIndex = 0)
        {
            IPEndPoint ucEp = net.GetLocalUnicastEndpoint(new NodeApplicationId(nodeId, applicationIndex));
            IPEndPoint mcEp = null;
            if (groupId != null)
            {
                mcEp = net.GetLocalMulticastEndpoint(nodeId, new GroupApplicationId(groupId, applicationIndex));
            }

            return new ApplicationOnNode(net, new NodeApplicationId(nodeId, applicationIndex), ucEp, mcEp);
        }
    }
}
