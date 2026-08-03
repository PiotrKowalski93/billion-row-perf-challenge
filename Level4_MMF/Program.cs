using Shared;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.IO.Hashing;
using System.Text;

var FilePath = GlobalConstants.FilePath_1B;

Console.WriteLine("====== Level 4: Memory-Mapped Files ======");
Console.WriteLine($"File: {FilePath}");
Console.WriteLine($"Processor Count: {Environment.ProcessorCount}");
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

var fileInfo = new FileInfo(FilePath);
var fileSize = fileInfo.Length;

if(fileSize < 0)
{
    Console.WriteLine("Error: File size is negative, which is unexpected.");
    return;
}

var threadCount = Environment.ProcessorCount;
var threadLocalDics = new Dictionary<int, (string Name, StationStatsStruct Stats)>[threadCount];
var lineCounters = new long[threadCount];

using var mmf = MemoryMappedFile.CreateFromFile(FilePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);

using var accessor = mmf.CreateViewAccessor(0, fileSize, MemoryMappedFileAccess.Read);

unsafe
{
    try
    {
        byte* basePtr = null;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref basePtr);

        // I ommited calculating BOM
        var chunkSize = fileSize / threadCount;

        Parallel.For(0, threadCount, threadIndex =>
        {
            // Calculating chunk sizes
            var startOffset = threadIndex * chunkSize;
            var endOffset = (threadIndex == threadCount - 1) ? fileSize : startOffset + chunkSize;

            // Adjust startOffset to the next newline character to avoid splitting lines
            if (startOffset > 0)
            {
                while (startOffset < fileSize && basePtr[startOffset - 1] != '\n')
                {
                    startOffset++;
                }
            }

            // Adjust endOffset to the previous newline character to avoid splitting lines
            if (endOffset < fileSize && threadIndex < threadCount - 1)
            {
                while (endOffset < fileSize && basePtr[endOffset - 1] != '\n')
                {
                    endOffset++;
                }
            }

            var localStats = new Dictionary<int, (string Name, StationStatsStruct Stats)>();
            long localLineCounter = 0;
            var position = startOffset;

            // Each thread processes its chunk of the file
            while (position < endOffset)
            {
                // We need to find semicolon position
                var semicolonPos = position;

                while (semicolonPos < endOffset && basePtr[semicolonPos] != ';')
                {
                    semicolonPos++;
                }

                if (semicolonPos >= endOffset)
                {
                    break; // No more semicolons in this chunk
                }

                // Find new line position
                var newLinePos = semicolonPos + 1;
                while (newLinePos < endOffset && basePtr[newLinePos] != '\n')
                {
                    newLinePos++;
                }

                if(newLinePos >= endOffset && threadIndex != threadCount - 1)
                {
                    break; // No more new lines in this chunk
                }

                var nameSpan = new ReadOnlySpan<byte>(basePtr + position, (int)(semicolonPos - position));
                var hash = (int)XxHash3.HashToUInt64(nameSpan);

                var tempLength = (int)(newLinePos - semicolonPos - 1);
                var tempSpan = new ReadOnlySpan<byte>(basePtr + semicolonPos + 1, tempLength);
                var tempValue = CustomParser.CustomParse(tempSpan);
                
                if(localStats.TryGetValue(hash, out var existingStats))
                {
                    // Update existing stats
                    existingStats.Stats.Update(tempValue);
                }
                else
                {
                    // Add new stats
                    var name = Encoding.UTF8.GetString(nameSpan);
                    var stationStats = new StationStatsStruct();
                    stationStats.Update(tempValue);

                    localStats[hash] = (name, stationStats);
                }

                localLineCounter++;
                position = newLinePos + 1; // Move to the next line
            }

            threadLocalDics[threadIndex] = localStats;
            lineCounters[threadIndex] = localLineCounter;
        });
    }
    finally
    {
        // Isn't disposing the accessor enough?
        accessor.SafeMemoryMappedViewHandle.ReleasePointer();
    }

    // Merge results from all threads
    var finalDict = new Dictionary<string, StationStatsStruct>(capacity: 413);
    foreach (var localDict in threadLocalDics)
    {
        foreach (var (_, (name, stats)) in localDict)
        {
            if (!finalDict.TryGetValue(name, out var existingStats))
            {
                existingStats = new StationStatsStruct();
                finalDict[name] = existingStats;
            }
            else
            {
                finalDict[name] = stats;
            }
            
        }
    }
    
    stopwatch.Stop();

    var totalLines = lineCounters.Sum();
    var output = ResultLogger.FormatOutputStruct(finalDict.OrderBy(kvp => kvp.Key));

    Console.WriteLine();
    Console.WriteLine($"Result: {output}");
    Console.WriteLine();
    Console.WriteLine($"Processed {totalLines} rows");
    Console.WriteLine($"Found {finalDict.Count()} unique stations");
    Console.WriteLine($"Execution Time: {stopwatch.Elapsed} ms");

    ResultLogger.SaveResult("Multithread", output, stopwatch.Elapsed, totalLines, finalDict.Count());
}