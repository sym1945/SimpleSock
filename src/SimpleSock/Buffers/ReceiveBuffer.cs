using System;
using System.Buffers;


namespace SimpleSock.Buffers
{
    public class ReceiveBuffer : IDisposable
    {
        private int _BytesBuffered = 0;
        private byte[] _Buffer;

        public int BytesBuffered => _BytesBuffered;

        public int BytesRemaining => _Buffer.Length - _BytesBuffered;

        public byte[] Bytes => _Buffer;


        public ReceiveBuffer(int initLength = 4096)
        {
            _Buffer = ArrayPool<byte>.Shared.Rent(initLength);
        }

        /// <summary>
        /// 남은 Buffer 사이즈 확인해서 부족하면 2배 늘린다.
        /// </summary>
        public void CheckRemainingSize()
        {
            if (BytesRemaining == 0)
            {
                var newBuffer = ArrayPool<byte>.Shared.Rent(_Buffer.Length * 2);
                Buffer.BlockCopy(_Buffer, 0, newBuffer, 0, _Buffer.Length);
                ArrayPool<byte>.Shared.Return(_Buffer);
                _Buffer = newBuffer;
            }
        }

        /// <summary>
        /// bytesRead만큼 축적해 Buffered 포인터를 옮긴다.
        /// </summary>
        /// <param name="bytesRead"></param>
        public void Accumulate(int bytesRead)
        {
            _BytesBuffered += bytesRead;
        }

        /// <summary>
        /// bytesConsumed만큼 소비해 Buffered 포인터를 옮긴다.
        /// </summary>
        /// <param name="bytesConsumed"></param>
        public void Consume(int bytesConsumed)
        {
            if (bytesConsumed <= 0)
                return;

            if (bytesConsumed > _BytesBuffered)
                throw new Exception("byteComsumed is longer than buffer length");

            Buffer.BlockCopy(_Buffer, bytesConsumed, _Buffer, 0, _BytesBuffered - bytesConsumed);

            _BytesBuffered -= bytesConsumed;

            //Array.Clear(_Buffer, _BytesBuffered, BytesRemaining);
        }

        public void Dispose()
        {
            if (_Buffer != null)
            {
                ArrayPool<byte>.Shared.Return(_Buffer);
                _Buffer = null;
            }
        }
    }
}
