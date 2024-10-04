using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RichardCache
{
    public class RCache<TKey, TValue> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, CacheEntry<TValue>> _cache = new ConcurrentDictionary<TKey, CacheEntry<TValue>>();
        private readonly int _expirationMilliseconds;
        private readonly Task _cleanupTask;
        private readonly SemaphoreSlim _cleanupSemaphore = new SemaphoreSlim(1, 1);

        public RCache() : this(100000) { }
        public RCache(int expirationMilliseconds)
        {
            _expirationMilliseconds = expirationMilliseconds;
            _cleanupTask = StartCleanupTask();
        }

        public int Count => _cache.Count;
        public IEnumerable<TKey> Keys => _cache.Keys;

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
                        TriggerCleanup();
                    }
                }

                var newCacheEntry = new CacheEntry<TValue>();
                var existingCacheEntry = _cache.GetOrAdd(key, newCacheEntry);

                if (existingCacheEntry.Equals(newCacheEntry))
                {
                    var value = factory(key);
                    var expirationTime = Environment.TickCount + _expirationMilliseconds;
                    _cache[key] = new CacheEntry<TValue>(value, expirationTime);
                    TriggerCleanup();
                    return value;
                }
            }
        }

        private async Task StartCleanupTask()
        {

            await _cleanupSemaphore.WaitAsync();
            var currentTime = Environment.TickCount;
            foreach (var (key, entry) in _cache)
            {
                if (entry.IsExpired(currentTime))
                {
                    _cache.TryRemove(key, out _);
                }
            }
            _cleanupSemaphore.Release();
            await Task.Delay(_expirationMilliseconds);

        }

        private void TriggerCleanup()
        {
            if (_cleanupSemaphore.CurrentCount == 1)
            {
                _cleanupSemaphore.Wait();
                _cleanupSemaphore.Release();
            }
        }

        public void Dispose()
        {
            _cleanupTask.Wait();

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
