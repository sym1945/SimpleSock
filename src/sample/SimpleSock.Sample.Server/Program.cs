using SimpleSock.Interfaces;
using SimpleSock.Models;
using SimpleSock.Sample.Shared;
using System;

namespace SimpleSock.Sample.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            SimpleSockServer<Packet> server = null;

            Action<ISession, Packet> onRecv = (session, packet) =>
            {
                Console.WriteLine("Recv:");
                Console.WriteLine(packet);
                Console.WriteLine();

                (session as Session<Packet>).SendAsync(packet);
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

            Action<ISession> onAccepted = (session) =>
            {
                Console.WriteLine($"Event: Accepted {session}");
            };

            server = new SimpleSockServer<Packet>("127.0.0.1", 5020, new PacketConverterSample()
                , onAccepted: onAccepted
               , onRecv: onRecv
               , onSent: onSent
               , onLog: onLog
               , onError: onError
               , onClose: onClose);

            bool loop = true;
            while (loop)
            {
                var input = Console.ReadLine();

                switch (input.ToLower())
                {
                    case "exit": loop = false; break;
                    case "stop": server.Stop(); break;
                    case "start": server.Start(); break;
                }
            }
        }
    }
}
