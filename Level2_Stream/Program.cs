using Shared;
using System.Diagnostics;
using System.Text;

Console.WriteLine("====== Level 2: Stream Implementation ======");
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

using var reader = new StreamReader(GlobalConstants.FilePath_10M, Encoding.UTF8, true, 3076);

string line;
int lineCounter = 0;

var stationStatsDict = new Dictionary<string, StationStats>(capacity: GlobalConstants.ExpectedStationCount);

while ((line = reader.ReadLine()) != null)
{
    var separatorIndex = line.IndexOf(';');
    var stationName = line[..separatorIndex];

    var temperature = double.Parse(line.AsSpan(separatorIndex + 1));

    if (!stationStatsDict.TryGetValue(stationName, out var stats))
    {
        stats = new StationStats();
        stationStatsDict.Add(stationName, stats);
    }

    stats.Update(temperature);
    lineCounter++;
}

stopwatch.Stop();

var output = ResultLogger.FormatOutput(stationStatsDict.OrderBy(kvp => kvp.Key));

Console.WriteLine();
Console.WriteLine($"Result: {output}");
Console.WriteLine();
Console.WriteLine($"Processed {lineCounter} rows");
Console.WriteLine($"Found {stationStatsDict.Count()} unique stations");
Console.WriteLine($"Execution Time: {stopwatch.Elapsed} ms");

ResultLogger.SaveResult("Stream", output, stopwatch.Elapsed, lineCounter, stationStatsDict.Count());