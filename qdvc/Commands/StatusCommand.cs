using qdvc.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static qdvc.Infrastructure.IOContext;
using Console = qdvc.Infrastructure.SystemContext.Console;

namespace qdvc.Commands
{
    public class StatusCommand(DvcCache? dvcCache)
    {
        public DvcCache? DvcCache { get; } = dvcCache;

        public StatusStatistics Statistics { get; } = new StatusStatistics();

        public async Task ExecuteAsync(IEnumerable<string> files)
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = -1
            };

            var targetFiles = files.Where(IsTargetFile);

            Statistics.Reset();

            await Parallel.ForEachAsync(targetFiles, options, async (file, _) =>
            {
                await OutputFileStatusAsync(file);
            });

            Console.StdOutWriteLine(Statistics.ToString());
        }

        private bool IsTargetFile(string file)
        {
            if (file.EndsWith(".dvc", StringComparison.OrdinalIgnoreCase))
            {
                var trackedFileName = file.Substring(0, file.Length - 4);
                if (FileSystem.File.Exists(trackedFileName))
                    return false;
                return true;
            }
            else return true;
        }

        private async Task OutputFileStatusAsync(string file)
        {
            if (!FileSystem.File.Exists(file))
            {
                Console.StdErrWriteLine($"File {file} does not exist.");
                return;
            }

            Statistics.IncrementTotalFiles();

            if (file.EndsWith(".dvc", StringComparison.OrdinalIgnoreCase))
            {
                await OutputDvcFileStatusAsync(file);
            }
            else
            {
                await OutputTrackedFileStatusAsync(file);
            }
        }

        private Task OutputDvcFileStatusAsync(string file)
        {
            string trackedFileName = file.Substring(0, file.Length - 4);
            Console.StdOutWriteLine($"Missing: {trackedFileName}");
            Statistics.IncrementMissingFiles();
            return Task.CompletedTask;
        }

        private async Task OutputTrackedFileStatusAsync(string file)
        {
            var dvcFilePath = $"{file}.dvc";

            if (!FileSystem.File.Exists(dvcFilePath))
            {
                //Console.StdOutWriteLine($"Untracked: {file}");
                Statistics.IncrementUntrackedFiles();
                return;
            }

            var md5 = await Hashing.ComputeMD5HashForFileAsync(file);
            var md5InDvc = await DvcFileUtils.ReadHashFromDvcFile(dvcFilePath);

            if (md5 != md5InDvc)
            {
                Console.StdOutWriteLine($"Modified: {file}");
                Statistics.IncrementModifiedFiles();
                return;
            }

            var cacheFilePath = DvcCache?.GetCacheFilePath(md5);
            if (cacheFilePath == null || !FileSystem.File.Exists(cacheFilePath))
            {
                Console.StdOutWriteLine($"Not in cache: {file}");
                Statistics.IncrementNotInCacheFiles();
                return;
            }

            Statistics.IncrementUpToDateFiles();
        }
    }

    public class StatusStatistics
    {
        private volatile int _totalFiles;
        private volatile int _untrackedFiles;
        private volatile int _modifiedFiles;
        private volatile int _notInCacheFiles;
        private volatile int _missingFiles;
        private volatile int _upToDateFiles;

        public int TotalFiles => _totalFiles;
        public int UntrackedFiles => _untrackedFiles;
        public int ModifiedFiles => _modifiedFiles;
        public int NotInCacheFiles => _notInCacheFiles;
        public int MissingFiles => _missingFiles;
        public int UpToDateFiles => _upToDateFiles;
        
        public void IncrementTotalFiles() => Interlocked.Increment(ref _totalFiles);

        public void IncrementUntrackedFiles() => Interlocked.Increment(ref _untrackedFiles);

        public void IncrementModifiedFiles() => Interlocked.Increment(ref _modifiedFiles);

        public void IncrementNotInCacheFiles() => Interlocked.Increment(ref _notInCacheFiles);

        public void IncrementMissingFiles() => Interlocked.Increment(ref _missingFiles);

        public void IncrementUpToDateFiles() => Interlocked.Increment(ref _upToDateFiles);

        public void Reset()
        {
            _totalFiles = 0;
            _untrackedFiles = 0;
            _modifiedFiles = 0;
            _notInCacheFiles = 0;
            _missingFiles = 0;
            _upToDateFiles = 0;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"Total files: {TotalFiles}");
            bool everythingUpToDate = true;

            if (UpToDateFiles > 0 && UpToDateFiles < TotalFiles)
            {
                sb.Append($", Up to date: {UpToDateFiles}");

                everythingUpToDate = false;
            }

            if (UntrackedFiles > 0)
            {
                sb.Append($", Untracked: {UntrackedFiles}");
                everythingUpToDate = false;
            }

            if (ModifiedFiles > 0)
            {
                sb.Append($", Modified: {ModifiedFiles}");
                everythingUpToDate = false;
            }

            if (NotInCacheFiles > 0)
            {
                sb.Append($", Not in cache: {NotInCacheFiles}");
                everythingUpToDate = false;
            }

            if (MissingFiles > 0)
            {
                sb.Append($", Missing: {MissingFiles}");
                everythingUpToDate = false;
            }

            if (everythingUpToDate)
                sb.AppendLine(", Everything is up to date");

            return sb.ToString();
        }
    }
}