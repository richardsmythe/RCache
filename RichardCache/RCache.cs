using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RichardCache
{
    public class RCache<TKey, TValue> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, CacheEntry<TValue>> _cache = new();
        private readonly int _expirationMilliseconds;
        private readonly SemaphoreSlim _cleanupSemaphore = new (1, 1);

        public RCache() : this(100000) { }
        public RCache(int expirationMilliseconds)
        {
            _expirationMilliseconds = expirationMilliseconds;
            StartCleanupTask();
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        {
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
                var newValue = factory(key);
                var expirationTime = Environment.TickCount + _expirationMilliseconds;
                var newCacheEntry = new CacheEntry<TValue>(newValue, expirationTime);
                var existingCacheEntry = _cache.GetOrAdd(key, newCacheEntry);
                if (existingCacheEntry.Equals(newCacheEntry))
                {
       
                    return newValue;
                }
                if (!existingCacheEntry.IsExpired(Environment.TickCount))
                {
                    return existingCacheEntry.Value;
                }
                else
                {
                    _cache.TryRemove(key, out _);
                }
            }
        }

        private Task StartCleanupTask()
        {
            return Task.Run(async () =>
            {
                while (true)
                {
                    await _cleanupSemaphore.WaitAsync();
                    try
                    {
                        var currentTime = Environment.TickCount;
                        foreach (var (key, entry) in _cache)
                        {
                            if (entry.IsExpired(currentTime))
                            {
                                _cache.TryRemove(key, out _);
                            }
                        }
                    }
                    finally
                    {
                        _cleanupSemaphore.Release();
                    }
                    await Task.Delay(_expirationMilliseconds);
                }
            });
        }
    }

    public struct CacheEntry<TValue>
    {
        private readonly TValue _value;
        private readonly int _expirationTime;
        private int _valueSet;

        public CacheEntry(TValue value, int expirationTime)
        {
            _value = value;
            _expirationTime = expirationTime;
            _valueSet = 1;

        }

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

        public bool IsExpired(int currentTime) => currentTime >= _expirationTime;

        public override bool Equals(object objInstance)
        {
            if (objInstance is CacheEntry<TValue> other)
            {
                return EqualityComparer<TValue>.Default.Equals(_value, other._value) &&
                       _expirationTime == other._expirationTime &&
                       _valueSet == other._valueSet;
            }
            return false;
        }

        public override int GetHashCode() => HashCode.Combine(_value, _expirationTime, _valueSet);
    }
}
