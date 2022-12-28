using BenchmarkDotNet.Attributes;
using SimpleSock.Models;
using SimpleSock.Test.Models;
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleSock.Test
{
    [MemoryDiagnoser]
    public class MarshalBenchmark
    {
        private static byte[] _Bytes = new byte[10];
        private static Packet _Packet;
        private static int _RepeatCount = 10000;

        [GlobalSetup]
        public void Init()
        {
            _Bytes = new byte[] { 0x02, 0x04, 0x00, 0x00, 0x00, 0x0B, 0x00, 0xDC, 0x03, 0x61, 0x00, 0x73, 0x00, 0x03 };
            _Packet = new Packet(_Bytes);
        }


        //[Benchmark]
        public Packet MarshalByteToStruct()
        {
            // Header
            IntPtr ptr = Marshal.AllocHGlobal(PacketHeader.Size);
            Marshal.Copy(_Bytes, 0, ptr, PacketHeader.Size);
            PacketHeader header = (PacketHeader)Marshal.PtrToStructure(ptr, typeof(PacketHeader));
            Marshal.FreeHGlobal(ptr);

            // Tail
            PacketTail tail = new PacketTail(_Bytes[_Bytes.Length - 1]);

            Packet packet = new Packet
            {
                Header = header,
                Data = Encoding.Unicode.GetString(_Bytes, PacketHeader.Size, (int)header.DataSize),
                Tail = tail,
            };

            return packet;
        }

        //[Benchmark]
        public Packet ParseByteToStruct()
        {
            return new Packet(_Bytes);
        }

        //[Benchmark]
        public byte[] MarshalStructToBytes()
        {
            var totalSize = _Packet.GetSize();
            byte[] bytes = new byte[totalSize];

            // Header
            IntPtr headPtr = Marshal.AllocHGlobal(PacketHeader.Size);
            Marshal.StructureToPtr(_Packet.Header, headPtr, true);
            Marshal.Copy(headPtr, bytes, 0, PacketHeader.Size);
            Marshal.FreeHGlobal(headPtr);

            // Data
            Encoding.Unicode.GetBytes(_Packet.Data, 0, _Packet.Data.Length, bytes, PacketHeader.Size);

            // Tail
            bytes[totalSize - 1] = _Packet.Tail.Etx;

            return bytes;
        }

        //[Benchmark]
        //public byte[] ParseStructToBytes()
        //{
        //    var totalSize = _Packet.GetSize();
        //    byte[] bytes = new byte[totalSize];

        //    unsafe
        //    {
        //        // Header
        //        bytes[0] = _Packet.Header.Stx;

        //        byte* a1 = stackalloc byte[4];
        //        Span<byte> sa1 = new Span<byte>(a1, 4);
        //        BinaryPrimitives.WriteUInt32LittleEndian(sa1, _Packet.Header.DataSize);
        //        sa1.CopyTo(new Span<byte>(bytes, 1, 4));

        //        byte* a2 = stackalloc byte[2];
        //        Span<byte> sa2 = new Span<byte>(a2, 2);
        //        BinaryPrimitives.WriteUInt16LittleEndian(sa2, _Packet.Header.ProtocolId);
        //        sa2.CopyTo(new Span<byte>(bytes, 5, 2));

        //        byte* a3 = stackalloc byte[2];
        //        Span<byte> sa3 = new Span<byte>(a3, 2);
        //        BinaryPrimitives.WriteUInt16LittleEndian(sa3, _Packet.Header.PacketId);
        //        sa3.CopyTo(new Span<byte>(bytes, 7, 2));

        //        // Data
        //        Encoding.Unicode.GetBytes(_Packet.Data, 0, _Packet.Data.Length, bytes, PacketHeader.Size);

        //        // Tail
        //        bytes[totalSize - 1] = _Packet.Tail.Etx;
        //    }

        //    return bytes;
        //}


        [Benchmark]
        public byte[] ParseStructToBytes2()
        {
            var totalSize = _Packet.GetSize();
            byte[] bytes = new byte[totalSize];

            unsafe
            {
                // Header
                bytes[0] = _Packet.Header.Stx;
                GetBytes(_Packet.Header.DataSize, bytes, 1);
                GetBytes(_Packet.Header.ProtocolId, bytes, 5);
                GetBytes(_Packet.Header.PacketId, bytes, 7);

                // Data
                Encoding.Unicode.GetBytes(_Packet.Data, 0, _Packet.Data.Length, bytes, PacketHeader.Size);

                // Tail
                bytes[totalSize - 1] = _Packet.Tail.Etx;
            }

            return bytes;
        }

        [Benchmark]
        public byte[] ParseStructToBytes3()
        {
            var totalSize = _Packet.GetSize();
            byte[] bytes = new byte[totalSize];

            // Header
            bytes[0] = _Packet.Header.Stx;
            GetBytes2(_Packet.Header.DataSize, bytes, 1);
            GetBytes2(_Packet.Header.ProtocolId, bytes, 5);
            GetBytes2(_Packet.Header.PacketId, bytes, 7);

            // Data
            Encoding.Unicode.GetBytes(_Packet.Data, 0, _Packet.Data.Length, bytes, PacketHeader.Size);

            // Tail
            bytes[totalSize - 1] = _Packet.Tail.Etx;

            return bytes;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static void MemCopy(byte* source, byte* target, int sourceOffset, int targetOffset, int count)
        {
            int loopCnt = targetOffset + count;

            for (int i = targetOffset, j = sourceOffset; i < loopCnt; ++i, ++j)
            {
                target[i] = source[j];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static void GetBytes(uint value, byte[] bytes, int bytesIndex)
        {
            fixed (byte* pbytes = &bytes[bytesIndex])
            {
                *(uint*)pbytes = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static void GetBytes(ushort value, byte[] bytes, int bytesIndex)
        {
            fixed (byte* pbytes = &bytes[bytesIndex])
            {
                *(ushort*)pbytes = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static void GetBytes2(uint value, byte[] bytes, int bytesIndex)
        {
            if (bytes.Length < bytesIndex + 4)
                throw new IndexOutOfRangeException();

            fixed (byte* pbytes = &bytes[bytesIndex])
            {
                *(uint*)pbytes = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static void GetBytes2(ushort value, byte[] bytes, int bytesIndex)
        {
            if (bytes.Length < bytesIndex + 2)
                throw new IndexOutOfRangeException();

            fixed (byte* pbytes = &bytes[bytesIndex])
            {
                *(ushort*)pbytes = value;
            }
        }




        //[Benchmark]
        //public void MarshalByteToStructRepeat()
        //{
        //    for (int i = 0; i < _RepeatCount; i++)
        //        MarshalByteToStruct();
        //}

        //[Benchmark]
        //public void ParseByteToStructRepeat()
        //{
        //    for (int i = 0; i < _RepeatCount; i++)
        //        ParseByteToStruct();
        //}

        //[Benchmark]
        //public void MarshalStructToBytesRepeat()
        //{
        //    for (int i = 0; i < _RepeatCount; i++)
        //        MarshalStructToBytes();
        //}

        //[Benchmark]
        //public void ParseStructToBytesRepeat()
        //{
        //    for (int i = 0; i < _RepeatCount; i++)
        //        ParseStructToBytes();
        //}


    }
}
