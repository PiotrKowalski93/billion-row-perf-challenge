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
