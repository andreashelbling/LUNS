namespace LUNS.Tests.Util
{
    internal class TestPacketReceivedEventArgs
    {
        public TestPacketReceivedEventArgs(TestPacket packet)
        {
            Packet = packet;
        }

        public TestPacket Packet { get; }
    }
}
