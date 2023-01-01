using SimpleSock.Extensions;
using SimpleSock.Interfaces;
using SimpleSock.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
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

        private TcpListener _Listner;
        private CancellationTokenSource _Canceller;
        private Task _AcceptTask;

        private Action<ISession> _OnAccepted;
        private Action<ISession, TPacket> _OnRecv;



        public SimpleSockServer(string ip, int port, IPacketConverter<TPacket> packetConverter, Action<ISession, TPacket> onRecv = null, Action<ISession> onAccepted = null)
        {
            _SessionContainer = new SessionContainer<Session<TPacket>>();
            _IP = ip;
            _Port = port;
            _PacketConverter = packetConverter;
            
            _OnRecv = onRecv;
            _OnAccepted = onAccepted;
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

                    Session<TPacket> session = new Session<TPacket>(acceptedClient, _PacketConverter, onClose: Session_Closed);
                    session.Received += Session_Received;

                    //if (_SessionContainer.AddSession(session))
                    //    _OnAccepted?.Invoke(session);
                    //else
                    //    session.Dispose();
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

        private void Session_Received(ISession session, TPacket packet)
        {
            _OnRecv?.Invoke(session, packet);
        }
    }


    class SessionContainer<TSession>
        where TSession : ISession
    {
        private readonly ConcurrentDictionary<Guid, TSession> _SessionMap;

        public SessionContainer()
        {
            _SessionMap = new ConcurrentDictionary<Guid, TSession>(new GuidEqualityComparer());
        }

        public IEnumerable<TSession> GetSessions()
        {
            return _SessionMap.Values;
        }

        public bool GetSession(Guid sessionId, out TSession session)
        {
            return _SessionMap.TryGetValue(sessionId, out session);
        }

        public bool AddSession(TSession session)
        {
            return _SessionMap.TryAdd(session.SessionId, session);
        }

        public bool RemoveSession(TSession session)
        {
            return RemoveSessionInner(session.SessionId, out _);
        }

        public bool RemoveSession(Guid sessionId, out TSession session)
        {
            return RemoveSessionInner(sessionId, out session);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool RemoveSessionInner(Guid sessionId, out TSession session)
        {
            bool result = _SessionMap.TryRemove(sessionId, out session);
            session?.Dispose();

            return result;
        }

        public void Clear()
        {
            foreach (var session in _SessionMap.Values)
                session.Dispose();

            _SessionMap.Clear();
        }
    }


    class GuidEqualityComparer : IEqualityComparer<Guid>
    {
        public bool Equals(Guid x, Guid y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(Guid obj)
        {
            return obj.GetHashCode();
        }
    }
}
