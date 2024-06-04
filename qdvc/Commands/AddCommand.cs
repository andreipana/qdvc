using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using qdvc.Utilities;
using static qdvc.Infrastructure.IOContext;
using Console = qdvc.Infrastructure.SystemContext.Console;

namespace qdvc.Commands
{
    public class AddCommand
    {
        public AddCommand(DvcCache? dvcCache)
        {
            DvcCache = dvcCache;
        }

        public DvcCache? DvcCache { get; }

        private AddStatistics Statistics { get; } = new AddStatistics();

        public async Task ExecuteAsync(IEnumerable<string> files)
        {
            Statistics.Reset();

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = -1
            };

            var filesWithoutDvcFiles = files.Where(f => !f.EndsWith(".dvc", StringComparison.OrdinalIgnoreCase));

            await Parallel.ForEachAsync(filesWithoutDvcFiles, options, async (file, _) =>
            {
                await AddFileAsync(file);
            });

            Console.StdOutWriteLine(Statistics.ToString());
        }

        private async Task AddFileAsync(string file)
        {
            if (!FileSystem.File.Exists(file))
            {
                Console.StdErrWriteLine($"File {file} does not exist.");
                return;
            }

            Statistics.IncreaseTotalFiles();

            var dvcFilePath = $"{file}.dvc";

            var md5 = await Hashing.ComputeMD5HashForFileAsync(file);
            var operation = "Added";

            if (FileSystem.File.Exists(dvcFilePath))
            {
                var md5InDvc = await DvcFileUtils.ReadHashFromDvcFile(dvcFilePath);
                if (md5InDvc == md5)
                {
                    //Console.WriteLine($"File {dvcFilePath} already exists and up-to-date");
                    Statistics.IncreaseUpToDateFiles();
                    return;
                }

                operation = "Re-added";
                Statistics.IncreaseReAddedFiles();
            }
            else
                Statistics.IncreaseAddedFiles();

            var fi = FileSystem.FileInfo.New(file);

            FileSystem.File.WriteAllText(dvcFilePath,
                $"""
                outs:
                - md5: {md5}
                  size: {fi.Length}
                  hash: md5
                  path: {fi.Name}

                """);

            var result = await CopyFileToCacheAsync(file, md5);

            var copyResult = result switch
            {
                CopyToCacheResult.Success => "Cached",
                CopyToCacheResult.NoCache => "No cache",
                _ => "Failed to cache"
            };

            Console.StdOutWriteLine($"{operation} {file} ({copyResult})");
        }

        private Task<CopyToCacheResult> CopyFileToCacheAsync(string file, string md5)
        {
            var cacheFile = DvcCache?.GetCacheFilePath(md5);
            if (cacheFile == null)
                return Task.FromResult(CopyToCacheResult.NoCache);

            try
            {
                FileSystem.File.Copy(file, cacheFile, true);
            }
            catch
            {
                return Task.FromResult(CopyToCacheResult.Failed);
            }

            return Task.FromResult(CopyToCacheResult.Success);
        }

        private enum CopyToCacheResult
        {
            Success,
            NoCache,
            Failed
        }

        private class AddStatistics
        {
            private volatile int _totalFiles;
            private volatile int _upToDateFiles;
            private volatile int _addedFiles;
            private volatile int _reAddedFiles;

            public int TotalFiles => _totalFiles;
            public int UpToDateFiles => _upToDateFiles;
            public int AddedFiles => _addedFiles;
            public int ReAddedFiles => _reAddedFiles;

            public void IncreaseTotalFiles() => Interlocked.Increment(ref _totalFiles);
            public void IncreaseUpToDateFiles() => Interlocked.Increment(ref _upToDateFiles);
            public void IncreaseAddedFiles() => Interlocked.Increment(ref _addedFiles);
            public void IncreaseReAddedFiles() => Interlocked.Increment(ref _reAddedFiles);

            internal void Reset()
            {
                Interlocked.Exchange(ref _totalFiles, 0);
                Interlocked.Exchange(ref _upToDateFiles, 0);
                Interlocked.Exchange(ref _addedFiles, 0);
                Interlocked.Exchange(ref _reAddedFiles, 0);
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append($"Total files: {TotalFiles}");

                if (AddedFiles > 0)
                    sb.Append($", Added: {AddedFiles}");

                if (ReAddedFiles > 0)
                    sb.Append($", Re-added: {ReAddedFiles}");

                if (UpToDateFiles > 0)
                    sb.Append($", Up-to-date: {UpToDateFiles}");

                return sb.ToString();
            }
        }
    }
}
