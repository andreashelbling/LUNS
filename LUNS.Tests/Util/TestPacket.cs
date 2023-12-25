using System.Net;

namespace LUNS.Tests.Util
{
    internal class TestPacket
    {
        public TestPacket(IPEndPoint targetEP, IPEndPoint sourceEP, byte[] data)
        {
            Data = data;
            SourceEndPoint = sourceEP;
            TargetEndPoint = targetEP;
        }

        public byte[] Data { get; }

        public IPEndPoint SourceEndPoint { get; }

        public IPEndPoint TargetEndPoint { get; }
    }
}
