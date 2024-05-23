using System;
using System.Threading.Tasks;
using Xunit;
using RichardCache;

namespace Tests
{
    public class RCacheTests
    {
        [Fact]
        public async Task Test_Cache_AddAndGet()
        {
            ICache<string, int> cache = new RCache<string, int>();
            string key = "test_key";
            int expectedValue = 123;
            int retrievedValue = await cache.GetOrAdd(key, async (k) =>
            {
                return expectedValue;
            });
            Assert.Equal(expectedValue, retrievedValue);
        }

        [Fact]
        public async Task Test_Cache_Handle_Multiple_Threads_Access()
        {
            const int threads = 50;
            const int iterations = 100;
            var cache = new RCache<int, string>();
            var tasks = Enumerable.Range(0, threads)
                .Select(i => Task.Run(async () =>
                {
                    for (var j = 0; j < iterations; j++)
                    {
                        var key = i * iterations + j;
                        var value = await cache.GetOrAdd(key, async k =>
                        {
                            await Task.Delay(10);
                            return $"Value for key {k}";
                        });

                        Assert.Equal($"Value for key {key}", value);
                    }
                })).ToList();

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task Test_Cache_Maintain_Consistency_Under_Concurrent_Access()
        {
            ICache<int, string> cache = new RCache<int, string>();
            int threads = 50;
            int iterationsPerThread = 100;
            var tasks = Enumerable.Range(0, threads).Select(async threadId =>
            {
                for (int i = 0; i < iterationsPerThread; i++)
                {
                    string value = await cache.GetOrAdd(i, async key =>
                    {
                        await Task.Delay(10);
                        return $"Value for key {key} from thread {threadId}";
                    });
                    Assert.StartsWith($"Value for key {i}", value);
                }
            }).ToList();
            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task Test_Cache_Expiration()
        {
            ICache<string, int> cache = new RCache<string, int>(5, TimeSpan.FromSeconds(1));
            string key = "test_key";
            int expectedValue = 123;
            int retrievedValue = await cache.GetOrAdd(key, async (k) =>
            {
                return expectedValue;
            });
            Assert.Equal(expectedValue, retrievedValue);
            await Task.Delay(6000);
            int expiredValue = await cache.GetOrAdd(key, async (k) =>
            {
                return 456;
            });
            Assert.NotEqual(expectedValue, expiredValue);
        }

        [Fact]
        public async Task Test_Cache_LongRunningFactory()
        {
            ICache<string, int> cache = new RCache<string, int>();
            string key = "long_running";
            int value = 123;
            var longRunningFactoryTask = cache.GetOrAdd(key, async (k) =>
            {
                await Task.Delay(6000); 
                return value;
            });
            string anotherKey = "another_key";
            int anotherValue = 456;
            int retrievedAnotherValue = await cache.GetOrAdd(anotherKey, async (k) => anotherValue);
            Assert.Equal(anotherValue, retrievedAnotherValue);
            int retrievedValue = await longRunningFactoryTask;
            Assert.Equal(value, retrievedValue);
        }

        [Fact]
        public async Task Test_BackOffDelay()
        {
            ICache<int, string> cache = new RCache<int, string>();
            int key = 1;
            string value = "test_value";
            Func<int, Task<string>> longRunningFactoryTask = async (k) =>
            {
                await Task.Delay(6000);
                return value;
            };
            int concurrentTasks = 50;
            var tasks = Enumerable.Range(0, concurrentTasks)
                .Select(async _ =>
                {
                    string retrievedValue = await cache.GetOrAdd(key, longRunningFactoryTask);
                    Assert.Equal(value, retrievedValue);
                }).ToArray();
            await Task.WhenAll(tasks);
            string finalValue = await cache.GetOrAdd(key, longRunningFactoryTask);
            Assert.Equal(value, finalValue);
        }

    }
}
