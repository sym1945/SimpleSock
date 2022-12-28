using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SimpleSock
{
    public class TestClass
    {
        public void Make()
        {
            byte[] arr1 = new byte[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            byte[] arr2 = new byte[10] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            byte[] arr3 = new byte[10] { 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 };


            MyArraySegment<byte> seg1 = new MyArraySegment<byte>(arr1, 0, 5);
            MyArraySegment<byte> seg3 = seg1.Add(arr2, 0, 5).Add(arr3, 0, 5);

            ReadOnlySequence<byte> seq = new ReadOnlySequence<byte>(seg1, 0, seg3, seg3.Memory.Length);

            foreach (var segment in seq)
            {
                foreach (int item in segment.Span)
                {
                    Console.Write(item + ", ");
                }
            }

            Console.WriteLine();

            var position = seq.GetPosition(2);

            if (seq.TryGet(ref position, out ReadOnlyMemory<byte> mem))
            {
                for (int i = 0; i < mem.Span.Length; i++)
                {
                    Console.Write(mem.Span[i] + ", ");
                }
            }

        }
    }


    public class MyArraySegment<T> : ReadOnlySequenceSegment<T>
    {
        public MyArraySegment(T[] array, int start, int count)
        {
            this.Memory = new ReadOnlyMemory<T>(array, start, count);
        }

        public MyArraySegment<T> Add(T[] array, int start, int count)
        {
            var segment = new MyArraySegment<T>(array, start, count);
            segment.RunningIndex = RunningIndex + Memory.Length;

            Next = segment;
            return segment;
        }
    }


}
