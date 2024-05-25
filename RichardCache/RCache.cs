using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RichardCache
{
    public interface ICache<TKey, TValue> : IDisposable
    {
        TValue GetOrAdd(TKey key, Func<TKey, TValue> factory);
        int Count { get; }
        IEnumerable<TKey> Keys { get; }
    }

    public class RCache<TKey, TValue> : ICache<TKey, TValue> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, CacheEntry> _cache = new ConcurrentDictionary<TKey, CacheEntry>();
        private readonly int _expirationMilliseconds;
        private readonly SemaphoreSlim _cleanupSemaphore = new SemaphoreSlim(1);
        private bool _disposed = false;

        public RCache() : this(60) { }
        public RCache(int expirationSeconds)
            => _expirationMilliseconds = expirationSeconds * 1000;

        public int Count => _cache.Count;
        public IEnumerable<TKey> Keys => _cache.Keys;

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RCache<TKey, TValue>), "The cache has been disposed already.");

            int attempt = 0;
            while (true)
            {
                // Check if the key exists in the cache
                if (_cache.TryGetValue(key, out var cacheEntry))
                {
                    if (cacheEntry.IsExpired(Environment.TickCount))
                    {
                        _cache.TryRemove(key, out _);
                    }
                    else
                    {
                        return (TValue)cacheEntry.Value;
                    }
                }
                else
                {
                    var newCacheEntry = new CacheEntry();
                    var existingCacheEntry = _cache.GetOrAdd(key, newCacheEntry);
                    if (existingCacheEntry == newCacheEntry)
                    {
                        try
                        {
                            var value = factory(key);
                            var expirationTime = Environment.TickCount + _expirationMilliseconds;
                            newCacheEntry.SetValue(value, expirationTime);
                            return value;
                        }
                        catch (Exception e)
                        {
                            _cache.TryRemove(key, out _);
                            newCacheEntry.SetException(e);
                            throw;
                        }
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
        private TimeSpan BackOffDelay(int attempt) => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt));

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
                        if (entry.TaskStatus == TaskStatus.RanToCompletion && entry.Value is IDisposable disposable)
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
    /// Represents an entry in the cache, encapsulates a value along with its expiration time.
    /// The completion of cache operations within a TaskCompletionSource helps
    /// the interface remains consistent regardless of whether the actual operation is asynchronous. 
    /// This can simplify usage, as they can await the completion of cache operations uniformly, 
    /// whether they are synchronous or asynchronous. Plus, future revisions may use async.
    /// </summary>
    public class CacheEntry
    {
        private object _value;
        private int _expirationTime;
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        public object Value
        {
            get
            {
                _tcs.Task.Wait();
                return _value ?? throw new InvalidOperationException("Value is not set.");
            }
        }
        public TaskStatus TaskStatus => _tcs.Task.Status;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsExpired(int currentTime) => currentTime >= _expirationTime;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(object value, int expirationTime)
        {
            _value = value;
            _expirationTime = expirationTime;
            _tcs.TrySetResult(value);
        }

        public void SetException(Exception exception)
        {
            _tcs.TrySetException(exception);
        }
    }
}
