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
        private readonly int? _AcceptLimit;
        private readonly IPacketConverter<TPacket> _PacketConverter;
        private readonly SessionContainer<Session<TPacket>> _SessionContainer;
        private readonly object _SyncLock0 = new object();
        private readonly object _SyncLock1 = new object();


        private readonly Action<ISession> _OnAccepted;
        private readonly Action<ISession, TPacket> _OnRecv;
        private readonly Action<ISession, TPacket> _OnSent;
        private readonly Action<ISession> _OnClosed;
        private readonly Action<string> _OnLog;
        private readonly Action<Exception> _OnError;

        private bool _ServerStarted;
        private TcpListener _Listner;
        private CancellationTokenSource _Canceller;
        private Task _AcceptTask;
        private Timer _DelayTimer;

        public bool ServerStarted
        {
            get { return _ServerStarted; }
        }

        public int SessionCount
        { 
            get { return _SessionContainer.SessionCount; }
        }


        public SimpleSockServer(
            string ip
            , int port
            , IPacketConverter<TPacket> packetConverter
            , int? acceptLimit = null
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
            _AcceptLimit = acceptLimit;

            _OnAccepted = onAccepted ?? new Action<ISession>(s => { });
            _OnRecv = onRecv ?? new Action<ISession, TPacket>((s, p) => { });
            _OnSent = onSent ?? new Action<ISession, TPacket>((s, p) => { });
            _OnClosed = onClose ?? new Action<ISession>((s) => { });
            _OnLog = onLog ?? new Action<string>((m) => { });
            _OnError = onError ?? new Action<Exception>((e) => { });
        }

        public void Start()
        {
            lock (_SyncLock0)
            {
                if (_ServerStarted)
                    return;
                _ServerStarted = true;

                _OnLog.Invoke($"server started");

                StartAcceptTask();
            }
        }

        public void Stop()
        {
            lock (_SyncLock0)
            {
                if (!_ServerStarted)
                    return;
                _ServerStarted = false;

                if (_Listner != null)
                    _Listner.Stop();

                if (_Canceller != null)
                    _Canceller.Cancel();

                if (_AcceptTask != null)
                {
                    _AcceptTask.GetAwaiter().GetResult();
                    _AcceptTask.Dispose();
                    _AcceptTask = null;
                }

                _SessionContainer.Clear();

                _OnLog.Invoke("server stopped");
            }
        }

        public async Task BroadcastAsync(TPacket packet)
        {
            foreach (var session in _SessionContainer.GetSessions())
            {
                await session.SendAsync(packet);
            }
        }

        public Task<int> SendAsync(Guid sessionId, TPacket packet)
        {
            if (_SessionContainer.GetSession(sessionId, out Session<TPacket> session))
                return session.SendAsync(packet);
            else
                return Task.FromResult(0);
        }

        private void StartAcceptTask()
        {
            if (_Listner != null || _Canceller != null)
                return;

            if (!IPAddress.TryParse(_IP, out IPAddress ipAddress))
                throw new Exception($"invalid IP: {_IP}");

            TcpListener listener = new TcpListener(ipAddress, _Port);
            listener.Start();

            _OnLog.Invoke($"server listening... {{ ip: {_IP}, port: {_Port} }}");

            _Listner = listener;
            _Canceller = new CancellationTokenSource();

            _AcceptTask = DoAcceptAsync(_Listner, _AcceptLimit, _Canceller.Token)
                .ContinueWith(t =>
                {
                    _Canceller?.Dispose();
                    _Canceller = null;
                    _Listner = null;
                });
        }

        private void RestartAcceptTask(int dueTime)
        {
            lock (_SyncLock1)
            {
                if (_DelayTimer != null)
                    return;

                _DelayTimer = new Timer(
                    callback: (o) =>
                    {
                        StartAcceptTask();

                        _DelayTimer?.Dispose();
                        _DelayTimer = null;
                    }
                    , state: null
                    , dueTime: dueTime
                    , period: Timeout.Infinite
                );
            }
        }

        private async Task DoAcceptAsync(TcpListener listener, int? acceptLimit, CancellationToken cancelToken)
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
                    {
                        _OnLog.Invoke($"session accepted... {session}");
                        _OnLog.Invoke($"session added: {session}, total session count: {_SessionContainer.SessionCount}");

                        _OnAccepted.Invoke(session);

                        session.StartReceive();
                    }
                    else
                        session.Dispose();


                    if (acceptLimit.HasValue
                        && _SessionContainer.SessionCount >= acceptLimit.Value)
                    {
                        // 제한 인원 수용하면 accept 그만하쟈
                        listener.Stop();
                        _OnLog.Invoke($"session accept limited... limit session count: {acceptLimit.Value}");
                        break;
                    }
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
            if (!_ServerStarted)
                return;

            _SessionContainer.RemoveSession(session.SessionId, out _);

            _OnLog.Invoke($"session removed: {session}, total session count: {_SessionContainer.SessionCount}");

            if (_AcceptLimit.HasValue
                && _SessionContainer.SessionCount < _AcceptLimit.Value)
            {
                RestartAcceptTask(3000);
            }

            _OnClosed.Invoke(session);
        }

    }


}
