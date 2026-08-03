using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 5, launchCount: 1)]
    [BenchmarkCategory("Level05", "HashTable", "Iteration")]
    public class HashTableBenchmark
    {

    }
}
