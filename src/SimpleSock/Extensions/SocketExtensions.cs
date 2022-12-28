using System.Net.Sockets;

namespace SimpleSock.Extensions
{
    internal static class SocketExtensions
    {
        internal static bool IsIgnorableSocketException(this SocketException se)
        {
            switch (se.SocketErrorCode)
            {
                case SocketError.OperationAborted:
                case SocketError.ConnectionRefused:
                case SocketError.ConnectionAborted:
                case SocketError.ConnectionReset:
                case SocketError.NetworkReset:
                case SocketError.Shutdown:
                    return true;
                default:
                    return false;
            }
        }
    }
}
