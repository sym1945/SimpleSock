using SimpleSock.Containers;
using SimpleSock.Extensions;
using SimpleSock.Helpers;
using SimpleSock.Interfaces;
using SimpleSock.Models;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace SimpleSock
{
    public abstract class SimpleSockServer2<TPacket>
    {
        private readonly string _IP;
        private readonly int _Port;
        private readonly int? _AcceptLimit;
        private readonly SessionContainer<Session2<TPacket>> _SessionContainer;
        private readonly object _SyncLock0 = new object();
        private readonly object _SyncLock1 = new object();

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


        public SimpleSockServer2(
            string ip
            , int port
            , int? acceptLimit = null)
        {
            ExceptionHelper.ThrowExceptionIfIsNull(ip);

            _SessionContainer = new SessionContainer<Session2<TPacket>>();
            _IP = ip;
            _Port = port;
            _AcceptLimit = acceptLimit;
        }

        public void Start()
        {
            lock (_SyncLock0)
            {
                if (_ServerStarted)
                    return;
                _ServerStarted = true;

                OnLog($"server started");

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

                OnLog("server stopped");
            }
        }

        public Task BroadcastAsync(TPacket packet)
        {
            var tasks = _SessionContainer.GetSessions().Select(session => session.SendAsync(packet));

            return Task.WhenAll(tasks);
        }

        public Task<int> SendAsync(Guid sessionId, TPacket packet)
        {
            if (_SessionContainer.GetSession(sessionId, out Session2<TPacket> session))
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

            OnLog($"server listening... {{ ip: {_IP}, port: {_Port} }}");

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
            OnLog("start accept task...");

            try
            {
                while (!cancelToken.IsCancellationRequested)
                {
                    var acceptedClient = await listener.AcceptTcpClientAsync();

                    Session2<TPacket> session = CreateSession(acceptedClient);
                    session.SessionClosed = OnSessionClosed;

                    if (_SessionContainer.AddSession(session))
                    {
                        OnLog($"session accepted... {session}");
                        OnLog($"session added: {session}, total session count: {_SessionContainer.SessionCount}");

                        OnAccepted(session);

                        session.StartReceive();
                    }
                    else
                        session.Dispose();


                    if (acceptLimit.HasValue
                        && _SessionContainer.SessionCount >= acceptLimit.Value)
                    {
                        // 제한 인원 수용하면 accept 그만하쟈
                        listener.Stop();
                        OnLog($"session accept limited... limit session count: {acceptLimit.Value}");
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
                        OnError(ex);
                    }
                }
                else
                {
                    OnError(ex);
                }
            }

            OnLog("stop accept task...");
        }

        private void OnSessionClosed(ISession session)
        {
            if (!_ServerStarted)
                return;

            _SessionContainer.RemoveSession(session.SessionId, out _);

            OnLog($"session removed: {session}, total session count: {_SessionContainer.SessionCount}");

            if (_AcceptLimit.HasValue
                && _SessionContainer.SessionCount < _AcceptLimit.Value)
            {
                RestartAcceptTask(3000);
            }

            OnClosed(session);
        }

        protected virtual void OnAccepted(ISession session)
        {
        }
        protected virtual void OnClosed(ISession session)
        {
        }
        protected virtual void OnLog(string log)
        {
        }
        protected virtual void OnError(Exception exception)
        {
        }

        protected abstract Session2<TPacket> CreateSession(TcpClient client);
    }


}
