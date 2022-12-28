using SimpleSock.Helpers;
using SimpleSock.Interfaces;
using SimpleSock.Test.Models;
using System;
using System.Text;

namespace SimpleSock.Test.Implements
{
    public class PacketConverterSample : IPacketConverter<Packet>
    {
        public bool Filter(byte[] buffer, int bytesBufferd, ref int bytesOffset, out Packet data)
        {
            data = null;
            if (buffer == null || bytesBufferd < PacketHeader.Size + PacketTail.Size)
                return false;

            int iStx = -1;
            iStx = Array.IndexOf(buffer, PacketHeader.STX, bytesOffset);
            if (iStx < 0)
                return false;

            uint dataSize = BitConverter.ToUInt16(buffer, iStx + 1);

            int requiredLength = iStx + PacketHeader.Size + (int)dataSize + PacketTail.Size;
            if (requiredLength > bytesBufferd)
                return false;

            try
            {
                //byte[] bytes = new byte[requiredLength];
                //Buffer.BlockCopy(buffer, iStx, bytes, 0, requiredLength);

                var packet = new Packet
                {
                    Header = new PacketHeader(
                        dataSize: dataSize,
                        protocolId: BitConverter.ToUInt16(buffer, iStx + 5),
                        packetId: BitConverter.ToUInt16(buffer, iStx + 7)
                    ),
                    //Bytes = bytes,
                    Data = Encoding.Unicode.GetString(buffer, iStx + 9, (int)dataSize),
                    Tail = new PacketTail(buffer[requiredLength - PacketTail.Size])
                };

                data = packet;

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                bytesOffset = requiredLength;
            }
        }

        public byte[] ToBytes(Packet packet)
        {
            var totalSize = packet.GetSize();
            byte[] bytes = new byte[totalSize];

            // Header
            bytes[0] = packet.Header.Stx;
            BinHelper.GetBytes(packet.Header.DataSize, bytes, 1);
            BinHelper.GetBytes(packet.Header.ProtocolId, bytes, 5);
            BinHelper.GetBytes(packet.Header.PacketId, bytes, 7);

            // Data
            if (packet.Data != null)
                Encoding.Unicode.GetBytes(packet.Data, 0, packet.Data.Length, bytes, PacketHeader.Size);

            // Tail
            bytes[totalSize - 1] = packet.Tail.Etx;

            return bytes;
        }
    }

}
