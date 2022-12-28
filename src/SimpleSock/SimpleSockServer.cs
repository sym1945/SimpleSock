//using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimpleSock
{
    class SimpleSockServer
    {
        class MyArraySegment<T> : ReadOnlySequenceSegment<T>
        {
            public MyArraySegment(T[] array)
            {
                this.Memory = array;
            }

            public MyArraySegment<T> Add(T[] array)
            {
                var segment = new MyArraySegment<T>(array);
                segment.RunningIndex = RunningIndex + Memory.Length;

                Next = segment;
                return segment;
            }
        }

        public class BufferSegment
        {
            public byte[] Buffer { get; set; }
            public int Count { get; set; }
            public int Remaining => Buffer.Length - Count;
        }

        void Process()
        {
            ReadOnlySequence<byte> seq = new ReadOnlySequence<byte>();

            //JsonSerializer.Deserialize();
        }

        async Task ProcessLinesAsync(NetworkStream stream)
        {
            const int minimumBufferSize = 512;

            var segments = new List<BufferSegment>();
            var bytesConsumed = 0;
            var bytesConsumedBufferIndex = 0;
            var segment = new BufferSegment { Buffer = ArrayPool<byte>.Shared.Rent(1024) };

            segments.Add(segment);

            while (true)
            {
                // Calculate the amount of bytes remaining in the buffer 
                if (segment.Remaining < minimumBufferSize)
                {
                    // Allocate a new segment 
                    segment = new BufferSegment { Buffer = ArrayPool<byte>.Shared.Rent(1024) };
                    segments.Add(segment);
                }

                var bytesRead = await stream.ReadAsync(segment.Buffer, segment.Count, segment.Remaining);
                if (bytesRead == 0)
                {
                    break;
                }

                // Keep track of the amount of buffered bytes 
                segment.Count += bytesRead;

                while (true)
                {
                    // Look for a EOL in the list of segments 
                    var (segmentIndex, segmentOffset) = IndexOf(segments, (byte)'\n', bytesConsumedBufferIndex, bytesConsumed);

                    if (segmentIndex >= 0)
                    {
                        // Process the line 
                        //ProcessLine(segments, segmentIndex, segmentOffset);

                        bytesConsumedBufferIndex = segmentOffset;
                        bytesConsumed = segmentOffset + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                // Drop fully consumed segments from the list so we don't look at them again 
                for (var i = bytesConsumedBufferIndex; i >= 0; --i)
                {
                    var consumedSegment = segments[i];

                    // Return all segments unless this is the current segment 

                    if (consumedSegment != segment)
                    {
                        ArrayPool<byte>.Shared.Return(consumedSegment.Buffer);
                        segments.RemoveAt(i);
                    }
                }
            }
        }


        (int segmentIndex, int segmentOffest) IndexOf(List<BufferSegment> segments, byte value, int startBufferIndex, int startSegmentOffset)
        {
            var first = true;

            for (var i = startBufferIndex; i < segments.Count; ++i)
            {
                var segment = segments[i];

                // Start from the correct offset 
                var offset = first ? startSegmentOffset : 0;
                var index = Array.IndexOf(segment.Buffer, value, offset, segment.Count - offset);

                if (index >= 0)
                {
                    // Return the buffer index and the index within that segment where EOL was found 
                    return (i, index);
                }

                first = false;
            }

            return (-1, -1);
        }


        //(int segmentIndex, int segmentOffest) Filter(List<BufferSegment> segments, int startBufferIndex, int startSegmentOffset)
        //{
        //    var first = true;

        //    for (var i = 0; i < segments.Count; ++i)
        //    {
        //        var segment = segments[i];

        //        // Start from the correct offset 
        //        // Stx 찾고 DataLength 찾아서 HEADER LENGTH + DATA LENGTH + TAIL LENGTH 까지 존재하면 고
        //        var stx = segment.Buffer[startSegmentOffset];
        //        var dataSize = BitConverter.ToUInt32(segment.Buffer, 1);
        //        var protocolId = BitConverter.ToUInt16(segment.Buffer, 5);
        //        var packetId = BitConverter.ToUInt16(segment.Buffer, 7);

        //        var offset = first ? startSegmentOffset : 0;
        //        //var index = Array.IndexOf(segment.Buffer, value, offset, segment.Count - offset);

        //        if (index >= 0)
        //        {
        //            // Return the buffer index and the index within that segment where EOL was found 
        //            return (i, index);
        //        }

        //        first = false;
        //    }

        //    return (-1, -1);
        //}
    }
}
