using BenchmarkDotNet.Attributes;
using Jitbit.Utils;
using RichardCache;
using System.Runtime.Caching;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob]
    public class Benchmark
    {
        [Params(1000)]
        public int CacheSize { get; set; }
        private ICache<string, int> _richardCache;
        private MemoryCache _memoryCache;
        private FastCache<string, int> _fastCache;
        private const int ExpirationSeconds = 60;

        [GlobalSetup]
        public void Setup()
        {
            _richardCache = new RCache<string, int>();
            _memoryCache = new MemoryCache("MemoryCacheTest");
            _fastCache = new FastCache<string, int>();

            // fill the caches for lookup tests
            for (int i = 0; i < CacheSize; i++)
            {
                string key = $"key_{i}";
                _richardCache.GetOrAdd(key, _ => Task.FromResult(i)).GetAwaiter().GetResult();
                _memoryCache.Add(key, i, DateTimeOffset.MaxValue);
                _fastCache.GetOrAdd(key, i, TimeSpan.FromSeconds(ExpirationSeconds));
            }
        }

        [Benchmark]
        public async Task RichardCache_GetOrAdd()
        {
            for (int i = 0; i < CacheSize; i++)
            {
                string key = $"key_{i}";
                int value = await _richardCache.GetOrAdd(key, key => Task.FromResult(i));
            }
        }

        [Benchmark]
        public async Task RichardCache_LookupExistingKeys()
        {
            for (int i = 0; i < CacheSize; i++)
            {
                int value = await _richardCache.GetOrAdd($"key_{i}", key => Task.FromResult(i));
            }
        }

        [Benchmark]
        public async Task RichardCache_LookupNonExistingKeys()
        {
            int value = await _richardCache.GetOrAdd($"key_{CacheSize + 1}", key => Task.FromResult(CacheSize + 1));
        }



        [Benchmark]
        public void MemoryCache_LookupExistingKeys()
        {
            for (int i = 0; i < CacheSize; i++)
            {
                var value = _memoryCache.Get($"key_{i}");
            }
        }

        [Benchmark]
        public void MemoryCache_LookupNonExistingKeys()
        {
            var value = _memoryCache.Get($"key_{CacheSize + 1}");
        }

        [Benchmark]
        public void MemoryCache_Add()
        {
            for (int i = 0; i < CacheSize; i++)
            {
                _memoryCache.Add($"key_{i}", CacheSize + 1, DateTimeOffset.MaxValue);
            }
        }

        [Benchmark]
        public void FastCache_GetOrAdd()
        {
            for (int i = 0; i < CacheSize; i++)
            {
                _fastCache.GetOrAdd($"key_{i}", i, TimeSpan.FromSeconds(ExpirationSeconds));
            }
        }

        [Benchmark]
        public void FastCache_LookupExistingKeys()
        {
            for (int i = 0; i < CacheSize; i++)
            {
                _fastCache.TryGet($"key_{i}", out _);
            }
        }

        [Benchmark]
        public void FastCache_LookupNonExistingKeys()
        {
            _fastCache.TryGet($"key_{CacheSize + 1}", out _);
        }
    }
}
