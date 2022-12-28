using SimpleSock.Interfaces;
using System;
using System.Threading.Tasks;

namespace SimpleSock.Test.Implements
{
    public class ConsoleLogger : ILogger
    {
        public void Write(string logText)
        {
            Console.WriteLine(logText);
        }

        public Task WriteAsync(string logText)
        {
            return Task.Run(() => Write(logText));
        }
    }
}
