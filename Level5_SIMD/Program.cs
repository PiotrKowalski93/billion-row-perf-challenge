using Shared;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.Intrinsics.X86;

var FilePath = GlobalConstants.FilePath_1B;

Console.WriteLine("=== Level 5: SIMD (AVX2) Implementation ===");
Console.WriteLine($"File: {GlobalConstants.FilePath}");
Console.WriteLine($"Processor Count: {Environment.ProcessorCount}");
Console.WriteLine($"AVX2 Supported: {Avx2.IsSupported}"); // 32 bytes
Console.WriteLine($"AVX-512 Supported: {Avx512F.IsSupported}"); // 64 bytes
Console.WriteLine();

if (!File.Exists(GlobalConstants.FilePath))
{
    Console.WriteLine($"ERROR: File not found at {GlobalConstants.FilePath}");
    return;
}

if (!Avx2.IsSupported)
{
    Console.WriteLine("WARNING: AVX2 not supported. Performance will be limited.");
}

GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();

var stopwatch = Stopwatch.StartNew();

var fileInfo = new FileInfo(FilePath);
var fileSize = fileInfo.Length;

if (fileSize < 0)
{
    Console.WriteLine("Error: File size is negative, which is unexpected.");
    return;
}

var threadCount = Environment.ProcessorCount;

var threadLocalResults = new HashTable[threadCount];
var lineCounters = new long[threadCount];

using var mmf = MemoryMappedFile.CreateFromFile(FilePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
using var accessor = mmf.CreateViewAccessor(0, fileSize, MemoryMappedFileAccess.Read);

unsafe
{
    byte* basePtr = null;
    accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref basePtr);

    //BOM CHCEK

    Parallel.For(0, threadCount, threadIndex =>
    {
        var chunkSize = fileSize / threadCount;

        var startOffset = threadIndex * chunkSize;
        var endOffset = (threadIndex == threadCount - 1) ? fileSize : startOffset + chunkSize;

        // Adjust startOffset to the next newline character to avoid splitting lines
        while (startOffset < fileSize && basePtr[startOffset] != '\n')
        {
            startOffset++;
        }

        // Adjust endOffset to the previous newline character to avoid splitting lines
        while (endOffset < fileSize && basePtr[endOffset] != '\n')
        {
            endOffset++;
        }

        var localHashTable = new HashTable(expectedCount: GlobalConstants.ExpectedStationCount, allowResize: true);
        long localLineCounter = 0;
        var position = startOffset;



        // SIMD





        threadLocalResults[threadIndex] = localHashTable;
        lineCounters[threadIndex] = localLineCounter;
    });
}