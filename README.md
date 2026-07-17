# billion-row-perf-challenge

https://github.com/gunnarmorling/1brc

The One Billion Row Challenge (1BRC) is a fun exploration of how far modern Java(C# in my case) can be pushed for aggregating one billion rows from a text file. Grab all your (virtual) threads, reach out to SIMD, optimize your GC, or pull any other trick, and create the fastest implementation for solving this task!

# **Amdahl's Law**

Amdahl's Law describes the theoretical maximum performance improvement that can be achieved by optimizing or parallelizing only part of a program.

The formula is:

**Speedup = 1 / ((1 - P) + P / N)**

Where:

* **P** = fraction of the program that can be optimized or parallelized
* **N** = improvement factor (or number of parallel workers/threads)

### Example

If 80% of a program can be parallelized (**P = 0.8**) and you run it on 8 threads:

**Speedup = 1 / (0.2 + 0.8 / 8) = 3.33x**

Even with an infinite number of threads, the maximum speedup would be:

**1 / (1 - 0.8) = 5x**

because the remaining 20% is still sequential.

### Impact on Optimization

Amdahl's Law teaches that:

* Optimizing code that consumes only a small percentage of execution time has little overall impact.
* The biggest gains come from improving the parts of the system where most time is spent.
* Profiling should always be performed before optimization to identify real bottlenecks.
* As performance improves, the remaining bottlenecks become increasingly important.

**Key takeaway:** Focus optimization efforts on the hottest paths of the application. A 2x improvement in code that accounts for 50% of runtime is far more valuable than a 10x improvement in code that accounts for only 1%.

### ConcurrentDict vs Lock vs Split-Merge


| Method                  | MeasurementCount | Mean         | Error       | StdDev       | Gen0        | Gen1       | Gen2    | Allocated     |
|------------------------ |----------------- |-------------:|------------:|-------------:|------------:|-----------:|--------:|--------------:|
| LockedDictionary        | 100_000          |   5,318.0 us |   103.52 us |    101.67 us |           - |          - |       - |      26.19 KB |
| ConcurrentDictionary    | 100_000          |   5,518.4 us |   111.56 us |    323.67 us |   3601.5625 |   500.0000 |       - |   21909.63 KB |
| PartitionedDictionaries | 100_000          |     639.5 us |    12.78 us |     35.62 us |     50.7813 |     2.9297 |       - |     290.22 KB |


| LockedDictionary        | 1_000_000        |  52,380.6 us |   993.81 us |  1,020.57 us |           - |          - |       - |      26.21 KB |
| ConcurrentDictionary    | 1_000_000        |  44,165.5 us | 1,013.69 us |  2,988.88 us |  36000.0000 |  4583.3333 | 83.3333 |  218788.98 KB |
| PartitionedDictionaries | 1_000_000        |   4,662.3 us |   143.05 us |    421.77 us |     46.8750 |    15.6250 |       - |     290.38 KB |

| LockedDictionary        | 10_000_000       | 511,318.1 us | 9,630.47 us |  9,458.41 us |           - |          - |       - |      26.63 KB |
| ConcurrentDictionary    | 10_000_000       | 437,915.4 us | 8,289.89 us | 20,797.72 us | 359000.0000 | 44000.0000 |       - | 2187535.13 KB |
| PartitionedDictionaries | 10_000_000       |  39,017.0 us | 1,023.28 us |  3,017.18 us |           - |          - |       - |     290.52 KB |

PartitionedDictionaries is way faster than other methods