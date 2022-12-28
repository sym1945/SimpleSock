using SimpleSock.Buffers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSock.Models
{
    public class Session
    {
        private readonly TcpClient _Client;
        private readonly NetworkStream _Stream;
        private ReceiveBuffer _RecvBuffer;
        private bool _Disposed, _IsDisposing;
        private bool _Closed, _IsClosing;
        private bool _Disconnecting;
        private bool _Connected = true;
        private Task _RecvTask;
        private CancellationTokenSource _RecvTaskCancller;

        public SessionState State
        {
            get
            {
                if (_Connected == false)
                    return SessionState.Disconnected;

                if (Type == SessionType.Unknown)
                    return SessionState.NotCertified;
                else
                    return SessionState.Certified;
            }
        }

        public IPEndPoint RemoteEndPoint { get; }

        public IPEndPoint LocalEndPoint { get; }


        public Session(TcpClient client)
        {
            _Client = client;
            _Stream = client.GetStream();

            RemoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
            LocalEndPoint = (IPEndPoint)client.Client.LocalEndPoint;
        }

        public void StartReceive()
        {
            if (_Connected == false)
                return;
            if (_RecvTask != null)
                return;

            _RecvTask = Task.Run(StartReceiveAsync);
            WaitHandle();
        }

        private async Task StartReceiveAsync()
        {
            _RecvBuffer = new ReceiveBuffer();
            _RecvTaskCancller = new CancellationTokenSource();
            var cancelToken = _RecvTaskCancller.Token;

            //_Logger.WriteDebugLog($"START RECEIVE, Session={this}");

            try
            {
                while (_Connected)
                {
                    //_Logger.WriteDebugLog($"BEGIN RECEIVE, Session={this}");
                    _RecvBuffer.CheckRemainingSize();
                    var bytesRead = await _Stream.ReadAsync(
                        _RecvBuffer.Bytes, _RecvBuffer.BytesBuffered, _RecvBuffer.BytesRemaining, cancelToken
                    );
                    _RecvBuffer.Accumulate(bytesRead);
                    //_Logger.WriteDebugLog($"END RECEIVE, Session={this}");

                    if (bytesRead == 0)
                        break;
                    if (!_Connected)
                        break;

                    var packets = PacketParser.Parse(_RecvBuffer.Bytes, _RecvBuffer.BytesBuffered, out int byteConsumed);

                    foreach (var packet in packets)
                    {
                        //_Logger.WriteRecvLog(this, packet.ToString());

                        //_Logger.WriteDebugLog($"BEGIN PROCESS, Session={this}");
                        //_Events.OnPacketReceived(this, packet);
                        //_Logger.WriteDebugLog($"END PROCESS, Session={this}");

                        //_PacketAwaiter.Release(packet);
                    }

                    if (byteConsumed > 0)
                        _RecvBuffer.Consume(byteConsumed);
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
                //if (ex.InnerException is SocketException se)
                //{
                //    if (se.IsIgnorableSocketException() == false)
                //        _Logger.WriteErrorLog(se);
                //}
                //else
                //    _Logger.WriteErrorLog(ex);
            }

            //_Logger.WriteDebugLog($"STOP RECEIVE, Session={this}");
        }

        public Task<int> SendAsync(Packet packet, SendOption option = default(SendOption))
        {
            return SendAsyncInner(packet, option);
        }
        protected async Task<int> SendAsyncInner(Packet packet, SendOption option)
        {
            if (_Connected == false)
                return 0;

            if (option.AutoPacketId)
                packet.Header.PacketId = GetNextPacketId();

            var sentBytes = 0;
            var segment = new ArraySegment<byte>(packet.ToBytes());

            try
            {
                await _Stream.WriteAsync(segment.Array, 0, segment.Count);
                sentBytes = segment.Count;
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
                        _Logger.WriteErrorLog(se);
                        throw ex;
                    }
                }
                else
                {
                    _Logger.WriteErrorLog(ex);
                    throw ex;
                }
            }

            if (sentBytes > 0)
            {
                if (!option.PacketLogging.HasFlag(PacketLogging.None))
                {
                    bool includeMessage = (option.PacketLogging == PacketLogging.Default
                                        || option.PacketLogging.HasFlag(PacketLogging.Message)
                    );
                    _Logger.WriteSendLog(this, packet.ToString(includeMessage));
                }
            }

            return sentBytes;
        }

        private async void WaitHandle()
        {
            await ClosingHandleAsync();
        }

        private async Task ClosingHandleAsync()
        {
            try
            {
                await _RecvTask;
            }
            catch (Exception ex)
            {
                //_Logger.WriteErrorLog(ex);
            }
            finally
            {
                if (!_Closed && !_IsClosing)
                    Close();

                if (!_Disposed && !_IsDisposing)
                {
                    Dispose(true);
                    //_Events.OnSessionDisconnected(this);
                    //_Logger.WriteDebugLog("disconnected");
                }
            }
        }

        public Task<Packet> WaitReplyAsync(Packet packet, string replyPackeId)
        {
            //if (_Connected == false)
            //    return TaskExtensions.CompletedTask<Packet>();

            //return _PacketAwaiter.WaitAsync(this, packet, replyPackeId);
        }

        protected virtual void Close()
        {
            if (_IsClosing || _Closed || _Disposed)
                return;
            _IsClosing = true;

            //_Logger.WriteDebugLog($"START CLOSE, Session={this}");
            try
            {
                _Client.Client.Shutdown(SocketShutdown.Both);
                _Stream.Close();
                _Client.Close();
            }
            catch (Exception ex)
            {
                //_Logger.WriteErrorLog(ex);
            }

            _Connected = false;
            _Closed = true;
            _IsClosing = false;

            //_Logger.WriteDebugLog($"END CLOSE, Session={this}");
        }

        public async Task DisconnectAsync()
        {
            if (_Disconnecting || _Disposed || !_Connected)
                return;
            _Disconnecting = true;

            _RecvTaskCancller.Cancel();
            Close();

            await ClosingHandleAsync();

            _Disconnecting = false;
        }

        public Task CloseAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            DisconnectAsync().GetAwaiter().GetResult();
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_IsDisposing || _Disposed)
                return;
            _IsDisposing = true;

            if (disposing)
            {
                //_Logger.WriteDebugLog($"START DISPOSE, Session={this}");
                _RecvBuffer?.Dispose();
                //_PacketAwaiter?.Dispose();
                _Client.Dispose();
                //_Logger.WriteDebugLog($"END DISPOSE, Session={this}");
            }

            _Disposed = true;
            _IsDisposing = false;
        }

        public override string ToString()
        {
            return RemoteEndPoint.ToString();
        }

        protected virtual ushort GetNextPacketId()
        {
            if (_PrimaryPacketId == ushort.MaxValue)
                _PrimaryPacketId = 0;
            return ++_PrimaryPacketId;
        }
    }
}
