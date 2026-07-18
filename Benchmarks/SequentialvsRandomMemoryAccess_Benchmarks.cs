using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    //| Method          | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
    //|---------------- |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
    //| SmallSequential |   1.671 ms |   0.9513 ms | 0.0521 ms |  0.13 |    0.00 |         - |          NA |
    //| LargeSequential |  12.856 ms |   0.9793 ms | 0.0537 ms |  1.00 |    0.01 |         - |          NA |
    //| SmallRandom     |  23.878 ms |  21.4545 ms | 1.1760 ms |  1.86 |    0.08 |         - |          NA |
    //| LargeRandom     | 324.534 ms | 159.6894 ms | 8.7531 ms | 25.24 |    0.60 |         - |          NA |

    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 1, iterationCount: 3)]
    [BenchmarkCategory("SequentialvsRandomMemoryAccess")]
    public class SequentialvsRandomMemoryAccess_Benchmarks
    {
        private const int SmallSize = 4 * 1024 * 1024;
        private const int LargeSize = 32 * 1024 * 1024;

        // Data arrays for sequential and random access
        private int[] smallData = [];
        private int[] largeData = [];

        // Random indices for random access
        private int[] smallRandomIndices = [];
        private int[] largeRandomIndices = [];

        [GlobalSetup]
        public void Setup()
        {
            var rng = new Random(42);

            // Small data (32MB - fits in L3)
            smallData = new int[SmallSize];
            for (var i = 0; i < SmallSize; i++)
                smallData[i] = rng.Next();

            // Large data (128MB - exceeds L3)
            largeData = new int[LargeSize];
            for (var i = 0; i < LargeSize; i++)
                largeData[i] = rng.Next();

            // Generate random indices
            smallRandomIndices = GenerateRandomIndices(SmallSize, rng);
            largeRandomIndices = GenerateRandomIndices(LargeSize, rng);
        }

        private static int[] GenerateRandomIndices(int size, Random rng)
        {
            var indices = new int[size];
            for (var i = 0; i < size; i++)
                indices[i] = i;

            // Fisher-Yates shuffle
            for (var i = size - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            return indices;
        }

        /// <summary>
        /// Sequential access, small data (32MB total fits in L3 cache)
        /// Data: 16MB, no indices needed for sequential
        /// Result: CPU cache hit + prefetcher = fastest
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("Sequential", "CacheHit")]
        public long SmallSequential()
        {
            long sum = 0;
            var data = smallData;

            for (var i = 0; i < data.Length; i++)
                sum += data[i];

            return sum;
        }

        /// <summary>
        /// Random access, small data (32MB total: 16MB data + 16MB indices = fits in L3)
        /// Result: CPU cache hit but no prefetching
        /// Should be ~3-5x slower than SmallSequential but still fast (L3 hit)
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("Random", "CacheHit")]
        public long SmallRandom()
        {
            long sum = 0;
            var data = smallData;
            var indices = smallRandomIndices;

            for (var i = 0; i < indices.Length; i++)
                sum += data[indices[i]];

            return sum;
        }

        /// <summary>
        /// Sequential access, large data (256MB total exceeds L3 cache)
        /// Data: 128MB
        /// Result: RAM access but prefetcher helps a lot
        /// Streaming read pattern is efficient even from RAM
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Sequential", "RAMAccess")]
        public long LargeSequential()
        {
            long sum = 0;
            var data = largeData;

            for (var i = 0; i < data.Length; i++)
                sum += data[i];

            return sum;
        }

        /// <summary>
        /// Random access, large data (256MB total: 128MB data + 128MB indices = 4x L3)
        /// Result: RAM cache miss + no prefetching = slowest
        /// Every access is a RAM round trip with ~100ns latency
        /// Expected: ~20-30x slower than LargeSequential
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("Random", "RAMAccess")]
        public long LargeRandom()
        {
            long sum = 0;
            var data = largeData;
            var indices = largeRandomIndices;

            for (var i = 0; i < indices.Length; i++)
                sum += data[indices[i]];

            return sum;
        }
    }
}
