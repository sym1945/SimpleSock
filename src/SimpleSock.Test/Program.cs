using BenchmarkDotNet.Running;
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
            SimpleSockClient<Packet> client = null;

            Action<ISession, Packet> onRecv = (session, packet) =>
            {
                Console.WriteLine("Recv:");
                Console.WriteLine(packet);
                Console.WriteLine();

                Packet repPacket = null;

                if (packet.Header.ProtocolId == (ushort)ProtocolList.LINKTEST_REQ)
                {
                    repPacket = Packet.CreateLinktestRsp(packet.Header.PacketId);
                    client.SendAsync(repPacket);
                }
            };

            Action<Exception> onError = (e) =>
            {
                Console.WriteLine($"Error: {e.Message}");
            };

            Action<ISession> onClose = (session) =>
            {
                Console.WriteLine($"Event: Closed ({session})");
            };

            Action<string> onLog = (e) =>
            {
                Console.WriteLine(e);
            };

            Action<ISession, Packet> onSent = (session, packet) =>
            {
                Console.WriteLine("Send:");
                Console.WriteLine(packet);
                Console.WriteLine();
            };


            client = new SimpleSockClient<Packet>("127.0.0.1", 5020, new PacketConverterSample()
               , onRecv: onRecv
               , onSent: onSent
               , onLog: onLog
               , onError: onError
               , onClose: onClose);


            Task connTask() => Task.Run(async () =>
            {
                bool isConnecetd = false;

                while (!isConnecetd)
                {
                    try
                    {
                        await client.ConnectAsync();

                        isConnecetd = true;

                        await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        onError(ex);
                    }
                }
            });

            connTask();

            bool loop = true;
            while (loop)
            {
                var input = Console.ReadLine();

                switch (input.ToLower())
                {
                    case "exit": loop = false; break;
                    case "stop": var diconTask = client.DisconnectAsync(); break;
                    case "start": var conTask = connTask(); break;
                    default:
                        {
                            var packet = Packet.CreateSendPacket(input);

                            client.SendAsync(packet);
                            break;
                        }
                }
            }
        }

        private static void PrintBytes(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
                Console.Write($"{bytes[i]} ");

            Console.WriteLine();
        }

    }

}
