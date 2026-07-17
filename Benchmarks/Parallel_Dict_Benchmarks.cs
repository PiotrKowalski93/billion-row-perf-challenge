using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [BenchmarkCategory("Concurrency")]
    public class Parallel_Dict_Benchmarks
    {
        [Params(10_000, 100_000, 1_000_000, 10_000_000)]
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

    }
}
