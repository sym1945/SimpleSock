using SimpleSock.Buffers;
using SimpleSock.Helpers;
using SimpleSock.Implements;
using SimpleSock.Interfaces;
using SimpleSock.Models;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace SimpleSock
{
    public class SimpleSockClient
    {
        private readonly SemaphoreSlim _AsyncLock = new SemaphoreSlim(1, 1);
        //private TcpClient _TcpClient = null;
        //private NetworkStream _NetStream = null;
        //private Task _RecvTask = null;
        private string _IP = "127.0.0.1";
        private int _Port = 5020;
        private Session _Session;

        private readonly Action<Packet> _OnRecv;

        public SimpleSockClient(Action<Packet> onRecv)
        {
            _OnRecv = onRecv;
        }

        public void Connect()
        {
            ConnectAsync().GetAwaiter().GetResult();
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
                Session session = new Session(client);

                // Recv 시작


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

        private async Task DoReceiveAsync(NetworkStream netStream, CancellationToken cancelToken)
        {
            ReceiveBuffer recvBuffer = new ReceiveBuffer(4096);
            IRecieveFilter<Packet> recvFilter = new DefaultReceiveFilter();
            int byteConsumed = 0;

            while (!cancelToken.IsCancellationRequested)
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

                while (recvFilter.Filter(recvBuffer.Bytes, recvBuffer.BytesBuffered, ref byteConsumed, out Packet data))
                {
                    _OnRecv?.Invoke(data);
                }

                if (byteConsumed > 0)
                {
                    recvBuffer.Consume(byteConsumed);
                    byteConsumed = 0;
                }
            }

            recvBuffer.Dispose();
        }


        public void Disconnect()
        {
            DisconnectAsync().GetAwaiter().GetResult();
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

        public int Send(string data)
        {
            return 0;
        }

        public Task<int> SendAsync(Packet packet)
        {
            return Task.FromResult<int>(1);

            //if (_TcpClient == null || !_TcpClient.Connected || _NetStream == null)
            //    return -1;

            //var totalSize = packet.GetSize();
            //byte[] bytes = new byte[totalSize];

            //// Header
            //bytes[0] = packet.Header.Stx;
            //BinHelper.GetBytes(packet.Header.DataSize, bytes, 1);
            //BinHelper.GetBytes(packet.Header.ProtocolId, bytes, 5);
            //BinHelper.GetBytes(packet.Header.PacketId, bytes, 7);

            //// Data
            //if (packet.Data != null)
            //    Encoding.Unicode.GetBytes(packet.Data, 0, packet.Data.Length, bytes, PacketHeader.Size);

            //// Tail
            //bytes[totalSize - 1] = packet.Tail.Etx;

            //await _NetStream.WriteAsync(bytes, 0, bytes.Length);

            //return bytes.Length;
        }


    }
}
