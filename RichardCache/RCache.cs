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
        Task<TValue> GetOrAdd(TKey key, Func<TKey, Task<TValue>> factory);
        int Count { get; }
        IEnumerable<TKey> Keys { get; }
    }

    public class RCache<TKey, TValue> : ICache<TKey, TValue> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, CacheEntry> _cache = new ConcurrentDictionary<TKey, CacheEntry>();
        private readonly int _expirationMilliseconds;
        private readonly SemaphoreSlim _cleanupSemaphore = new SemaphoreSlim(1);
        private bool _disposed = false;

        public RCache() : this(60, TimeSpan.FromMinutes(2))
        {
        }

        public RCache(int expirationSeconds, TimeSpan cleanupInterval)
        {
            _expirationMilliseconds = expirationSeconds * 1000;
            StartCleanupWorker(cleanupInterval);
        }

        public int Count => _cache.Count;
        public IEnumerable<TKey> Keys => _cache.Keys;

        public async Task<TValue> GetOrAdd(TKey key, Func<TKey, Task<TValue>> factory)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RCache<TKey, TValue>), "The cache has been disposed already.");
            
            int attempt = 0;
            while (true)
            {
                if (_cache.TryGetValue(key, out var cacheEntry))
                {
                    return (TValue)await cacheEntry.ValueTask;
                }

                var newCacheEntry = new CacheEntry();
                var existingCacheEntry = _cache.GetOrAdd(key, newCacheEntry);

                if (existingCacheEntry == newCacheEntry)
                {
                    try
                    {
                        var value = await factory(key);
                        var expirationTime = Environment.TickCount + _expirationMilliseconds;
                        newCacheEntry.SetValue(value, expirationTime);
                        return value;
                    }
                    catch (Exception e)
                    {
                        _cache.TryRemove(key, out _);
                        newCacheEntry.SetException(e);
                    }
                }
                else
                {
                    await Task.Delay(BackOffDelay(attempt));
                    attempt++;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TimeSpan BackOffDelay(int attempt) => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt));

        private void StartCleanupWorker(TimeSpan cleanupInterval)
        {
            Task.Run(async () =>
            {
                while (!_disposed)
                {
                    try
                    {
                        await Task.Delay(cleanupInterval);
                        await RemoveExpiredEntries();
                    }
                    catch (Exception)
                    {
                        _disposed = true;
                    }
                }
            });
        }

        private async Task RemoveExpiredEntries()
        {
            await _cleanupSemaphore.WaitAsync();
            try
            {
                if (_disposed) return;
                var currentTime = Environment.TickCount;
                foreach (var item in _cache.ToArray())
                {
                    if (item.Value.IsExpired(currentTime))
                    {
                        _cache.TryRemove(item.Key, out _);
                    }
                }
            }
            finally
            {
                _cleanupSemaphore.Release();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _disposed = true;
                    _cleanupSemaphore.Wait();
                    foreach (var entry in _cache)
                    {
                        if (entry.Value.ValueTask.Status == TaskStatus.RanToCompletion)
                        {
                            ((IDisposable)entry.Value.ValueTask).Dispose();
                        }
                    }
                    _cache.Clear();
                    _cleanupSemaphore.Release();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class CacheEntry
    {
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _expirationTime;

        public Task<object> ValueTask => _tcs.Task;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsExpired(int currentTime) => currentTime >= _expirationTime;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(object value, int expirationTime)
        {
            _tcs.TrySetResult(value);
            _expirationTime = expirationTime;
        }

        public void SetException(Exception exception)
        {
            _tcs.TrySetException(exception);
        }
    }
}
