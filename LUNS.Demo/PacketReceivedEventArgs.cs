using System;
using System.Net;

namespace LUNS.Demo
{
    internal class PacketReceivedEventArgs : EventArgs
    {
        public PacketReceivedEventArgs(byte[] data, IPEndPoint source, IPEndPoint target)
        {
            Data = data;
            Source = source;
            Target = target;
        }

        public byte[] Data { get; }

        public IPEndPoint Source { get; }

        public IPEndPoint Target { get; }
    }
}
