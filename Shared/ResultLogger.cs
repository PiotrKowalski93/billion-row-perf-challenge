using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    public static class ResultLogger
    {
        private const string ResultFileName = "results.log";

        /// <summary>
        /// Appends the 1BRC result to a log file in the solution directory.
        /// </summary>
        /// <param name="projectName">Name of the project (e.g., "Level01_Naive")</param>
        /// <param name="output">The 1BRC formatted output string</param>
        /// <param name="elapsed">Time elapsed for processing</param>
        /// <param name="rowCount">Number of rows processed</param>
        /// <param name="stationCount">Number of unique stations found</param>
        public static void SaveResult(
            string projectName,
            string output,
            TimeSpan elapsed,
            long rowCount,
            int stationCount)
        {
            try
            {
                var solutionDirectory = GlobalConstants.FilesDirectory;
                var filePath = Path.Combine(solutionDirectory, ResultFileName);

                // Collect memory statistics
                var workingSetMB = Environment.WorkingSet / 1024 / 1024;
                var gcMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
                var gen0Collections = GC.CollectionCount(0);
                var gen1Collections = GC.CollectionCount(1);
                var gen2Collections = GC.CollectionCount(2);

                // Calculate throughput
                var throughputMBps = rowCount > 0 ? (rowCount * 25.0 / 1024 / 1024) / elapsed.TotalSeconds : 0; // Assuming ~25 bytes per row
                var rowsPerSecond = rowCount / elapsed.TotalSeconds;

                var logEntry = $"""
                ================================================================================
                [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {projectName}
                ================================================================================
                Performance:
                  Rows:               {rowCount:N0}
                  Stations:           {stationCount}
                  Elapsed:            {elapsed}
                  Throughput:         {rowsPerSecond:N0} rows/sec ({throughputMBps:F2} MB/sec)
                
                Memory:
                  Working Set:        {workingSetMB:N0} MB
                  GC Memory:          {gcMemoryMB:N0} MB
                  Gen0 Collections:   {gen0Collections}
                  Gen1 Collections:   {gen1Collections}
                  Gen2 Collections:   {gen2Collections}
                
                Processor:
                  CPU Cores:          {Environment.ProcessorCount}
                --------------------------------------------------------------------------------
                {output}



                """;

                File.AppendAllText(filePath, logEntry, System.Text.Encoding.UTF8);
                Console.WriteLine($"\n📁 Results saved: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n⚠️ Results could not be saved: {ex.Message}");
            }
        }

        public static string FormatOutput<T>(IEnumerable<KeyValuePair<string, T>> stats)
            where T : StationStats
        {
            return "{" + string.Join(", ", stats.Select(x => $"{x.Key}={x.Value}")) + "}";
        }

    }
}
