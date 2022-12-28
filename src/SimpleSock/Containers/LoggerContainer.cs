using SimpleSock.Implements;
using SimpleSock.Interfaces;

namespace SimpleSock.Containers
{
    public class LoggerContainer
    {
        public ILogger GetLogger()
        {
            return new ConsoleLogger();
        }
    }
}
