using System;

namespace LUNS.Library.PacketIO
{
    internal class PacketReceivedEventArgs : EventArgs
    { 
        public Packet Packet { get; }

        public PacketReceivedEventArgs(Packet packet)
        {
            Packet = packet;
        }
    }
}
