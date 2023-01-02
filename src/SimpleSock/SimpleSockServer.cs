using SimpleSock.Containers;
using SimpleSock.Extensions;
using SimpleSock.Interfaces;
using SimpleSock.Models;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace SimpleSock
{
    public class SimpleSockServer<TPacket>
    {
        private readonly SessionContainer<Session<TPacket>> _SessionContainer;
        private readonly IPacketConverter<TPacket> _PacketConverter;
        private readonly SemaphoreSlim _AsyncLock = new SemaphoreSlim(1, 1);
        private readonly string _IP;
        private readonly int _Port;
        private readonly int _BackLog = 1;

        private readonly Action<ISession> _OnAccepted;
        private readonly Action<ISession, TPacket> _OnRecv;

        private TcpListener _Listner;
        private CancellationTokenSource _Canceller;
        private Task _AcceptTask;


        public SimpleSockServer(string ip, int port, IPacketConverter<TPacket> packetConverter, Action<ISession, TPacket> onRecv = null, Action<ISession> onAccepted = null)
        {
            _SessionContainer = new SessionContainer<Session<TPacket>>();
            _IP = ip;
            _Port = port;
            _PacketConverter = packetConverter;
            
            _OnRecv = onRecv ?? new Action<ISession, TPacket>((s, p) => { });
            _OnAccepted = onAccepted ?? new Action<ISession>(s => { });
        }

        public void Start()
        {
            if (_Listner != null || _Canceller != null)
                return;

            if (!IPAddress.TryParse(_IP, out IPAddress ipAddress))
                throw new Exception($"Invalid IP: {_IP}");

            TcpListener listener = new TcpListener(ipAddress, _Port);
            listener.Start(_BackLog);

            _Listner = listener;

            _Canceller = new CancellationTokenSource();

            _AcceptTask = DoAcceptAsync(_Listner, _Canceller.Token);
        }

        public void Stop()
        {
            if (_Listner == null || _Canceller == null)
                return;

            _Canceller.Cancel();
            _Canceller.Dispose();
            _Canceller = null;

            _Listner.Stop();
            _Listner = null;

            if (_AcceptTask != null)
                _AcceptTask.GetAwaiter().GetResult();

            _SessionContainer.Clear();
        }

        private async Task DoAcceptAsync(TcpListener listener, CancellationToken cancelToken)
        {
            Console.WriteLine("[EV] Accept start");

            try
            {
                while (!cancelToken.IsCancellationRequested)
                {
                    var acceptedClient = await listener.AcceptTcpClientAsync();

                    Session<TPacket> session = new Session<TPacket>(
                        client: acceptedClient
                        , packetConverter: _PacketConverter
                        , onRecv: _OnRecv
                        , onClose: Session_Closed
                    );

                    if (_SessionContainer.AddSession(session))
                        _OnAccepted.Invoke(session);
                    else
                        session.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SocketException se)
                {
                    if (se.IsIgnorableSocketException() == false)
                    {
                        //_Logger.WriteErrorLog(se);
                    }
                }
                else
                {
                    //    _Logger.WriteErrorLog(ex);
                }
            }

            Console.WriteLine("[EV] Accept end");
        }

        private void Session_Closed(ISession session)
        {
            Console.WriteLine($"[EV] session closed: {session}");

            _SessionContainer.RemoveSession(session.SessionId, out _);
        }
        
    }

  
}
