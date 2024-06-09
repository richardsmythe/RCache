using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RichardCache
{
    public class RCache<TKey, TValue> : IDisposable where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, CacheEntry<TValue>> _cache = new ConcurrentDictionary<TKey, CacheEntry<TValue>>();
        private readonly int _expirationMilliseconds;
        private readonly SemaphoreSlim _cleanupSemaphore = new SemaphoreSlim(1);
        private bool _disposed = false;

        public RCache() : this(60000) { }
        public RCache(int expirationMilliseconds)
            => _expirationMilliseconds = expirationMilliseconds;

        public int Count => _cache.Count;
        public IEnumerable<TKey> Keys => _cache.Keys;

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        {
            int attempt = 0;
            while (true)
            {
                if (_cache.TryGetValue(key, out var cacheEntry))
                {
                    if (cacheEntry.IsExpired(Environment.TickCount))
                    {
                        _cache.TryRemove(key, out _);
                    }
                    else
                    {
                        return cacheEntry.Value;
                    }
                }
                else
                {
                    var newCacheEntry = new CacheEntry<TValue>();
                    var existingCacheEntry = _cache.GetOrAdd(key, newCacheEntry);
                    // check if the new CacheEntry is the one actually added to the dictionary
                    if (ReferenceEquals(existingCacheEntry, newCacheEntry))
                    {
                        var value = factory(key);
                        var expirationTime = Environment.TickCount + _expirationMilliseconds;
                        newCacheEntry.SetValue(value, expirationTime);
                        return value;
                    }
                    else
                    {
                        Thread.Sleep(BackOffDelay(attempt));
                        attempt++;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TimeSpan BackOffDelay(int attempt)
        {
            int baseDelayMs = 100;
            int maxDelayMs = 10000;
            double maxDelay = baseDelayMs * Math.Pow(2, attempt);
            int tickCount = Environment.TickCount;
            double jitter = tickCount % 100;
            double delay = jitter * Math.Min(maxDelay, maxDelayMs);
            return TimeSpan.FromMilliseconds(delay);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _cleanupSemaphore.Wait();
                try
                {
                    foreach (var entry in _cache.Values)
                    {
                        if (entry.Value is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                    _cache.Clear();
                }
                finally
                {
                    _cleanupSemaphore.Release();
                }
                _cleanupSemaphore.Dispose();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Represents an entry in the Cache. It is ensured that each entry 
    /// </summary>
    public class CacheEntry<TValue>
    {
        private TValue _value;
        private int _expirationTime;
        private int _valueSet;

        public TValue Value
        {
            get
            {
                if (Volatile.Read(ref _valueSet) == 1)
                {
                    return _value;
                }
                throw new InvalidOperationException("Value is not set.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsExpired(int currentTime)
        {
            return currentTime >= _expirationTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(TValue value, int expirationTime)
        {
            _value = value;
            _expirationTime = expirationTime;
            Volatile.Write(ref _valueSet, 1);
        }
    }
}
