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
    public class Session<TPacket> : ISession<TPacket>
    {
        private readonly Guid _SessionId;
        private readonly TcpClient _Client;
        private readonly NetworkStream _Stream;
        private readonly IPacketConverter<TPacket> _PacketConverter;

        private readonly Action<ISession, TPacket> _OnRecv;
        private readonly Action<ISession, TPacket> _OnSent;
        private readonly Action<ISession> _OnClosed;
        private readonly Action<string> _OnLog;
        private readonly Action<Exception> _OnError;

        private bool _Disposed, _IsDisposing;
        private bool _Closed, _IsClosing;
        private Task _RecvTask;
        private CancellationTokenSource _RecvTaskCancller;


        public Guid SessionId
        {
            get { return _SessionId; }
        }

        public SessionState State
        {
            get
            {
                if (_Closed)
                    return SessionState.NotConnected;
                else
                    return SessionState.Connected;
            }
        }

        public IPEndPoint RemoteEndPoint { get; }

        public IPEndPoint LocalEndPoint { get; }



        public Session(
            TcpClient client
            , IPacketConverter<TPacket> packetConverter
            , Action<ISession, TPacket> onRecv = null
            , Action<ISession, TPacket> onSent = null
            , Action<ISession> onClose = null
            , Action<string> onLog = null
            , Action<Exception> onError = null)
        {
            _SessionId = Guid.NewGuid();
            _Client = client;
            _Stream = client.GetStream();
            _PacketConverter = packetConverter;

            RemoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
            LocalEndPoint = (IPEndPoint)client.Client.LocalEndPoint;

            _OnRecv = onRecv ?? new Action<ISession, TPacket>((s, p) => { });
            _OnSent = onSent ?? new Action<ISession, TPacket>((s, p) => { });
            _OnClosed = onClose ?? new Action<ISession>((s) => { });
            _OnLog = onLog ?? new Action<string>((m) => { });
            _OnError = onError ?? new Action<Exception>((e) => { });
        }
        ~Session()
        {
            Dispose(false);
        }

        internal void StartReceive()
        {
            if (_Closed)
                return;
            if (_RecvTask != null || _RecvTaskCancller != null)
                return;

            _RecvTaskCancller = new CancellationTokenSource();
            _RecvTask = DoReceiveAsync(_Stream, _RecvTaskCancller.Token).ContinueWith(t => Close());
        }

        private async Task DoReceiveAsync(NetworkStream netStream, CancellationToken cancelToken)
        {
            ReceiveBuffer recvBuffer = null;

            try
            {
                recvBuffer = new ReceiveBuffer(4096);
                var byteConsumed = 0;

                _OnLog.Invoke($"start receive task... {this}");

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

                    if (_Closed)
                        break;

                    while (_PacketConverter.Filter(recvBuffer.Bytes, recvBuffer.BytesBuffered, ref byteConsumed, out TPacket packet))
                    {
                        _OnRecv.Invoke(this, packet);
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
                        _OnError.Invoke(se);
                    }
                }
                else
                {
                    _OnError.Invoke(ex);
                }
            }

            recvBuffer?.Dispose();

            _OnLog.Invoke($"stop receive task... {this}");
        }

        public Task<int> SendAsync(TPacket packet)
        {
            if (_RecvTaskCancller == null)
                return Task.FromResult(0);

            return SendAsyncInner(_Stream, packet, _RecvTaskCancller.Token);
        }

        //public Task<int> SendAsync(byte[] bytes)
        //{
        //    if (_RecvTaskCancller == null)
        //        return Task.FromResult(0);

        //    return SendAsyncInner(_Stream, bytes, 0, bytes.Length, _RecvTaskCancller.Token);
        //}

        protected async Task<int> SendAsyncInner(NetworkStream netStream, TPacket packet, CancellationToken cancelToken)
        {
            if (_Closed || !netStream.CanWrite)
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
                _OnSent.Invoke(this, packet);
            }

            return sentBytes;
        }

        private void Close()
        {
            if (_Closed || _IsClosing)
                return;
            _IsClosing = true;

            _Stream.Close();
            _Client.Close();

            _Stream.Dispose();
            _Client.Dispose();

            _Closed = true;
            _IsClosing = false;

            _OnLog.Invoke($"session closed... {this}");
            _OnClosed.Invoke(this);
        }

        public async Task CloseAsync()
        {
            if (_RecvTaskCancller == null || _RecvTask == null)
            {
                // StartReceive 호출 전 Close 된 경우
                Close();
                return;
            }

            _RecvTaskCancller.Cancel();
            _Stream.Close();

            await _RecvTask;

            _RecvTaskCancller.Dispose();
            _RecvTaskCancller = null;
            _RecvTask = null;
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed || _IsDisposing)
                return;
            _IsDisposing = true;

            var closeTask = CloseAsync();
            if (disposing)
            {
            }
            else
            {
            }

            _Disposed = true;
            _IsDisposing = false;
        }

        public override string ToString()
        {
            return $"{{ sessionId: {_SessionId}, endPoint: {RemoteEndPoint.ToString()} }}";
        }

    }
}
