using BenchmarkDotNet.Attributes;
using System.Text;
using System.IO.Hashing;

namespace Benchmarks
{
    //| Method                      | MeasurementCount | Mean        | Error     | StdDev    | Ratio | Gen0      | Gen1    | Allocated  | Alloc Ratio |
    //|---------------------------- |----------------- |------------:|----------:|----------:|------:|----------:|--------:|-----------:|------------:|
    //| DotNetStringHash_Dictionary | 100000           |  4,436.3 us | 153.72 us |  39.92 us |  1.00 |  773.4375 | 23.4375 |  4865936 B |       1.000 |
    //| SimpleHash_Dictionary       | 100000           |  2,086.4 us |  54.81 us |  14.24 us |  0.47 |         - |       - |    15984 B |       0.003 |
    //| FNV1aHash_Dictionary        | 100000           |  2,287.0 us |  48.28 us |   7.47 us |  0.52 |         - |       - |    15984 B |       0.003 |
    //| xxHash_Dictionary           | 100000           |  1,602.3 us |  15.79 us |   4.10 us |  0.36 |    1.9531 |       - |    15984 B |       0.003 |
    //| PureStringHashCode          | 100000           |  2,928.0 us |  72.22 us |  11.18 us |  0.66 |  769.5313 |       - |  4849952 B |       0.997 |
    //| PureSimpleHash              | 100000           |    990.3 us |   5.72 us |   0.89 us |  0.22 |         - |       - |          - |       0.000 |
    //| PureFNV1aHash               | 100000           |    943.9 us |   7.97 us |   2.07 us |  0.21 |         - |       - |          - |       0.000 |
    //| PureXxHashCode              | 100000           |    293.5 us |   5.03 us |   1.31 us |  0.07 |         - |       - |          - |       0.000 |
    //|                             |                  |             |           |           |       |           |         |            |             |
    //| DotNetStringHash_Dictionary | 1000000          | 43,421.8 us | 390.27 us |  60.39 us |  1.00 | 7666.6667 |       - | 48518354 B |       1.000 |
    //| SimpleHash_Dictionary       | 1000000          | 20,926.0 us | 249.77 us |  64.87 us |  0.48 |         - |       - |    15984 B |       0.000 |
    //| FNV1aHash_Dictionary        | 1000000          | 22,547.0 us | 277.90 us |  72.17 us |  0.52 |         - |       - |    15984 B |       0.000 |
    //| xxHash_Dictionary           | 1000000          | 15,955.6 us | 172.51 us |  26.70 us |  0.37 |         - |       - |    15984 B |       0.000 |
    //| PureStringHashCode          | 1000000          | 30,313.5 us | 995.49 us | 258.53 us |  0.70 | 7718.7500 |       - | 48502368 B |       1.000 |
    //| PureSimpleHash              | 1000000          | 10,057.0 us | 208.64 us |  54.18 us |  0.23 |         - |       - |          - |       0.000 |
    //| PureFNV1aHash               | 1000000          |  9,490.2 us |  75.47 us |  19.60 us |  0.22 |         - |       - |          - |       0.000 |
    //| PureXxHashCode              | 1000000          |  2,958.4 us |  48.96 us |  12.72 us |  0.07 |         - |       - |          - |       0.000 |



    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 1, iterationCount: 5)]
    [BenchmarkCategory("Level04", "Hashing")]
    public class HashAlgorithmBenchmark
    {
        private byte[][] stationBytes = [];
        private int[] measurementIndices = []; // Simulates repeated stations

        [Params(100_000, 1_000_000)]
        public int MeasurementCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            const int stationCount = 413; // Real 1BRC station count

            // Generate 413 realistic station names as byte arrays
            stationBytes = new byte[stationCount][];

            var random = new Random(42);
            var prefixes = new[] { "New", "San", "Port", "Mount", "Saint", "North", "South", "East", "West" };
            var suffixes = new[] { "ville", "town", "city", "burg", "field", "wood", "dale", "mont", "berg" };

            for (var i = 0; i < stationCount; i++)
            {
                string name;
                if (i < 50)
                {
                    name = i switch
                    {
                        0 => "Hamburg",
                        1 => "Bulawayo",
                        2 => "Palembang",
                        3 => "St. John's",
                        4 => "Cracow",
                        5 => "Bridgetown",
                        6 => "Istanbul",
                        7 => "Roseau",
                        8 => "Conakry",
                        9 => "Bangkok",
                        10 => "Aarhus",
                        11 => "Tashkent",
                        12 => "Amsterdam",
                        13 => "Copenhagen",
                        14 => "Helsinki",
                        _ => $"{prefixes[i % prefixes.Length]}{suffixes[i % suffixes.Length]}_{i}"
                    };
                }
                else
                {
                    name = $"{prefixes[random.Next(prefixes.Length)]}{suffixes[random.Next(suffixes.Length)]}_{i}";
                }

                stationBytes[i] = Encoding.UTF8.GetBytes(name);
            }

            // Simulate 1BRC: Same stations repeated many times
            measurementIndices = new int[MeasurementCount];
            for (var i = 0; i < MeasurementCount; i++)
            {
                measurementIndices[i] = random.Next(stationCount);
            }
        }

        /// <summary>
        /// Baseline: Using String.GetHashCode() as hash algorithm
        /// Convert byte[] to string, then hash the string
        /// Good distribution but requires string allocation
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("HashAlgorithm")]
        public int DotNetStringHash_Dictionary()
        {
            var dict = new Dictionary<int, int>();

            foreach (var idx in measurementIndices)
            {
                var bytes = stationBytes[idx];
                // Simulate: Convert bytes to string, then hash
                var hash = Encoding.UTF8.GetString(bytes).GetHashCode();

                if (dict.TryGetValue(hash, out var count))
                    dict[hash] = count + 1;
                else
                    dict[hash] = 1;
            }

            return dict.Count;
        }

        /// <summary>
        /// Simple multiplicative hash: hash = hash * 31 + byte
        /// Very fast but poor distribution = more collisions
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("HashAlgorithm")]
        public int SimpleHash_Dictionary()
        {
            var dict = new Dictionary<int, int>();

            foreach (var idx in measurementIndices)
            {
                var bytes = stationBytes[idx];
                var hash = ComputeSimpleHash(bytes);

                if (dict.TryGetValue(hash, out var count))
                    dict[hash] = count + 1;
                else
                    dict[hash] = 1;
            }

            return dict.Count;
        }

        /// <summary>
        /// FNV-1a hash: Excellent distribution for byte sequences
        /// Used in Level 4 implementation
        /// Best balance of speed and quality
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("HashAlgorithm")]
        public int FNV1aHash_Dictionary()
        {
            var dict = new Dictionary<int, int>();

            foreach (var idx in measurementIndices)
            {
                var bytes = stationBytes[idx];
                var hash = ComputeFNV1aHash(bytes);

                if (dict.TryGetValue(hash, out var count))
                    dict[hash] = count + 1;
                else
                    dict[hash] = 1;
            }

            return dict.Count;
        }

        [Benchmark]
        [BenchmarkCategory("HashAlgorithm")]
        public int xxHash_Dictionary()
        {
            var dict = new Dictionary<ulong, int>();

            foreach (var idx in measurementIndices)
            {
                var bytes = stationBytes[idx];
                var hash = XxHash3.HashToUInt64(bytes);

                if (dict.TryGetValue(hash, out var count))
                    dict[hash] = count + 1;
                else
                    dict[hash] = 1;
            }

            return dict.Count;
        }

        [Benchmark]
        [BenchmarkCategory("PureHash")]
        public long PureXxHashCode()
        {
            long sum = 0;
            foreach (var idx in measurementIndices)
            {
                var bytes = stationBytes[idx];
                var hash = (int)XxHash3.HashToUInt64(bytes);
                sum += hash;
            }
            return sum;
        }

        /// <summary>
        /// Just hash computation overhead (no dictionary)
        /// Shows pure hash algorithm performance
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("PureHash")]
        public long PureStringHashCode()
        {
            long sum = 0;
            foreach (var idx in measurementIndices)
            {
                var bytes = stationBytes[idx];
                var hash = Encoding.UTF8.GetString(bytes).GetHashCode();
                sum += hash;
            }
            return sum;
        }

        [Benchmark]
        [BenchmarkCategory("PureHash")]
        public long PureSimpleHash()
        {
            long sum = 0;
            foreach (var idx in measurementIndices)
            {
                var bytes = stationBytes[idx];
                var hash = ComputeSimpleHash(bytes);
                sum += hash;
            }
            return sum;
        }

        [Benchmark]
        [BenchmarkCategory("PureHash")]
        public long PureFNV1aHash()
        {
            long sum = 0;
            foreach (var idx in measurementIndices)
            {
                var bytes = stationBytes[idx];
                var hash = ComputeFNV1aHash(bytes);
                sum += hash;
            }
            return sum;
        }

        // Hash Implementations
        private static int ComputeSimpleHash(ReadOnlySpan<byte> bytes)
        {
            unchecked
            {
                var hash = 0;
                foreach (var b in bytes)
                {
                    hash = hash * 31 + b;
                }
                return hash;
            }
        }

        private static int ComputeFNV1aHash(ReadOnlySpan<byte> bytes)
        {
            unchecked
            {
                var hash = (int)2166136261;
                foreach (var b in bytes)
                {
                    hash ^= b;
                    hash *= 16777619;
                }
                return hash;
            }
        }
    }
}
