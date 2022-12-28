﻿using BenchmarkDotNet.Running;
using SimpleSock.Interfaces;
using SimpleSock.Models;
using SimpleSock.Test.Implements;
using SimpleSock.Test.Models;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimpleSock.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            //BenchmarkRunner.Run<MarshalBenchmark>();
            SimpleSockClient<Packet> sockCli = null;

            Action<ISession, Packet> onRecv = (session, packet) =>
            {
                Console.WriteLine("Recv:");
                Console.WriteLine(packet);
                Console.WriteLine();

                Packet repPacket = null;

                if (packet.Header.ProtocolId == (ushort)ProtocolList.LINKTEST_REQ)
                    repPacket = Packet.CreateLinktestRsp(packet.Header.PacketId);
                else
                {
                    repPacket = Packet.CreateSendPacket(packet.Data);
                }


                sockCli.SendAsync(repPacket)
                        .ContinueWith(d =>
                        {
                            var sendMsg = string.Empty;

                            if (d.IsFaulted)
                                sendMsg = "Err - Send Failed:";
                            else
                                sendMsg = "Send:";

                            Console.WriteLine(sendMsg);
                            Console.WriteLine(repPacket);
                            Console.WriteLine();
                        });
            };

            sockCli = new SimpleSockClient<Packet>("127.0.0.1", 5020, new PacketConverterSample(), onRecv);

            Task conn = sockCli.ConnectAsync()
                .ContinueWith(d => Console.WriteLine("Connected!"));

            Console.ReadLine();
        }

        private static void PrintBytes(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
                Console.Write($"{bytes[i]} ");

            Console.WriteLine();
        }

    }
    
}
