using System;
using System.Runtime.CompilerServices;

namespace SimpleSock.Helpers
{
    internal static class ExceptionHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowExceptionIfIsNull(object target)
        {
            if (target is null)
                throw new NullReferenceException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowExceptionIfIsNull(object target, string message)
        {
            if (target is null)
                throw new NullReferenceException(message);
        }
    }
}
