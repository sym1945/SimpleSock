using SimpleSock.Buffers;
using SimpleSock.Enums;
using SimpleSock.Extensions;
using SimpleSock.Interfaces;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace SimpleSock.Models
{
    public abstract class Session2<TPacket> : ISession<TPacket>
    {
        private readonly Guid _SessionId;
        private readonly TcpClient _Client;
        private readonly NetworkStream _Stream;
        private readonly IPacketConverter<TPacket> _PacketConverter;
        private readonly CancellationTokenSource _SessionTaskCancller;
        
        private int _Disposed = 0;
        private int _Closed = 0;
        private Task _RecvTask;


        public Guid SessionId
        {
            get { return _SessionId; }
        }

        public SessionState State
        {
            get
            {
                if (_Closed > 0)
                    return SessionState.NotConnected;
                else
                    return SessionState.Connected;
            }
        }

        public IPEndPoint RemoteEndPoint { get; }

        public IPEndPoint LocalEndPoint { get; }

        internal Action<ISession> SessionClosed;


        public Session2(TcpClient client, IPacketConverter<TPacket> packetConverter)
        {
            _SessionId = Guid.NewGuid();
            _Client = client;
            _Stream = client.GetStream();
            _PacketConverter = packetConverter;

            RemoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
            LocalEndPoint = (IPEndPoint)client.Client.LocalEndPoint;

            _SessionTaskCancller = new CancellationTokenSource();
        }
        ~Session2()
        {
            Dispose(false);
        }

        public void StartReceive()
        {
            if (_Closed > 0)
                return;
            if (_RecvTask != null)
                return;

            _RecvTask = DoReceiveAsync(_Stream, _SessionTaskCancller.Token)
                            .ContinueWith(t =>
                            {
                                // 상대측에서 먼저 끊는 경우 Close 호출 안될 수도 있으니... 
                                Close();

                                // 완전 닫혀다고 외부에 알리자...
                                Closed();
                            });
        }

        private async Task DoReceiveAsync(NetworkStream netStream, CancellationToken cancelToken)
        {
            ReceiveBuffer recvBuffer = null;

            try
            {
                recvBuffer = new ReceiveBuffer(4096);
                var byteConsumed = 0;

                OnLog($"start receive task... {this}");

                while (!cancelToken.IsCancellationRequested && netStream.CanRead)
                {
                    recvBuffer.CheckRemainingSize();

                    var bytesRead = await netStream.ReadAsync(
                        recvBuffer.Bytes
                        , recvBuffer.BytesBuffered
                        , recvBuffer.BytesRemaining
                        , cancelToken
                    );
                    if (bytesRead == 0)
                        break;

                    recvBuffer.Accumulate(bytesRead);

                    if (_Closed > 0)
                        break;

                    while (_PacketConverter.Filter(recvBuffer.Bytes, recvBuffer.BytesBuffered, ref byteConsumed, out TPacket packet))
                    {
                        OnRecv(this, packet);
                    }

                    if (byteConsumed > 0)
                    {
                        recvBuffer.Consume(byteConsumed);
                        byteConsumed = 0;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
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
                        OnError(se);
                    }
                }
                else
                {
                    OnError(ex);
                }
            }
            finally
            {
                recvBuffer?.Dispose();

                OnLog($"stop receive task... {this}");
            }
        }

        public Task<int> SendAsync(TPacket packet)
        {
            if (_Closed > 0)
                return Task.FromResult(0);

            return SendAsyncInner(_Stream, packet, _SessionTaskCancller.Token);
        }

        protected async Task<int> SendAsyncInner(NetworkStream netStream, TPacket packet, CancellationToken cancelToken)
        {
            if (_Closed > 0 || !netStream.CanWrite)
                return 0;

            var bytes = _PacketConverter.ToBytes(packet);

            var sentBytes = 0;

            try
            {
                await netStream.WriteAsync(bytes, 0, bytes.Length, cancelToken);
                sentBytes = bytes.Length;
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
                        throw ex;
                    }
                }
                else
                {
                    throw ex;
                }
            }

            if (sentBytes > 0)
            {
                OnSent(this, packet);
            }

            return sentBytes;
        }

        private void Close()
        {
            if (Interlocked.CompareExchange(ref _Closed, 1, 0) == 1)
                return;

            _Stream.Close();
            _Client.Close();
        }

        private void Closed()
        {
            OnLog($"session closed... {this}");
            OnClosed(this);
            SessionClosed?.Invoke(this);
        }

        public async Task CloseAsync()
        {
            if (!_SessionTaskCancller.IsCancellationRequested)
                _SessionTaskCancller.Cancel();

            // NetworkStream.ReadAsync 대기중엔 CancelToken에 반응없음...
            // 그냥 강제로 Stream Close 시킨다.
            Close();

            if (_RecvTask != null)
            {
                if (!_RecvTask.IsCompleted)
                    await _RecvTask;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _Disposed, 1, 0) == 1)
                return;

            var closeTask = CloseAsync()
                .ContinueWith(t =>
                {
                    _SessionTaskCancller.Dispose();

                    if (_RecvTask != null)
                    {
                        _RecvTask.Dispose();
                        _RecvTask = null;
                    }

                    _Stream.Dispose();
                    _Client.Dispose();

                    SessionClosed = null;
                });

            if (disposing)
            {
            }
            else
            {
            }
        }

        protected virtual void OnRecv(ISession session, TPacket packet)
        {
        }

        protected virtual void OnSent(ISession session, TPacket packet)
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

        public override string ToString()
        {
            return $"{{ sessionId: {_SessionId}, endPoint: {RemoteEndPoint.ToString()} }}";
        }

    }
}
