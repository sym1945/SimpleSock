using SimpleSock.Interfaces;
using SimpleSock.Models;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleSock.Implements
{
    public class DefaultReceiveFilter : IRecieveFilter<Packet>
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
    }

}
