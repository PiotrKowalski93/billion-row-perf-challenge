using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
        /*
 * ============================================================================
 * BENCHMARK RESULTS & ANALYSIS
 * ============================================================================
 *
 * | Method        | Line                    | Mean       | Ratio |
 * |-------------- |------------------------ |-----------:|------:|
 * | StringIndexOf | A;0.0                   |  0.3384 ns |  1.00 |
 * | SpanIndexOf   | A;0.0                   |  0.7801 ns |  2.31 |  ← no wait!
 * | PointerScan   | A;0.0                   |  0.5436 ns |  1.61 |
 * | SpanParse     | A;0.0                   | 18.1852 ns | 53.78 |
 * | PointerParse  | A;0.0                   | 18.3355 ns | 54.22 |
 * |               |                         |            |       |
 * | StringIndexOf | Ouagadougou;32.4        |  1.1006 ns |  1.00 |
 * | SpanIndexOf   | Ouagadougou;32.4        |  1.1844 ns |  1.08 |
 * | PointerScan   | Ouagadougou;32.4        |  2.8255 ns |  2.57 |  <- gap starts opening
 * | SpanParse     | Ouagadougou;32.4        | 26.9212 ns | 24.46 |
 * | PointerParse  | Ouagadougou;32.4        | 26.4674 ns | 24.05 |
 * |               |                         |            |       |
 * | StringIndexOf | Petropavlovsk...;-12.7  |  1.1006 ns |  1.00 |
 * | SpanIndexOf   | Petropavlovsk...;-12.7  |  1.1072 ns |  1.01 |
 * | PointerScan   | Petropavlovsk...;-12.7  |  6.0376 ns |  5.49 |  <- 5.5x slower!
 * | SpanParse     | Petropavlovsk...;-12.7  | 28.9211 ns | 26.28 |
 * | PointerParse  | Petropavlovsk...;-12.7  | 31.2659 ns | 28.41 |
 *
 * ============================================================================
 * WHY THESE RESULTS?
 * ============================================================================
 *
 * [1] "A;0.0" — StringIndexOf fastest, SpanIndexOf slowest
 * ─────────────────────────────────────────────────────────────
 *   Separator is at index 1; all methods finish in 1-2 iterations.
 *   At this scale (<1 ns) the algorithmic difference is near zero; JIT decisions are being measured:
 *
 *     string.IndexOf(';')      -> JIT recognizes it, aggressively inlines
 *     Line.AsSpan().IndexOf()  -> extra .AsSpan() method-call layer;
 *                                 at sub-nanosecond scale this overhead is visible
 *
 *   RULE: For results <1 ns we are measuring JIT inlining decisions, not algorithms.
 *
 * [2] Why does PointerScan slow down dramatically as the string grows?
 * ─────────────────────────────────────────────────────────────
 *   PointerScan is a scalar loop -> checks 1 character per step.
 *   StringIndexOf / SpanIndexOf  -> SpanHelpers.IndexOf -> SIMD (AVX2/SSE4.2):
 *
 *     PointerScan (scalar), "Petropavlovsk-Kamchatsky":
 *       'P'==';'? 'e'==';'? 't'==';'? ... -> 24 steps
 *
 *     StringIndexOf (AVX2, 256-bit - 16 chars/iteration):
 *       ['P','e','t','r','o','p','a','v','l','o','v','s','k','-','K','a'] -> No
 *       ['m','c','h','a','t','s','k','y',';',...] -> Yes  ->  2 steps
 *
 *   The farther away the separator is, the wider the scalar/SIMD step gap grows.
 *   A hand-written pointer loop cannot beat SIMD.
 *
 * [3] StringIndexOf ~ SpanIndexOf (for long strings)
 * ─────────────────────────────────────────────────────────────
 *   Both run the same SpanHelpers.IndexOf SIMD code underneath.
 *   string.IndexOf(char) already does AsSpan() + IndexOf internally.
 *   The difference only comes from JIT inlining decisions on short strings.
 *
 * [4] SpanParse ~ PointerParse
 * ─────────────────────────────────────────────────────────────
 *   Both ultimately call double.Parse(line[(sep+1)..]).
 *   Over 95% of the total time is the cost of double.Parse:
 *
 *     Separator search  ~  1 ns
 *     double.Parse      ~ 26 ns  <- dominant cost
 *     ──────────────────────────
 *     Total             ~ 27 ns
 *
 *   How fast we find the separator does not change the outcome.
 *   The bottleneck is parsing.
 *
 * ============================================================================
 * 1BRC TAKEAWAY
 * ============================================================================
 *   Writing a manual pointer loop instead of line.IndexOf(';') in ProcessLine
 *   SLOWS THINGS DOWN — the runtime already uses SIMD.
 *   Real optimization gains will come from optimizing double.Parse.
 * ============================================================================
 */

        // =============================================================================
        // Benchmark 1: String Character Iteration
        //
        // Comparison of 3 methods doing the same job:
        //   Indexer  -> s[i]                      : bounds check present (JIT may optimize away)
        //   Span     -> span[i]                   : bounds check present (JIT usually removes it)
        //   Pointer  -> unsafe char*, *p++        : no bounds check, direct memory access
        //
        // Expected order (fastest to slowest):
        //   Pointer ~ Span < Indexer
        //   (on short strings JIT may inline everything and the gap approaches zero)
        // =============================================================================
        [MemoryDiagnoser]
        [SimpleJob(warmupCount: 3, iterationCount: 5, launchCount: 1)]
        [BenchmarkCategory("Level04", "StringPointer", "Iteration")]
        public class StringIterationBenchmark
        {
            [Params(32, 1_024, 65_536)]
            public int Length { get; set; }

            private string text = string.Empty;

            [GlobalSetup]
            public void Setup()
            {
                var rng = new Random(42);
                var chars = new char[Length];

                for (var i = 0; i < Length; i++)
                    chars[i] = (char)('A' + rng.Next(26));

                text = new string(chars);
            }

            /// <summary>
            /// Classic indexer: range check on every access.
            /// JIT can eliminate the bounds check when the loop bound is constant.
            /// </summary>
            [Benchmark(Baseline = true)]
            public int Indexer()
            {
                var sum = 0;
                var s = text;

                for (var i = 0; i < s.Length; i++)
                    sum += s[i];

                return sum;
            }

            /// <summary>
            /// ReadOnlySpan: does not copy the string, points to the same memory.
            /// JIT aggressively removes bounds checks on Span-based loops.
            /// </summary>
            [Benchmark]
            public int Span()
            {
                var sum = 0;
                ReadOnlySpan<char> span = text.AsSpan();

                for (var i = 0; i < span.Length; i++)
                    sum += span[i];

                return sum;
            }

            /// <summary>
            /// Unsafe pointer: no bounds check, requires GC pin (fixed).
            /// p++ advances by sizeof(char)=2 bytes each step.
            /// </summary>
            [Benchmark]
            public unsafe int Pointer()
            {
                var sum = 0;

                fixed (char* ptr = text)
                {
                    var p = ptr;
                    var end = ptr + text.Length;

                    while (p < end)
                        sum += *p++;
                }

                return sum;
            }
        }

        // =============================================================================
        // Benchmark 2: Separator Search & Line Parse
        //
        // ProcessLine in 1BRC does two things:
        //   1) "StationName;12.3" -> find the ';' position
        //   2) Convert the temperature part to double
        //
        // 5 methods tested:
        //   StringIndexOf  -> string.IndexOf(';')         - managed, JIT optimized
        //   SpanIndexOf    -> span.IndexOf(';')            - can use SIMD (AVX2/SSE)
        //   PointerScan    -> unsafe char* scan            - hand-written loop
        //   SpanParse      -> find with Span + parse       - zero allocation
        //   PointerParse   -> find with Pointer + Span parse - maximum control
        //
        // Expected order (fastest to slowest):
        //   SpanIndexOf ~ SpanParse < StringIndexOf < PointerScan < PointerParse
        //   (Span.IndexOf can beat the pointer loop because it uses SIMD vectorization)
        // =============================================================================
        [MemoryDiagnoser]
        [SimpleJob(warmupCount: 3, iterationCount: 5, launchCount: 1)]
        [BenchmarkCategory("Level04", "StringPointer", "Search")]
        public class StringSearchBenchmark
        {
            // Realistic 1BRC line samples - station names of varying lengths
            [Params(
                "Ouagadougou;32.4",
                "Petropavlovsk-Kamchatsky;-12.7",
                "A;0.0"
            )]
            public string Line { get; set; } = string.Empty;

            /// <summary>
            /// string.IndexOf: managed, optimized by the CLR.
            /// </summary>
            [Benchmark(Baseline = true)]
            public int StringIndexOf() => Line.IndexOf(';');

            /// <summary>
            /// ReadOnlySpan.IndexOf: can use runtime SIMD (AVX2/SSE4.2).
            /// string.IndexOf already delegates to Span internally - the difference is usually minimal.
            /// </summary>
            [Benchmark]
            public int SpanIndexOf() => Line.AsSpan().IndexOf(';');

            /// <summary>
            /// Unsafe pointer scan: advances character by character, no SIMD.
            /// Acceptable on short strings; Span.IndexOf overtakes it on longer ones.
            /// </summary>
            [Benchmark]
            public unsafe int PointerScan()
            {
                fixed (char* ptr = Line)
                {
                    var p = ptr;
                    var end = ptr + Line.Length;

                    while (p < end)
                    {
                        if (*p == ';') return (int)(p - ptr);
                        p++;
                    }
                }

                return -1;
            }

            /// <summary>
            /// Full parse (Span): find separator + convert temperature to double.
            /// Zero allocation - exactly what Level3 ProcessLine does.
            /// </summary>
            [Benchmark]
            public double SpanParse()
            {
                ReadOnlySpan<char> line = Line.AsSpan();
                var sep = line.IndexOf(';');

                return double.Parse(line[(sep + 1)..]);
            }

            /// <summary>
            /// Full parse (Pointer): find separator with a pointer, switch to Span for parsing.
            /// double.Parse already accepts a Span, so no extra allocation.
            /// </summary>
            [Benchmark]
            public unsafe double PointerParse()
            {
                ReadOnlySpan<char> line = Line.AsSpan();
                int sep;

                fixed (char* ptr = line)
                {
                    var p = ptr;
                    var end = ptr + line.Length;
                    sep = -1;

                    while (p < end)
                    {
                        if (*p == ';') { sep = (int)(p - ptr); break; }
                        p++;
                    }
                }

                return sep < 0 ? 0 : double.Parse(line[(sep + 1)..]);
            }
        }
}
