using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleSock.Test.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketTail
    {
        public const byte ETX = 0x03;
        public readonly static int Size;

        public readonly byte Etx;

        public static PacketTail Default => new PacketTail(ETX);

        static PacketTail()
        {
            Size = Marshal.SizeOf<PacketTail>();
        }

        public PacketTail(byte etx)
        {
            Etx = etx;
        }

        public override string ToString()
        {
            return $"ETX: {Etx}";
        }
    }
}
