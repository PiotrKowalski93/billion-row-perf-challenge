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

        var chunkSize = fileSize / threadCount;

        Parallel.For(0, threadCount, threadIndex =>
        {

        });
    }
    finally
    {
        // Isn't disposing the accessor enough?
        accessor.SafeMemoryMappedViewHandle.ReleasePointer();
    }

    
}