using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleSock.Test.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketHeader
    {
        public const byte STX = 0x02;
        public readonly static int Size;

        public readonly byte Stx;
        public readonly uint DataSize;
        public readonly ushort ProtocolId;
        public readonly ushort PacketId;

        static PacketHeader()
        {
            Size = Marshal.SizeOf<PacketHeader>();
        }

        public PacketHeader(uint dataSize, ushort protocolId, ushort packetId, byte stx = STX)
        {
            Stx = stx;
            DataSize = dataSize;
            ProtocolId = protocolId;
            PacketId = packetId;
        }

        public override string ToString()
        {
            return $"STX: {Stx}, DataSize: {DataSize}, ProtocolId: {ProtocolId}, PacketId: {PacketId}";
        }
    }
}
