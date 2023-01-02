using SimpleSock.Interfaces;
using SimpleSock.Models;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace SimpleSock
{
    public class SimpleSockClient<TPacket>
    {
        private readonly IPacketConverter<TPacket> _PacketConverter;
        private readonly SemaphoreSlim _AsyncLock = new SemaphoreSlim(1, 1);

        private readonly string _IP;
        private readonly int _Port;

        private readonly Action<ISession, TPacket> _OnRecv;
        private readonly Action<ISession, TPacket> _OnSent;
        private readonly Action<ISession> _OnClosed;
        private readonly Action<string> _OnEvent;
        private readonly Action<Exception> _OnError;

        private ISession<TPacket> _Session;


        public SimpleSockClient(
            string ip
            , int port
            , IPacketConverter<TPacket> packetConverter
            , Action<ISession, TPacket> onRecv = null
            , Action<ISession, TPacket> onSent = null
            , Action<ISession> onClose = null
            , Action<string> onEvent = null
            , Action<Exception> onError = null)
        {
            _IP = ip;
            _Port = port;
            _PacketConverter = packetConverter;

            _OnRecv = onRecv ?? new Action<ISession, TPacket>((s, p) => { });
            _OnSent = onSent ?? new Action<ISession, TPacket>((s, p) => { });
            _OnClosed = onClose ?? new Action<ISession>((s) => { });
            _OnEvent = onEvent ?? new Action<string>((m) => { });
            _OnError = onError ?? new Action<Exception>((e) => { });
        }

        public async Task ConnectAsync()
        {
            try
            {
                await _AsyncLock.WaitAsync();

                if (_Session != null)
                    return;

                _OnEvent?.Invoke($"try connect to IP: {_IP}, Port: {_Port}");

                TcpClient client = new TcpClient();
                await client.ConnectAsync(_IP, _Port);

                _OnEvent?.Invoke($"connected IP: {_IP}, Port: {_Port}");

                // Session 생성
                var session = new Session<TPacket>(
                    client: client
                    , packetConverter: _PacketConverter
                    , onRecv: _OnRecv
                    , onSent: _OnSent
                    , onClose: _OnClosed
                    , onError: _OnError
                    , onEvent: _OnEvent
                );
                _Session = session;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                _AsyncLock.Release();
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                await _AsyncLock.WaitAsync();

                if (_Session == null)
                    return;

                await _Session.CloseAsync();

                _Session.Dispose();
                _Session = null;
            }
            catch (Exception ex)
            {
                _OnError.Invoke(ex);
            }
            finally
            {
                _AsyncLock.Release();
            }
        }

        public Task<int> SendAsync(TPacket packet)
        {
            if (_Session == null)
                return Task.FromResult(0);

            return _Session.SendAsync(packet);
        }

    }
}
