using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSock.Interfaces
{
    public interface ILogger
    {
        Task WriteAsync(string logText);

        void Write(string logText);
    }
}
