using BenchmarkDotNet.Attributes;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace SimpleSock.Test
{
    [MemoryDiagnoser]
    public class ConvertBenchmark
    {

        [GlobalSetup]
        public void Init()
        {
        }


        [Benchmark]
        [Arguments(short.MinValue)]
        [Arguments(short.MaxValue)]
        public byte[] ToBytes_BinaryPrimitives_short(short value)
        {
            byte[] bytes = new byte[2];
            BinaryPrimitives.WriteInt16LittleEndian(new Span<byte>(bytes), value);

            return bytes;
        }

        [Benchmark]
        [Arguments(short.MinValue)]
        [Arguments(short.MaxValue)]
        public byte[] ToBytes_Unsafe_short(short value)
        {
            byte[] bytes = new byte[2];

            unsafe
            {
                fixed (byte* pbytes = bytes)
                {
                    *(short*)pbytes = value;
                }
            }

            return bytes;
        }

        [Benchmark]
        [Arguments(ushort.MinValue)]
        [Arguments(ushort.MaxValue)]
        public byte[] ToBytes_BinaryPrimitives_ushort(ushort value)
        {
            byte[] bytes = new byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(bytes), value);

            return bytes;
        }

        [Benchmark]
        [Arguments(ushort.MinValue)]
        [Arguments(ushort.MaxValue)]
        public byte[] ToBytes_Unsafe_ushort(ushort value)
        {
            byte[] bytes = new byte[2];

            unsafe
            {
                fixed (byte* pbytes = bytes)
                {
                    *(ushort*)pbytes = value;
                }
            }

            return bytes;
        }



        [Benchmark]
        [Arguments(int.MinValue)]
        [Arguments(int.MaxValue)]
        public byte[] ToBytes_BinaryPrimitives_int(int value)
        {
            byte[] bytes = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(new Span<byte>(bytes), value);

            return bytes;
        }

        [Benchmark]
        [Arguments(int.MinValue)]
        [Arguments(int.MaxValue)]
        public byte[] ToBytes_Unsafe_int(int value)
        {
            byte[] bytes = new byte[4];

            unsafe
            {
                fixed (byte* pbytes = bytes)
                {
                    *(int*)pbytes = value;
                }
            }

            return bytes;
        }


        [Benchmark]
        [Arguments(uint.MinValue)]
        [Arguments(uint.MaxValue)]
        public byte[] ToBytes_BinaryPrimitives_uint(uint value)
        {
            byte[] bytes = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(bytes), value);

            return bytes;
        }

        [Benchmark]
        [Arguments(uint.MinValue)]
        [Arguments(uint.MaxValue)]
        public byte[] ToBytes_Unsafe_uint(uint value)
        {
            byte[] bytes = new byte[4];

            unsafe
            {
                fixed (byte* pbytes = bytes)
                {
                    *(uint*)pbytes = value;
                }
            }

            return bytes;
        }

        [Benchmark]
        [Arguments(long.MinValue)]
        [Arguments(long.MaxValue)]
        public byte[] ToBytes_BinaryPrimitives_long(long value)
        {
            byte[] bytes = new byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(new Span<byte>(bytes), value);

            return bytes;
        }

        [Benchmark]
        [Arguments(long.MinValue)]
        [Arguments(long.MaxValue)]
        public byte[] ToBytes_Unsafe_long(long value)
        {
            byte[] bytes = new byte[8];

            unsafe
            {
                fixed (byte* pbytes = bytes)
                {
                    *(long*)pbytes = value;
                }
            }

            return bytes;
        }

        [Benchmark]
        [Arguments(ulong.MinValue)]
        [Arguments(ulong.MaxValue)]
        public byte[] ToBytes_BinaryPrimitives_ulong(ulong value)
        {
            byte[] bytes = new byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(new Span<byte>(bytes), value);

            return bytes;
        }

        [Benchmark]
        [Arguments(ulong.MinValue)]
        [Arguments(ulong.MaxValue)]
        public byte[] ToBytes_Unsafe_ulong(ulong value)
        {
            byte[] bytes = new byte[8];

            unsafe
            {
                fixed (byte* pbytes = bytes)
                {
                    *(ulong*)pbytes = value;
                }
            }

            return bytes;
        }

        [Benchmark]
        [Arguments(float.MinValue)]
        [Arguments(float.MaxValue)]
        public byte[] ToBytes_BinaryPrimitives_float(float value)
        {
            return BitConverter.GetBytes(value);
        }

        [Benchmark]
        [Arguments(float.MinValue)]
        [Arguments(float.MaxValue)]
        public byte[] ToBytes_Unsafe_float(float value)
        {
            byte[] bytes = new byte[4];

            unsafe
            {
                fixed (byte* pbytes = bytes)
                {
                    *(float*)pbytes = value;
                }
            }

            return bytes;
        }

        [Benchmark]
        [Arguments(double.MinValue)]
        [Arguments(double.MaxValue)]
        public byte[] ToBytes_BinaryPrimitives_double(double value)
        {
            return BitConverter.GetBytes(value);
        }

        [Benchmark]
        [Arguments(double.MinValue)]
        [Arguments(double.MaxValue)]
        public byte[] ToBytes_Unsafe_double(double value)
        {
            byte[] bytes = new byte[8];

            unsafe
            {
                fixed (byte* pbytes = bytes)
                {
                    *(double*)pbytes = value;
                }
            }

            return bytes;
        }



    }



}
