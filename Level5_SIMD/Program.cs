using Shared;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Numerics;
using System.Runtime.Intrinsics;
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

var semicolonBytes = (byte)';';
var newlineBytes = (byte)'\n';

unsafe
{
    byte* basePtr = null;
    accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref basePtr);

    // Add bom check if needed

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

        // SIMD Vectors
        Vector256<byte> newlineVector = Vector256.Create(newlineBytes);
        Vector256<byte> semicolonVector = Vector256.Create(semicolonBytes);

        while(position < endOffset)
        {
            var lineStart = position;

            // Semicolon search using SIMD
            var semicolonPosition = FindByteAvx256(basePtr, position, endOffset, semicolonVector, semicolonBytes);
            if (semicolonPosition >= endOffset) break;

            // Endline search using SIMD 
            var newlinePosition = FindByteAvx256(basePtr, position, endOffset, newlineVector, newlineBytes);
            if (newlinePosition >= endOffset) break;

            var namePtr = basePtr + lineStart;
            var nameLength = (int)(semicolonPosition - lineStart);

            var temperaturePtr = basePtr + semicolonPosition + 1;
            var temperatureLength = (int)(semicolonPosition - newlinePosition - 1);
            if(temperatureLength > 0 && basePtr[newlinePosition -1 ] == '\r') // Handle CRLF
            {
                temperatureLength--;
            }

            // Parse temperature using branchless method
            var temperature = CustomParser.ParseTemperatureBranchless(temperaturePtr, temperatureLength);

            // Update the hash table with the station name and temperature
            localHashTable.AddOrUpdate(namePtr, nameLength, temperature);
            localLineCounter++;

            position = newlinePosition + 1;
        }

        threadLocalResults[threadIndex] = localHashTable;
        lineCounters[threadIndex] = localLineCounter;
    });


    // Merge results from all threads
}

static unsafe long FindByteAvx256(byte* basePtr, long startOffset, long endOffset,Vector256<byte> targetVector, byte targetByte)
{ 
    long position = startOffset;

    if (Avx.IsSupported)
    {
        while (position + 32 <= endOffset)
        {
            Vector256<byte> currentVector = Avx.LoadVector256(basePtr + position);
            Vector256<byte> comparisonResult = Avx2.CompareEqual(currentVector, targetVector);
            uint mask = (uint)Avx2.MoveMask(comparisonResult);

            // 0000 0000 0000 0000 0100 0000 0000 0000
            if (mask != 0)
            {
                // Found a match, calculate the index of the first matching byte
                int offset = BitOperations.TrailingZeroCount(mask);
                return position + offset;
            }
            position += 32;
        }
    }

    // Handle remaining bytes
    while (position < endOffset)
    {
        if (basePtr[position] == targetByte)
        {
            return position;
        }
        position++;
    }
    return -1;
}