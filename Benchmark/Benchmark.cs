using RichardCache;
using System.Runtime.Caching;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Jitbit.Utils;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [ShortRunJob]
    public class Benchmark
    {
        [Params(1000,100)]
        public int CacheSize { get; set; }
        private RCache<string, int> _richardCache;
        private MemoryCache _memoryCache;
        private FastCache<string, int> _fastCache;
        private const int ExpirationMilliseconds = 100000;

        [GlobalSetup]
        public void Setup()
        {
            _richardCache = new RCache<string, int>(ExpirationMilliseconds);
            _memoryCache = new MemoryCache("MemoryCacheTest");
            _fastCache = new FastCache<string, int>();

            // Fill the caches for lookup tests
            for (int i = 0; i < CacheSize; i++)
            {
                string key = $"key_{i}";
                _richardCache.GetOrAdd(key, _ => i);
                _memoryCache.Add(key, i, DateTimeOffset.MaxValue);
                _fastCache.GetOrAdd(key, i, TimeSpan.FromMilliseconds(ExpirationMilliseconds));
            }
        }

        [Benchmark]
        public void RichardCache_GetOrAdd()
        {
            for (int i = 0; i < CacheSize; i++)
            {
                string key = $"key_{i}";
                int value = _richardCache.GetOrAdd(key, _ => i);
            }
        }

        [Benchmark]
        public void RichardCache_LookupExistingKeys()
        {
            for (int i = 0; i < CacheSize; i++)
            {
                int value = _richardCache.GetOrAdd($"key_{i}", _ => i);
            }
        }

        [Benchmark]
        public void RichardCache_LookupNonExistingKeys()
        {
            int value = _richardCache.GetOrAdd($"key_{CacheSize + 1}", _ => CacheSize + 1);
        }

        //[Benchmark]
        //public void MemoryCache_LookupExistingKeys()
        //{
        //    for (int i = 0; i < CacheSize; i++)
        //    {
        //        var value = _memoryCache.Get($"key_{i}");
        //    }
        //}

        //[Benchmark]
        //public void MemoryCache_LookupNonExistingKeys()
        //{
        //    var value = _memoryCache.Get($"key_{CacheSize + 1}");
        //}

        //[Benchmark]
        //public void MemoryCache_Add()
        //{
        //    for (int i = 0; i < CacheSize; i++)
        //    {
        //        _memoryCache.Add($"key_{i}", CacheSize + 1, DateTimeOffset.MaxValue);
        //    }
        //}

        [Benchmark]
        public void FastCache_GetOrAdd()
        {
            for (int i = 0; i < CacheSize; i++)
            {
                _fastCache.GetOrAdd($"key_{i}", i, TimeSpan.FromMilliseconds(ExpirationMilliseconds));
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
