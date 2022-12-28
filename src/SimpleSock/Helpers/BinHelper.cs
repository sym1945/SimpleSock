using System;
using System.Runtime.CompilerServices;


namespace SimpleSock.Helpers
{
    public static class BinHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void GetBytes(short value, byte[] bytes, int bytesIndex)
        {
            if (bytes.Length < bytesIndex + 2)
                throw new IndexOutOfRangeException();

            fixed (byte* pbytes = &bytes[bytesIndex])
            {
                *(short*)pbytes = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void GetBytes(ushort value, byte[] bytes, int bytesIndex)
        {
            if (bytes.Length < bytesIndex + 2)
                throw new IndexOutOfRangeException();

            fixed (byte* pbytes = &bytes[bytesIndex])
            {
                *(ushort*)pbytes = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void GetBytes(int value, byte[] bytes, int bytesIndex)
        {
            if (bytes.Length < bytesIndex + 4)
                throw new IndexOutOfRangeException();

            fixed (byte* pbytes = &bytes[bytesIndex])
            {
                *(int*)pbytes = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void GetBytes(uint value, byte[] bytes, int bytesIndex)
        {
            if (bytes.Length < bytesIndex + 4)
                throw new IndexOutOfRangeException();

            fixed (byte* pbytes = &bytes[bytesIndex])
            {
                *(uint*)pbytes = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void GetBytes(long value, byte[] bytes, int bytesIndex)
        {
            if (bytes.Length < bytesIndex + 8)
                throw new IndexOutOfRangeException();

            fixed (byte* pbytes = &bytes[bytesIndex])
            {
                *(long*)pbytes = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void GetBytes(ulong value, byte[] bytes, int bytesIndex)
        {
            if (bytes.Length < bytesIndex + 8)
                throw new IndexOutOfRangeException();

            fixed (byte* pbytes = &bytes[bytesIndex])
            {
                *(ulong*)pbytes = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void GetBytes(float value, byte[] bytes, int bytesIndex)
        {
            if (bytes.Length < bytesIndex + 4)
                throw new IndexOutOfRangeException();

            fixed (byte* pbytes = &bytes[bytesIndex])
            {
                *(float*)pbytes = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void GetBytes(double value, byte[] bytes, int bytesIndex)
        {
            if (bytes.Length < bytesIndex + 8)
                throw new IndexOutOfRangeException();

            fixed (byte* pbytes = &bytes[bytesIndex])
            {
                *(double*)pbytes = value;
            }
        }


    }
}
