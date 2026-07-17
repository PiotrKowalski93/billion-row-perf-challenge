using Level3_Parallel;
using Shared;
using System.Diagnostics;
using System.Text;

var FilePath = GlobalConstants.FilePath_10M;

Console.WriteLine("====== Level 3: Parallel Implementation ======");
Console.WriteLine($"File: {FilePath}");
Console.WriteLine();

// Verify if the file exists before attempting to read it
if (!File.Exists(FilePath))
{
    Console.WriteLine($"File not found: {FilePath}");
}

// Force GC before measurement
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();

var stopwatch = Stopwatch.StartNew();

var threadCount = Environment.ProcessorCount * 2;
var chunks = ComputeChunks(FilePath, threadCount);

Console.WriteLine($"Thread Count: {threadCount}");
Console.WriteLine();

var threadLocalDics = new Dictionary<string, StationStats>[threadCount];
var lineCounters = new int[threadCount];

Parallel.For(0, threadCount, threadIndex =>
{
    var localDic = new Dictionary<string, StationStats>(capacity: GlobalConstants.ExpectedStationCount / threadCount);
    var localLineCounter = 0;

    var startByte = chunks[threadIndex];
    var endByte = chunks[threadIndex + 1];
    var chunkSize = (int)(endByte - startByte);

    var buffer = new byte[chunkSize]; // huge allocation
    using (var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
    {
        stream.Seek(startByte, SeekOrigin.Begin);
        stream.ReadExactly(buffer, 0, chunkSize);
    }

    // huge allocation again
    var text = Encoding.UTF8.GetString(buffer);
    var lineStart = 0;

    for(int i = 0; i < text.Length; i++)
    {
        if (text[i] == '\n')
        {
            var lineEnd = i;

            ProcessLine(text.AsSpan(lineStart, lineEnd - lineStart), localDic);
            localLineCounter++;
            
            lineStart = i + 1;
        }
    }

    threadLocalDics[threadIndex] = localDic;
    lineCounters[threadIndex] = localLineCounter;
});

var finalDict = new Dictionary<string, StationStats>(capacity: 413);
foreach (var localDict in threadLocalDics)
{
    foreach (var kvp in localDict)
    {
        if (!finalDict.TryGetValue(kvp.Key, out var stats))
        {
            stats = new StationStats();
            finalDict[kvp.Key] = stats;
        }

        stats.Merge(kvp.Value);
    }
}

var totalLines = lineCounters.Sum();

stopwatch.Stop();

var output = ResultLogger.FormatOutput(finalDict.OrderBy(kvp => kvp.Key));

Console.WriteLine();
Console.WriteLine($"Result: {output}");
Console.WriteLine();
Console.WriteLine($"Processed {totalLines} rows");
Console.WriteLine($"Found {finalDict.Count()} unique stations");
Console.WriteLine($"Execution Time: {stopwatch.Elapsed} ms");

ResultLogger.SaveResult("Multithread", output, stopwatch.Elapsed, totalLines, finalDict.Count());

static void ProcessLine(ReadOnlySpan<char> line, Dictionary<string, StationStats> localDic)
{
    var separationIndex = line.IndexOf(";");
    if(separationIndex < 0) return;

    var stationName = line[..separationIndex].ToString(); // new allocation
    var temperature = double.Parse(line[(separationIndex + 1)..]);

    if (!localDic.TryGetValue(stationName, out var stats))
    {
        stats = new StationStats();
        localDic[stationName] = stats;
    }

    stats.Update(temperature);
}

static long[] ComputeChunks(string filePath, int threadCount)
{
    var fileInfo = new FileInfo(filePath);
    var fileSize = fileInfo.Length;

    var chunks = new long[threadCount + 1];

    // We do not have BOM in this file, so we can safely divide the file into equal parts

    chunks[0] = 0;
    chunks[threadCount] = fileSize;
    var chunkSize = fileSize / threadCount;

    using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

    for(int i = 1; i < threadCount; i++)
    {
        // End of boundary for each thread
        var targetPosition = i * chunkSize;

        fileStream.Seek(targetPosition, SeekOrigin.Begin);

        int b;
        while((b = fileStream.ReadByte()) != -1)
        {
            // New line character
            if (b == '\n') 
            {
                chunks[i] = fileStream.Position;
                break;
            }
        }

        if(b == -1)
        {
            chunks[i] = fileSize;
        }
    }

    return chunks;
}