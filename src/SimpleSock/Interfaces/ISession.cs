using SimpleSock.Enums;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SimpleSock.Interfaces
{
    public interface ISession : IDisposable
    {
        Guid SessionId { get; }
        SessionState State { get; }
        IPEndPoint RemoteEndPoint { get; }
        IPEndPoint LocalEndPoint { get; }

        Task<int> SendAsync(byte[] bytes);
        Task CloseAsync();
    }
}
