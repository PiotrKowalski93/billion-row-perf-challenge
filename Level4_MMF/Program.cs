using Shared;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;

var FilePath = GlobalConstants.FilePath_10M;

Console.WriteLine("====== Level 4: Memory-Mapped Files ======");
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

var fileInfo = new FileInfo(FilePath);
var fileSize = fileInfo.Length;

if(fileSize < 0)
{
    Console.WriteLine("Error: File size is negative, which is unexpected.");
    return;
}

var threadCount = Environment.ProcessorCount;
var threadLocalDics = new Dictionary< int, (string Name, StationStatsStruct Stats)>[threadCount];
var lineCounters = new int[threadCount];

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
            while (startOffset > fileSize && basePtr[startOffset - 1] != '\n')
            {
                startOffset++;
            }

            // Adjust endOffset to the previous newline character to avoid splitting lines
            while (endOffset < fileSize && basePtr[endOffset - 1] != '\n')
            {
                endOffset++;
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

                localLineCounter++;

                // Extract station name and temperature
                // Calculate hash code for station name
            }
        });
    }
    finally
    {
        // Isn't disposing the accessor enough?
        accessor.SafeMemoryMappedViewHandle.ReleasePointer();
    }

    
}