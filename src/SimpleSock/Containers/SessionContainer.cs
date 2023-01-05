using SimpleSock.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SimpleSock.Containers
{
    class SessionContainer<TSession>
        where TSession : ISession
    {
        private readonly ConcurrentDictionary<Guid, TSession> _SessionMap;

        public int SessionCount
        {
            get { return _SessionMap.Count; }
        }

        public SessionContainer()
        {
            _SessionMap = new ConcurrentDictionary<Guid, TSession>(new GuidEqualityComparer());
        }

        public IEnumerable<TSession> GetSessions()
        {
            return _SessionMap.Values;
        }

        public bool GetSession(Guid sessionId, out TSession session)
        {
            return _SessionMap.TryGetValue(sessionId, out session);
        }

        public bool AddSession(TSession session)
        {
            return _SessionMap.TryAdd(session.SessionId, session);
        }

        public bool RemoveSession(TSession session)
        {
            return RemoveSessionInner(session.SessionId, out _);
        }

        public bool RemoveSession(Guid sessionId, out TSession session)
        {
            return RemoveSessionInner(sessionId, out session);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool RemoveSessionInner(Guid sessionId, out TSession session)
        {
            bool result = _SessionMap.TryRemove(sessionId, out session);
            session?.Dispose();

            return result;
        }

        public void Clear()
        {
            foreach (var session in _SessionMap.Values)
                session.Dispose();

            _SessionMap.Clear();
        }
    }

    class GuidEqualityComparer : IEqualityComparer<Guid>
    {
        public bool Equals(Guid x, Guid y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(Guid obj)
        {
            return obj.GetHashCode();
        }
    }
}
