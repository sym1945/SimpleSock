namespace SimpleSock.Interfaces
{
    public interface IPacketConverter<T>
    {
        bool Filter(byte[] buffer, int bytesBufferd, ref int bytesOffset, out T packet);

        byte[] ToBytes(T packet);
    }
}
