using Shared;
using System.Diagnostics;

Console.WriteLine("====== Level 3: Parallel Implementation ======");
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