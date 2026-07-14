using Shared;
using System.Diagnostics;
using System.Text;

Console.WriteLine("====== Level 1: Naive Implementation ======");
Console.WriteLine($"File: {GlobalConstants.FilePath_10M}");
Console.WriteLine();

// Verify if the file exists before attempting to read it
if (!File.Exists(GlobalConstants.FilePath_10M))
{
    Console.WriteLine($"File not found: {GlobalConstants.FilePath_10M}");
}

// Force GC before measurement
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();

var stopwatch = Stopwatch.StartNew();

var lines = File.ReadLines(GlobalConstants.FilePath_10M, Encoding.UTF8);
var result = lines.Select(line =>
{
    var splitext = line.Split(";");

    var stationName = splitext[0];
    var temperature = double.Parse(splitext[1]);

    return new
    {
        Station = stationName,
        Temperature = temperature
    };
})
.GroupBy(x => x.Station)
.Select(g => new
{
    Station = g.Key,
    Min = g.Min(x => x.Temperature),
    Max = g.Max(x => x.Temperature),
    Avg = g.Average(x => x.Temperature),
})
.OrderBy(x => x.Station)
.ToList();

stopwatch.Stop();

var output = "{" + string.Join(", ", result.Select(r => $"{r.Station}={r.Min:F1}/{r.Avg:F1}/{r.Max:F1}")) + "}";

Console.WriteLine();
Console.WriteLine($"Result: {output}");
Console.WriteLine();
Console.WriteLine($"Processed {lines.Count()} rows");
Console.WriteLine($"Found {result.Count()} unique stations");
Console.WriteLine($"Execution Time: {stopwatch.Elapsed} ms");

ResultLogger.SaveResult("Naive", output, stopwatch.Elapsed, lines.Count(), result.Count());