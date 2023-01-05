using SimpleSock.Interfaces;
using SimpleSock.Sample.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSock.Sample.Client
{
    class Program
    {
        static void Main(string[] args)
        {
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
                Console.WriteLine($"Event: Closed {session}");
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
    }
}
