using BenchmarkDotNet.Attributes;
using Shared;
using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace Benchmarks
{
    //| Method             | RawValue | Mean       | Error     | StdDev    | Allocated |
    //|------------------- |--------- |-----------:|----------:|----------:|----------:|
    //| Utf8Parse          | -12.7    | 24.9845 ns | 0.8523 ns | 0.1319 ns |         - |
    //| SpanParse          | -12.7    | 39.7836 ns | 1.4299 ns | 0.2213 ns |         - |
    //| CustomParse        | -12.7    |  4.9599 ns | 0.1937 ns | 0.0503 ns |         - |
    //| IntParseBranchless | -12.7    |  0.8118 ns | 0.0532 ns | 0.0138 ns |         - |
    //
    //| Utf8Parse          | -9.5     | 22.5162 ns | 0.5687 ns | 0.1477 ns |         - |
    //| SpanParse          | -9.5     | 36.7148 ns | 1.2272 ns | 0.3187 ns |         - |
    //| CustomParse        | -9.5     |  4.0938 ns | 0.2021 ns | 0.0525 ns |         - |
    //| IntParseBranchless | -9.5     |  0.5673 ns | 0.0939 ns | 0.0145 ns |         - |
    //
    //| Utf8Parse          | 32.4     | 24.2564 ns | 0.8086 ns | 0.1251 ns |         - |
    //| SpanParse          | 32.4     | 38.4922 ns | 1.2387 ns | 0.3217 ns |         - |
    //| CustomParse        | 32.4     |  3.8348 ns | 0.0844 ns | 0.0219 ns |         - |
    //| IntParseBranchless | 32.4     |  0.7137 ns | 0.3437 ns | 0.0532 ns |         - |
    //
    //| Utf8Parse          | 9.1      | 24.0969 ns | 1.6948 ns | 0.4401 ns |         - |
    //| SpanParse          | 9.1      | 37.8323 ns | 5.3531 ns | 0.8284 ns |         - |
    //| CustomParse        | 9.1      |  4.0343 ns | 0.4558 ns | 0.1184 ns |         - |
    //| IntParseBranchless | 9.1      |  0.3809 ns | 0.0851 ns | 0.0221 ns |         - |

    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 5, launchCount: 1)]
    [BenchmarkCategory("Level04", "Double Parse")]
    public class DoubleParseBenchmark
    {
        [Params("9.1", "32.4", "-9.5", "-12.7")]
        public string RawValue { get; set; }

        private byte[] _bytes = [];

        [GlobalSetup]
        public void Setup() => _bytes = Encoding.UTF8.GetBytes(RawValue);

        [Benchmark]
        public double Utf8Parse()
        {
            Utf8Parser.TryParse(_bytes, out double value, out _);
            return value;
        }

        [Benchmark]
        public double SpanParse()
        {
            Span<char> chars = stackalloc char[_bytes.Length];
            var count = Encoding.UTF8.GetChars(_bytes, chars);
            return double.Parse(chars[..count], CultureInfo.InvariantCulture);
        }

        [Benchmark]
        public double CustomParse() => CustomParser.CustomParse(_bytes);

        [Benchmark]
        public unsafe int IntParseBranchless()
        {
            fixed (byte* ptr = _bytes)
                return ParseTemperatureInt(ptr, _bytes.Length);
        }

        public unsafe int ParseTemperatureInt(byte* ptr, int len)
        {
            var sign = 1;

            if (ptr[0] == '-')
            {
                sign = -1;
                ptr++;
                len--;
            }

            int value;

            if (len == 3)
            {
                // "D.D" -> D*10 + D   (e.g. "9.1" -> 91)
                value = (ptr[0] - '0') * 10
                      + (ptr[2] - '0');
            }
            else
            {
                // 32.5 => 325
                // "DD.D" -> D*100 + D*10 + D   (e.g. "32.4" -> 324)
                value = (ptr[0] - '0') * 100
                      + (ptr[1] - '0') * 10
                      + (ptr[3] - '0') * 1;
            }

            return sign * value;
        }
    }
}
