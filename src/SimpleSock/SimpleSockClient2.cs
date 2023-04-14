using SimpleSock.Enums;
using SimpleSock.Helpers;
using SimpleSock.Interfaces;
using SimpleSock.Models;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace SimpleSock
{
    public abstract class SimpleSockClient2<TPacket>
    {
        private readonly string _IP;
        private readonly int _Port;
        private readonly IPacketConverter<TPacket> _PacketConverter;
        private readonly object _Locker = new object();
        private int _IsConnecting;

        private ISession<TPacket> _Session;

        public ISession<TPacket> Session
        {
            get { return _Session; }
        }

        public bool IsConnected
        {
            get
            {
                lock (_Locker)
                {
                    if (_Session == null)
                        return false;

                    return _Session.State == SessionState.Connected;
                }
            }
        }

        public SimpleSockClient2(
            string ip
            , int port
            , IPacketConverter<TPacket> packetConverter)
        {
            ExceptionHelper.ThrowExceptionIfIsNull(ip);
            ExceptionHelper.ThrowExceptionIfIsNull(packetConverter);

            _IP = ip;
            _Port = port;
            _PacketConverter = packetConverter;
        }


        public async Task ConnectAsync()
        {
            try
            {
                if (Interlocked.CompareExchange(ref _IsConnecting, 1, 0) == 1)
                    return;
                if (IsConnected)
                    return;

                OnLog($"try connect to {{ ip: {_IP}, port: {_Port} }}");

                TcpClient client = new TcpClient();
                await client.ConnectAsync(_IP, _Port);

                OnLog($"connected {{ ip: {_IP}, port: {_Port} }}");

                // Session 생성
                var session = new Session<TPacket>(
                    client: client
                    , packetConverter: _PacketConverter
                    , onRecv: OnRecv
                    , onSent: OnSent
                    , onClose: OnSessionClosed
                    , onError: OnError
                    , onLog: OnLog
                );

                lock (_Locker)
                {
                    _Session = session;
                }

                OnConnected(_Session);

                session.StartReceive();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Interlocked.CompareExchange(ref _IsConnecting, 0, 1);
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if (!IsConnected)
                    return;

                await _Session.CloseAsync();
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        public Task<int> SendAsync(TPacket packet)
        {
            if (_Session == null)
                return Task.FromResult(0);

            return _Session.SendAsync(packet);
        }

        private void OnSessionClosed(ISession session)
        {
            lock (_Locker)
            {
                if (_Session != null
                    && _Session.SessionId == session.SessionId)
                {
                    _Session.Dispose();
                    _Session = null;
                }
            }

            OnClosed(session);
        }

        protected virtual void OnRecv(ISession session, TPacket packet)
        {
        }

        protected virtual void OnSent(ISession session, TPacket packet)
        {
        }

        protected virtual void OnConnected(ISession session)
        {
        }

        protected virtual void OnClosed(ISession session)
        {
        }

        protected virtual void OnError(Exception ex)
        {
        }

        protected virtual void OnLog(string log)
        {
        }

    }
}
