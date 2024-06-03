using System;
using System.Collections.Concurrent;
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
    public class StatusRepoCommand(DvcCache? dvcCache, HttpClient httpClient)
    {
        public DvcCache? DvcCache { get; } = dvcCache;
        public HttpClient HttpClient { get; } = httpClient;

        public StatusRepoStatistics Statistics { get; } = new StatusRepoStatistics();

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
                //Note: Here should get only .dvc files, because GetTargetFile above filters out all the non .dvc files.
                await OutputDvcFileStatusAsync(file);
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
            
            Console.StdOutWriteLine($"Untracked: {file}");
            Statistics.IncrementUntrackedFiles();

            return string.Empty;
        }

        private async Task OutputDvcFileStatusAsync(string file)
        {
            var md5 = await DvcFileUtils.ReadHashFromDvcFile(file);
            if (md5 == null)
            {
                Console.StdOutWriteLine($"Invalid: {file}");
                return;
            }

            Statistics.IncrementTotalFiles();

            var targetUrl = $"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/{md5[..2]}/{md5[2..]}";

            var headRequest = new HttpRequestMessage(HttpMethod.Head, targetUrl);
            var headResponse = await HttpClient.SendAsync(headRequest);

            if (headResponse.IsSuccessStatusCode)
            {
                if (DvcCache != null && DvcCache.ContainsFile(md5))
                {
                    Console.StdOutWriteLine($"Up-to-date: {file}");
                    Statistics.IncrementUpToDateFiles();
                }
                else
                {
                    Console.StdOutWriteLine($"Not cached: {file}");
                    Statistics.IncrementNotCachedFiles();
                }
            }
            else
            {
                Console.StdOutWriteLine($"Not pushed: {file}");
                Statistics.IncrementNotPushedFiles();
            }

            //Statistics.IncrementMissingFiles();
        }
    }

    public class StatusRepoStatistics
    {
        private volatile int _totalFiles;
        private volatile int _untrackedFiles;
        private volatile int _notCachedFiles;
        private volatile int _notPushedFiles;
        private volatile int _upToDateFiles;

        public int TotalFiles => _totalFiles;
        public int UntrackedFiles => _untrackedFiles;
        public int NotCachedFiles => _notCachedFiles;
        public int NotPushedFiles => _notPushedFiles;
        public int UpToDateFiles => _upToDateFiles;

        internal void IncrementTotalFiles() => Interlocked.Increment(ref _totalFiles);

        internal void IncrementNotCachedFiles() => Interlocked.Increment(ref _notCachedFiles);
        
        internal void IncrementUntrackedFiles() => Interlocked.Increment(ref _untrackedFiles);

        internal void IncrementNotPushedFiles() => Interlocked.Increment(ref _notPushedFiles);

        internal void IncrementUpToDateFiles() => Interlocked.Increment(ref _upToDateFiles);

        public void Reset()
        {
            _totalFiles = 0;
            _untrackedFiles = 0;
            _notCachedFiles = 0;
            _notPushedFiles = 0;
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

            //Note: maybe not show untracked files in the status --repo output, as this
            //is between cache and remote, and untracked files are not in the cache.
            //if (UntrackedFiles > 0)
            //{
            //    sb.Append($", Untracked: {UntrackedFiles}");
            //    everythingUpToDate = false;
            //}

            if (NotCachedFiles > 0)
            {
                sb.Append($", Not cached: {NotCachedFiles}");
                everythingUpToDate = false;
            }

            if (NotPushedFiles > 0)
            {
                sb.Append($", Not pushed: {NotPushedFiles}");
                everythingUpToDate = false;
            }

            if (everythingUpToDate)
                sb.Append(", Everything is up to date");

            return sb.ToString();
        }
    }
}
