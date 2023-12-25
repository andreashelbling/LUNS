using System.Net;

namespace LUNS.Library.PacketIO
{
    internal class Packet
    {
        public Packet(Packet other)
        {
            SourceEndPoint = other.SourceEndPoint;
            TargetEndPoint = other.TargetEndPoint;
            Data = (byte[])other.Data.Clone();
        }

        public Packet(IPEndPoint sourceEndPoint, IPEndPoint targetEndPoint, byte[] data)
        {
            SourceEndPoint = sourceEndPoint;
            TargetEndPoint = targetEndPoint;
            Data = data;
        }

        public IPEndPoint SourceEndPoint { get; }

        public IPEndPoint TargetEndPoint { get; }

        public byte[] Data { get; }
    }
}
