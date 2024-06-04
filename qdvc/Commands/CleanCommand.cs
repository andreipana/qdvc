using qdvc.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static qdvc.Infrastructure.IOContext;
using Console = qdvc.Infrastructure.SystemContext.Console;

namespace qdvc.Commands
{
    public class CleanCommand(bool force = false)
    {
        public bool Force { get; } = force;

        private CleanStatistics Statistics { get; } = new CleanStatistics();

        public async Task ExecuteAsync(IEnumerable<string> files)
        {
            processedFiles.Clear();

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = -1
            };

            var targetFiles = files.Select(GetTargetFile).Where(f => f != string.Empty);

            Statistics.Reset();

            await Parallel.ForEachAsync(targetFiles, options, async (file, _) =>
            {
                await CleanAsync(file);
            });

            Console.StdOutWriteLine(Statistics.ToString());
        }

        private readonly ConcurrentDictionary<string, int> processedFiles = new();


        private string GetTargetFile(string file)
        {
            var dvcFile = file.EndsWith(".dvc", StringComparison.OrdinalIgnoreCase) ? file : file + ".dvc";

            if (!processedFiles.TryAdd(dvcFile, 0))
                return string.Empty;

            if (FileSystem.File.Exists(dvcFile))
                return dvcFile;

            return string.Empty;
        }

        private async Task CleanAsync(string dvcFile)
        {
            var file = dvcFile.Substring(0, dvcFile.Length - 4);

            Statistics.IncrementTotalFiles();

            if (!FileSystem.File.Exists(file))
                return;

            var fileMd5 = await Hashing.ComputeMD5HashForFileAsync(file);
            var dvcMd5 = await DvcFileUtils.ReadHashFromDvcFile(dvcFile);

            if (fileMd5 != dvcMd5 && !Force)
            {
                Console.StdOutWriteLine($"Skip (modified): {file}");
                Statistics.IncrementModifiedFiles();
                return;
            }

            try
            {
                FileSystem.File.Delete(file);
                Console.StdOutWriteLine($"Removed: {file}");
                Statistics.IncrementRemovedFiles();
            }
            catch (Exception x)
            {
                Console.StdErrWriteLine($"Failed: {file}{Environment.NewLine}  {x.Message}");
                Statistics.IncrementFailedFiles();
            }
        }

        private class CleanStatistics
        {
            private volatile int _totalFiles;
            private volatile int _modifiedFiles;
            private volatile int _removedFiles;
            private volatile int _failedFiles;

            public int TotalFiles => _totalFiles;
            public int ModifiedFiles => _modifiedFiles;
            public int RemovedFiles => _removedFiles;
            public int FailedFiles => _failedFiles;

            public void IncrementTotalFiles() => Interlocked.Increment(ref _totalFiles);
            public void IncrementModifiedFiles() => Interlocked.Increment(ref _modifiedFiles);
            public void IncrementRemovedFiles() => Interlocked.Increment(ref _removedFiles);
            public void IncrementFailedFiles() => Interlocked.Increment(ref _failedFiles);

            internal void Reset()
            {
                Interlocked.Exchange(ref _totalFiles, 0);
                Interlocked.Exchange(ref _modifiedFiles, 0);
                Interlocked.Exchange(ref _removedFiles, 0);
                Interlocked.Exchange(ref _failedFiles, 0);
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append($"Total files: {TotalFiles}");

                if (RemovedFiles > 0)
                    sb.Append($", Removed: {RemovedFiles}");

                if (ModifiedFiles > 0)
                    sb.Append($", Skipped (modified): {ModifiedFiles}");

                if (FailedFiles > 0)
                    sb.Append($", Failed: {FailedFiles}");

                if (RemovedFiles == 0 && ModifiedFiles == 0 && FailedFiles == 0)
                    sb.Append(", all clean");

                return sb.ToString();
            }
        }
    }
}
