namespace SimpleSock.Interfaces
{
    public interface IRecieveFilter<T>
    {
        bool Filter(byte[] buffer, int bytesBufferd, ref int bytesOffset, out T data);
    }
}
