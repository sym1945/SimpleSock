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
    public class Session<T> : ISession
    {
        private readonly TcpClient _Client;
        private readonly NetworkStream _Stream;
        private readonly IPacketConverter<T> _PacketConverter;

        private bool _Disposed, _IsDisposing;
        private bool _Closed, _IsClosing;
        private bool _Connected = true;
        private Task _RecvTask;
        private CancellationTokenSource _RecvTaskCancller;


        public SessionState State
        {
            get
            {
                if (_Connected == false)
                    return SessionState.NotConnected;
                else
                    return SessionState.Connected;
            }
        }

        public IPEndPoint RemoteEndPoint { get; }

        public IPEndPoint LocalEndPoint { get; }


        public event Action<ISession, T> Received;


        public Session(TcpClient client, IPacketConverter<T> packetConverter)
        {
            _Client = client;
            _Stream = client.GetStream();
            _PacketConverter = packetConverter;

            RemoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
            LocalEndPoint = (IPEndPoint)client.Client.LocalEndPoint;

            BeginReceive();
        }
        ~Session()
        {
            Dispose(false);
        }

        public void BeginReceive()
        {
            if (_Connected == false)
                return;
            if (_RecvTask != null || _RecvTaskCancller != null)
                return;

            _RecvTaskCancller = new CancellationTokenSource();
            _RecvTask = DoReceiveAsync(_Stream, _RecvTaskCancller.Token).ContinueWith(t => Close());
        }

        private async Task DoReceiveAsync(NetworkStream netStream, CancellationToken cancelToken)
        {
            var recvBuffer = new ReceiveBuffer(4096);
            var byteConsumed = 0;
            //_Logger.WriteDebugLog($"START RECEIVE, Session={this}");

            try
            {
                while (!cancelToken.IsCancellationRequested && netStream.CanRead)
                {
                    //_Logger.WriteDebugLog($"BEGIN RECEIVE, Session={this}");
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
                    //_Logger.WriteDebugLog($"END RECEIVE, Session={this}");

                    if (!_Connected)
                        break;

                    while (_PacketConverter.Filter(recvBuffer.Bytes, recvBuffer.BytesBuffered, ref byteConsumed, out T packet))
                    {
                        // do something
                        if (Received != null)
                            Received.Invoke(this, packet);
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
                        //_Logger.WriteErrorLog(se);
                    }

                }
                else
                {
                    //    _Logger.WriteErrorLog(ex);
                }
            }

            recvBuffer.Dispose();

            //_Logger.WriteDebugLog($"STOP RECEIVE, Session={this}");
        }

        public Task<int> SendAsync(T packet)
        {
            return SendAsync(_PacketConverter.ToBytes(packet));
        }

        public Task<int> SendAsync(byte[] bytes)
        {
            if (_RecvTaskCancller == null)
                return Task.FromResult(0);

            return SendAsyncInner(_Stream, bytes, 0, bytes.Length, _RecvTaskCancller.Token);
        }

        protected async Task<int> SendAsyncInner(NetworkStream netStream, byte[] bytes, int offset, int count, CancellationToken cancelToken)
        {
            if (_Connected == false || !netStream.CanWrite)
                return 0;

            var sentBytes = 0;

            try
            {
                await netStream.WriteAsync(bytes, offset, count, cancelToken);
                sentBytes = count;
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
                        throw ex;
                    }
                }
                else
                {
                    //_Logger.WriteErrorLog(ex);
                    throw ex;
                }
            }

            if (sentBytes > 0)
            {
                //if (!option.PacketLogging.HasFlag(PacketLogging.None))
                //{
                //    bool includeMessage = (option.PacketLogging == PacketLogging.Default
                //                        || option.PacketLogging.HasFlag(PacketLogging.Message)
                //    );
                //    //_Logger.WriteSendLog(this, packet.ToString(includeMessage));
                //}
            }

            return sentBytes;
        }

        protected virtual void Close()
        {
            if (_Closed || _IsClosing)
                return;
            _IsClosing = true;

            //_Logger.WriteDebugLog($"START CLOSE, Session={this}");
            try
            {
                //_Client.Client.Shutdown(SocketShutdown.Both);
                _Stream.Close();
                _Client.Close();

                _Stream.Dispose();
                _Client.Dispose();
            }
            catch (Exception ex)
            {
                //_Logger.WriteErrorLog(ex);
            }
            
            _Closed = true;
            _IsClosing = false;

            _Connected = false;
            //_Logger.WriteDebugLog($"END CLOSE, Session={this}");
        }

        public async Task CloseAsync()
        {
            if (_RecvTaskCancller == null || _RecvTask == null)
                return;

            _RecvTaskCancller.Cancel();

            await _RecvTask;

            _RecvTaskCancller.Dispose();
            _RecvTaskCancller = null;
            _RecvTask = null;
        }


        public void Dispose()
        {
            Dispose(true);
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
            return RemoteEndPoint.ToString();
        }

    }
}
