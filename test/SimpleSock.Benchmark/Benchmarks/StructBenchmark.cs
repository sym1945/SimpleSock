using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace SimpleSock.Benchmark
{
    [MemoryDiagnoser]
    public class StructBenchmark
    {
        private static int _DataCount = 10000;

        [GlobalSetup]
        public void Init()
        {
        }


        [Benchmark]
        public List<DataClass> HandleDataClass()
        {
            var result = new List<DataClass>(_DataCount);
            for (int i = 0; i < _DataCount; i++)
            {
                result.Add(new DataClass
                {
                    Stx = 2,
                    DataSize = 100,
                    PacketId = (ushort)i,
                    ProtocolId = (ushort)(i + 1)
                });
            }

            var newList = new List<DataClass>(result.Count);
            foreach (var data in result)
            {
                var a = ProcessDataClass(data);
                newList.Add(a);
            }
                

            return result;
        }

        [Benchmark]
        public List<DataStruct> HandleDataStruct()
        {
            var result = new List<DataStruct>(_DataCount);
            for (int i = 0; i < _DataCount; i++)
            {
                result.Add(new DataStruct
                {
                    Stx = 2,
                    DataSize = 100,
                    PacketId = (ushort)i,
                    ProtocolId = (ushort)(i + 1)
                });
            }

            var newList = new List<DataStruct>(result.Count);
            foreach (var data in result)
            {
                var a = ProcessDataStruct(data);
                newList.Add(a);
            }

            return newList;
        }

        private List<DataClass> LoopDataClass(List<DataClass> datas)
        {
            List<DataClass> newList = new List<DataClass>(datas.Count);
            foreach (var d in datas)
            {
                newList.Add(d);
            }

            return newList;
        }

        private List<DataStruct> LoopDataStruct(List<DataStruct> datas)
        {
            List<DataStruct> newList = new List<DataStruct>(datas.Count);
            foreach (var d in datas)
            {
                newList.Add(d);
            }

            return newList;
        }

        private DataClass ProcessDataClass(DataClass data)
        {
            return data;
        }

        private DataStruct ProcessDataStruct(DataStruct data)
        {
            return data;
        }

    }



    public class DataClass
    {
        public byte Stx { get; set; }
        public uint DataSize { get; set; }
        public ushort ProtocolId { get; set; }
        public ushort PacketId { get; set; }
    }

    public struct DataStruct
    {
        public byte Stx { get; set; }
        public uint DataSize { get; set; }
        public ushort ProtocolId { get; set; }
        public ushort PacketId { get; set; }

        public DataStruct(byte stx, uint dataSize, ushort protocolId, ushort packetId)
        {
            Stx = stx;
            DataSize = dataSize;
            ProtocolId = protocolId;
            PacketId = packetId;
        }
    }
}
