using System;
using System.Collections.Concurrent;
using System.Threading;

namespace MicroBenchmarks
{
    // Taken from Steven Toubs blog post: https://devblogs.microsoft.com/pfxteam/coalescing-cancellationtokens-from-timeouts/
    // Slightly adapted to use newer language features
    internal static class CoalescedTokens
    {
        private const uint COALESCING_SPAN_MS = 50;
        private readonly static ConcurrentDictionary<long, CancellationToken> timeToToken = new ConcurrentDictionary<long, CancellationToken>();

        internal static CancellationToken FromTimeout(int millisecondsTimeout)
        {
            if (millisecondsTimeout <= 0)
                return new CancellationToken(true);

            uint currentTime = (uint)Environment.TickCount;
            long targetTime = millisecondsTimeout + currentTime;
            targetTime = ((targetTime + (COALESCING_SPAN_MS - 1)) / COALESCING_SPAN_MS) * COALESCING_SPAN_MS;

            if (!timeToToken.TryGetValue(targetTime, out var token))
            {
                token = new CancellationTokenSource((int)(targetTime - currentTime)).Token;

                if (timeToToken.TryAdd(targetTime, token))
                    token.Register(state => timeToToken.TryRemove((long)state, out _), targetTime);
            }

            return token;
        }
    }
}
