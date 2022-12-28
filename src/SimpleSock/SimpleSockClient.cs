using SimpleSock.Interfaces;
using SimpleSock.Models;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace SimpleSock
{
    public class SimpleSockClient<T>
    {
        private readonly IPacketConverter<T> _PacketConverter;
        private readonly SemaphoreSlim _AsyncLock = new SemaphoreSlim(1, 1);
        private readonly string _IP;
        private readonly int _Port;
        private Session<T> _Session;
        
        private Action<ISession, T> _OnRecv;


        public SimpleSockClient(string ip, int port, IPacketConverter<T> packetConverter, Action<ISession, T> onRecv = null)
        {
            _IP = ip;
            _Port = port;
            _PacketConverter = packetConverter;
            _OnRecv = onRecv;
        }

        public async Task ConnectAsync()
        {
            try
            {
                await _AsyncLock.WaitAsync();

                if (_Session != null)
                    return;

                //_Logger.Write($"TRY CONNECT...");
                TcpClient client = new TcpClient();
                await client.ConnectAsync(_IP, _Port);

                // Session 생성
                var session = new Session<T>(client, _PacketConverter);
                session.Received += Session_Received;

                _Session = session;
            }
            catch (Exception ex)
            {
                //_Logger.WriteErrorLog(ex);

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

                //_Logger.Write($"STOPPING...");

                await _Session.CloseAsync();

                _Session.Dispose();
                _Session = null;

                //_Logger.Write($"STOPPED");
            }
            catch (Exception ex)
            {
                //_Logger.WriteErrorLog(ex);
                throw ex;
            }
            finally
            {
                _AsyncLock.Release();
            }
        }

        public Task<int> SendAsync(T packet)
        {
            if (_Session == null)
                Task.FromResult(0);

            return _Session.SendAsync(packet);
        }

        private void Session_Received(ISession session, T packet)
        {
            if (_OnRecv != null)
                _OnRecv.Invoke(session, packet);
        }

    }
}
