using System.Net;

namespace LUNS.Tests.Util
{
    class TestPeer : TestReceiver
    {
        public TestPeer(IPEndPoint localEndpoint, bool bind = true)
            : base(localEndpoint, bind)
        { 
        }

        public void Send(byte[] data, IPEndPoint targetEP)
        {
            _client.Send(data, data.Length, targetEP);
        }
    }
}
