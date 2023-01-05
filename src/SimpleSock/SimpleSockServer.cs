using SimpleSock.Containers;
using SimpleSock.Extensions;
using SimpleSock.Helpers;
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
        private readonly string _IP;
        private readonly int _Port;
        private readonly int _BackLog = 1;
        private readonly IPacketConverter<TPacket> _PacketConverter;
        private readonly SessionContainer<Session<TPacket>> _SessionContainer;
        private readonly SemaphoreSlim _AsyncLock = new SemaphoreSlim(1, 1);

        private readonly Action<ISession> _OnAccepted;
        private readonly Action<ISession, TPacket> _OnRecv;
        private readonly Action<ISession, TPacket> _OnSent;
        private readonly Action<ISession> _OnClosed;
        private readonly Action<string> _OnLog;
        private readonly Action<Exception> _OnError;

        private TcpListener _Listner;
        private CancellationTokenSource _Canceller;
        private Task _AcceptTask;


        public SimpleSockServer(
            string ip
            , int port
            , IPacketConverter<TPacket> packetConverter
            , Action<ISession> onAccepted = null
            , Action<ISession, TPacket> onRecv = null
            , Action<ISession, TPacket> onSent = null
            , Action<ISession> onClose = null
            , Action<string> onLog = null
            , Action<Exception> onError = null)
        {
            ExceptionHelper.ThrowExceptionIfIsNull(ip);
            ExceptionHelper.ThrowExceptionIfIsNull(packetConverter);

            _SessionContainer = new SessionContainer<Session<TPacket>>();
            _IP = ip;
            _Port = port;
            _PacketConverter = packetConverter;
            
            _OnAccepted = onAccepted ?? new Action<ISession>(s => { });
            _OnRecv = onRecv ?? new Action<ISession, TPacket>((s, p) => { });
            _OnSent = onSent ?? new Action<ISession, TPacket>((s, p) => { });
            _OnClosed = onClose ?? new Action<ISession>((s) => { });
            _OnLog = onLog ?? new Action<string>((m) => { });
            _OnError = onError ?? new Action<Exception>((e) => { });
        }

        public void Start()
        {
            if (_Listner != null || _Canceller != null)
                return;

            if (!IPAddress.TryParse(_IP, out IPAddress ipAddress))
                throw new Exception($"Invalid IP: {_IP}");

            TcpListener listener = new TcpListener(ipAddress, _Port);
            listener.Start();

            _OnLog.Invoke($"server started, IP: {_IP}, Port: {_Port}");

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

            _Listner.Stop();

            if (_AcceptTask != null)
                _AcceptTask.GetAwaiter().GetResult();

            _SessionContainer.Clear();

            _OnLog.Invoke("server stopped");

            _Canceller = null;
            _Listner = null;
        }

        private async Task DoAcceptAsync(TcpListener listener, CancellationToken cancelToken)
        {
            _OnLog.Invoke("start accept task...");

            try
            {
                while (!cancelToken.IsCancellationRequested)
                {
                    var acceptedClient = await listener.AcceptTcpClientAsync();

                    Session<TPacket> session = new Session<TPacket>(
                        client: acceptedClient
                        , packetConverter: _PacketConverter
                        , onRecv: _OnRecv
                        , onSent: _OnSent
                        , onClose: OnSessionClosed
                        , onError: _OnError
                        , onLog: _OnLog
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
                        _OnError.Invoke(ex);
                    }
                }
                else
                {
                    _OnError.Invoke(ex);
                }
            }

            _OnLog.Invoke("stop accept task...");
        }

        private void OnSessionClosed(ISession session)
        {
            _SessionContainer.RemoveSession(session.SessionId, out _);

            _OnClosed.Invoke(session);
        }
        
    }

  
}
