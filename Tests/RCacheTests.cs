using RichardCache;

namespace Tests
{
    public class RCacheTests
    {
        [Fact]
        public void Test_Cache_AddAndGet()
        {
            var cache = new RCache<string, int>();
            string key = "test_key";
            int expectedValue = 123;
            int retrievedValue = cache.GetOrAdd(key, k => expectedValue);
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
                        var value = await Task.Run(() => cache.GetOrAdd(key, k => $"Value for key {k}"));
                        Assert.Equal($"Value for key {key}", value);
                    }
                })).ToList();

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task Test_Cache_Maintain_Consistency_Under_Concurrent_Access()
        {
            var cache = new RCache<int, string>();
            int threads = 50;
            int iterationsPerThread = 100;
            var tasks = Enumerable.Range(0, threads).Select(async threadId =>
            {
                for (int i = 0; i < iterationsPerThread; i++)
                {
                    string value = await Task.Run(() => cache.GetOrAdd(i, key => $"Value for key {key} from thread {threadId}"));
                    Assert.StartsWith($"Value for key {i}", value);
                }
            }).ToList();
            await Task.WhenAll(tasks);
        }

        [Fact]
        public void Test_Cache_Expiration()
        {
            var cache = new RCache<string, int>(5);
            string key = "test_key";
            int expectedValue = 123;
            int retrievedValue = cache.GetOrAdd(key, k => expectedValue);
            Assert.Equal(expectedValue, retrievedValue);
            Thread.Sleep(6000);
            int expiredValue = cache.GetOrAdd(key, k => 456);
            Assert.NotEqual(expectedValue, expiredValue);
        }

        [Fact]
        public void Test_Cache_LongRunningFactory()
        {
            var cache = new RCache<string, int>();
            string key = "long_running";
            int value = 123;
            var longRunningFactoryTask = cache.GetOrAdd(key, k =>
            {
                Thread.Sleep(6000);
                return value;
            });
            string anotherKey = "another_key";
            int anotherValue = 456;
            int retrievedAnotherValue = cache.GetOrAdd(anotherKey, k => anotherValue);
            Assert.Equal(anotherValue, retrievedAnotherValue);
            int retrievedValue = longRunningFactoryTask;
            Assert.Equal(value, retrievedValue);
        }

    }
}
