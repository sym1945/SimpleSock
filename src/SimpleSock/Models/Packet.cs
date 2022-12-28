using System;
using System.Text;

namespace SimpleSock.Models
{
    public class Packet
    {
        private static ushort _SendPacketId = 2;

        public PacketHeader Header { get; set; }
        public string Data { get; set; }
        public byte[] Bytes { get; set; }
        public PacketTail Tail { get; set; }

        public Packet()
        {
            Tail = PacketTail.Default;
        }

        public Packet(byte[] bytes)
        {
            Header = new PacketHeader(
                stx: bytes[0],
                dataSize: BitConverter.ToUInt16(bytes, 1),
                protocolId: BitConverter.ToUInt16(bytes, 5),
                packetId: BitConverter.ToUInt16(bytes, 7)
            );
            Data = Encoding.Unicode.GetString(bytes, 9, (int)Header.DataSize);
            Tail = new PacketTail(bytes[PacketHeader.Size + Header.DataSize]);
        }

        public int GetSize()
        {
            int dataSize = (Data != null) ? Encoding.Unicode.GetByteCount(Data) : 0;

            int result = PacketHeader.Size + PacketTail.Size + dataSize;
            return result;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Header: {Header}");
            sb.AppendLine($"Data: {Data}");
            sb.AppendLine($"Tail: {Tail}");
            return sb.ToString();
        }

        public static Packet CreateLinktestRsp(ushort packetId)
        {
            var packet = new Packet
            {
                Header = new PacketHeader(
                    dataSize: 0,
                    protocolId: (ushort)ProtocolList.LINKTEST_RSP,
                    packetId: packetId
                )
            };

            return packet;
        }

        public static Packet CreateReply(Packet request)
        {
            var packet = new Packet
            {
                Header = new PacketHeader(
                    dataSize: request.Header.DataSize,
                    protocolId: (ushort)ProtocolList.SEND_REQ,
                    packetId: request.Header.PacketId
                ),
                Data = request.Data
            };

            return packet;
        }

        public static Packet CreateSendPacket(string data)
        {
            var packet = new Packet
            {
                Header = new PacketHeader(
                    dataSize: (data != null) ? (uint)Encoding.Unicode.GetByteCount(data) : 0,
                    protocolId: (ushort)ProtocolList.SEND_REQ,
                    packetId: _SendPacketId
                ),
                Data = data
            };

            _SendPacketId += 2;

            return packet;
        }
    }

    public enum ProtocolList
    {
        SOCKET_CLOSING = 1,
        SOCKET_CLOSED = 2,
        LINKTEST_REQ = 3,
        LINKTEST_RSP = 4,
        SEND_REQ = 11,
        SEND_RSP = 12,
        SEND_ACK = 13,

        FILE_SEND_REQ = 5,
        FILE_SEND_RSP_OK = 6,
        FILE_SEND_RSP_FAIL = 7,
        FILE_SEND_RSP_HEAD = 8,
        FILE_SEND_RSP_BODY = 9,
        FILE_SEND_RSP_TAIL = 10,
        SEND_RSP_ACK = 13,
        USER_PACKET = 14
    };
}
