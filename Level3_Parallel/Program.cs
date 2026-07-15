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

}