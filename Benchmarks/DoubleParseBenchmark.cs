using BenchmarkDotNet.Attributes;
using Shared;
using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace Benchmarks
{
//| Method      | RawValue | Mean      | Error     | StdDev    | Allocated |
//|------------ |--------- |----------:|----------:|----------:|----------:|
//| Utf8Parse   | -12.7    | 24.407 ns | 0.1673 ns | 0.0434 ns |         - |
//| SpanParse   | -12.7    | 39.483 ns | 1.2778 ns | 0.1977 ns |         - |
//| CustomParse | -12.7    |  4.171 ns | 0.1527 ns | 0.0397 ns |         - |
//| Utf8Parse   | -9.5     | 22.806 ns | 2.4706 ns | 0.6416 ns |         - |
//| SpanParse   | -9.5     | 36.009 ns | 1.6764 ns | 0.4354 ns |         - |
//| CustomParse | -9.5     |  4.041 ns | 0.1031 ns | 0.0268 ns |         - |
//| Utf8Parse   | 32.4     | 23.946 ns | 0.9110 ns | 0.2366 ns |         - |
//| SpanParse   | 32.4     | 37.836 ns | 1.0289 ns | 0.1592 ns |         - |
//| CustomParse | 32.4     |  3.804 ns | 0.2227 ns | 0.0578 ns |         - |
//| Utf8Parse   | 9.1      | 22.409 ns | 0.6806 ns | 0.1768 ns |         - |
//| SpanParse   | 9.1      | 36.103 ns | 1.2624 ns | 0.3278 ns |         - |
//| CustomParse | 9.1      |  3.709 ns | 0.2294 ns | 0.0596 ns |         - |


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
    }
}
