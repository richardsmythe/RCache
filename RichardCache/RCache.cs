using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RichardCache
{
    public class RCache<TKey, TValue> : IDisposable where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, CacheEntry<TValue>> _cache = new ConcurrentDictionary<TKey, CacheEntry<TValue>>();
        private readonly int _expirationMilliseconds;
        private bool _disposed = false;

        public RCache() : this(100000) { }
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
                    if (!cacheEntry.IsExpired(Environment.TickCount))
                    {
                        return cacheEntry.Value;
                    }
                    else
                    {
                        _cache.TryRemove(key, out _);
                    }
                }

                var newCacheEntry = new CacheEntry<TValue>();
                var existingCacheEntry = _cache.GetOrAdd(key, newCacheEntry);

                if (existingCacheEntry.Equals(newCacheEntry))
                {
                    var value = factory(key);
                    var expirationTime = Environment.TickCount + _expirationMilliseconds;
                    newCacheEntry.SetValue(value, expirationTime);                 
                    _cache[key] = newCacheEntry;
                    return value;
                }
                Thread.Sleep(BackOffDelay(attempt));
                attempt++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TimeSpan BackOffDelay(int attempt)
        {
            int baseDelayMs = 10;
            int maxDelayMs = 100;
            int delay = baseDelayMs * (attempt + 1);
            delay = Math.Min(delay, maxDelayMs);
            return TimeSpan.FromMilliseconds(delay);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
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
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Represents an entry in the Cache.
    /// </summary>
    public struct CacheEntry<TValue>
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object objInstanceA)
        {
            if (objInstanceA is CacheEntry<TValue> objInstanceB)
            {
                return EqualityComparer<TValue>.Default.Equals(_value, objInstanceB._value) &&
                       _expirationTime == objInstanceB._expirationTime &&
                       _valueSet == objInstanceB._valueSet;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return HashCode.Combine(_value, _expirationTime, _valueSet);
        }
    }
}
