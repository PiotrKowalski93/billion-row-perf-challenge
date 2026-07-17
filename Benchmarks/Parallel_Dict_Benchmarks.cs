using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [BenchmarkCategory("Concurrency")]
    public class Parallel_Dict_Benchmarks
    {
        [Params(100_000, 1_000_000, 10_000_000)]
        public int MeasurementCount { get; set; }

        private List<(string StationName, double Temperature)> measurements = [];
        private readonly object _lockObj = new();

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(0197);

            var stations = new string[413];
            for (int i = 0; i < stations.Length; i++)
            {
                stations[i] = $"Station_{i:D3}";
            }

            for (int i = 0; i < MeasurementCount; i++)
            {
                var station = stations[random.Next(stations.Length)];
                var temperature = random.NextDouble() * 40 - 10;
                measurements.Add((station, temperature));
            }
        }

        [Benchmark]
        [BenchmarkCategory("Locked Dictionary")]
        public int LockedDictionary()
        {
            var sharedDict = new Dictionary<string, (double, double, double, int)>(capacity: 413);
            var threadCount = Environment.ProcessorCount;
            var chunkSize = MeasurementCount / threadCount;

            Parallel.For(0, threadCount, i =>
            {
                var start = i * chunkSize;
                var end = (i == threadCount - 1) ? MeasurementCount : (i + 1) * chunkSize;

                for(int j = start; j < end; j++)
                {
                    var (station, temperature) = measurements[j];
                    lock (_lockObj)
                    {
                        if (!sharedDict.TryGetValue(station, out var stats))
                        {
                            stats = (temperature, temperature, temperature, 1);
                        }
                        else
                        {
                            var (min, max, sum, count) = stats;
                            min = Math.Min(min, temperature);
                            max = Math.Max(max, temperature);
                            sum += temperature;
                            count++;
                            stats = (min, max, sum, count);
                        }
                        sharedDict[station] = stats;
                    }
                }
            });

            return sharedDict.Count;
        }

        [Benchmark]
        [BenchmarkCategory("Concurrent Dictionary")]
        public int ConcurrentDictionary()
        {
            var sharedDict = new System.Collections.Concurrent.ConcurrentDictionary<string, (double, double, double, int)>(concurrencyLevel: Environment.ProcessorCount, capacity: 413);
            var threadCount = Environment.ProcessorCount;
            var chunkSize = MeasurementCount / threadCount;
            
            Parallel.For(0, threadCount, i =>
            {
                var start = i * chunkSize;
                var end = (i == threadCount - 1) ? MeasurementCount : (i + 1) * chunkSize;

                for (int j = start; j < end; j++)
                {
                    var (station, temperature) = measurements[j];

                    sharedDict.AddOrUpdate(station,
                        key => (temperature, temperature, temperature, 1),
                        (key, stats) =>
                        {
                            var (min, max, sum, count) = stats;
                            min = Math.Min(min, temperature);
                            max = Math.Max(max, temperature);
                            sum += temperature;
                            count++;
                            return (min, max, sum, count);
                        });
                }
            });
            return sharedDict.Count;
        }

        [Benchmark]
        [BenchmarkCategory("Partitioned Dictionary")]
        public int PartitionedDictionaries()
        {
            var threadCount = Environment.ProcessorCount;
            var chunkSize = MeasurementCount / threadCount;
            var localDicts = new Dictionary<string, (double, double, double, int)>[threadCount];

            Parallel.For(0, threadCount, i =>
            {
                var localDict = new Dictionary<string, (double, double, double, int)>(capacity: 413);
                var start = i * chunkSize;
                var end = (i == threadCount - 1) ? MeasurementCount : (i + 1) * chunkSize;

                for (int j = start; j < end; j++)
                {
                    var (station, temperature) = measurements[j];
                    if (!localDict.TryGetValue(station, out var stats))
                    {
                        stats = (temperature, temperature, temperature, 1);
                    }
                    else
                    {
                        var (min, max, sum, count) = stats;
                        min = Math.Min(min, temperature);
                        max = Math.Max(max, temperature);
                        sum += temperature;
                        count++;
                        stats = (min, max, sum, count);
                    }
                    localDict[station] = stats;
                }
                localDicts[i] = localDict;
            });

            // Merge local dictionaries
            var finalDict = new Dictionary<string, (double, double, double, int)>(capacity: 413);
            foreach (var localDict in localDicts)
            {
                foreach (var kvp in localDict)
                {
                    if (!finalDict.TryGetValue(kvp.Key, out var stats))
                    {
                        finalDict[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        var (min1, max1, sum1, count1) = stats;
                        var (min2, max2, sum2, count2) = kvp.Value;
                        min1 = Math.Min(min1, min2);
                        max1 = Math.Max(max1, max2);
                        sum1 += sum2;
                        count1 += count2;
                        finalDict[kvp.Key] = (min1, max1, sum1, count1);
                    }
                }
            }
            return finalDict.Count;
        }

    }
}
