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
    public class PushCommand(DvcCache? dvcCache, HttpClient httpClient)
    {
        public DvcCache? DvcCache { get; } = dvcCache;

        public HttpClient HttpClient { get; } = httpClient;

        private PushStatistics Statistics { get; } = new PushStatistics();

        public async Task ExecuteAsync(IEnumerable<string> files)
        {
            Statistics.Reset();
            processedFiles.Clear();

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = -1
            };

            var dvcFiles = files.Select(GetTargetFile).Where(f => f != string.Empty);

            await Parallel.ForEachAsync(dvcFiles, options, async (dvcFilePath, _) =>
            {
                await PushDvcFile(dvcFilePath);
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

            Console.StdErrWriteLine($"File {file} is not tracked.");
            Statistics.IncreaseUntrackedFiles();

            return string.Empty;
        }

        private async Task PushDvcFile(string dvcFilePath)
        {
            Statistics.IncreaseTotalFiles();

            if (!FileSystem.File.Exists(dvcFilePath))
            {
                Console.StdErrWriteLine($"File {dvcFilePath} does not exist.");
                Statistics.IncreaseFailedFiles();
                return;
            }

            if (DvcCache == null)
            {
                // TODO: look for the file in current folder instead?
                Console.StdErrWriteLine("No DVC cache to take the file content from.");
                Statistics.IncreaseFailedFiles();
                return;
            }

            var file = dvcFilePath.ToLower().EndsWith(".dvc") ? dvcFilePath[..^4] : dvcFilePath;

            try
            {
                var md5 = await DvcFileUtils.ReadHashFromDvcFile(dvcFilePath);
                if (md5 == null)
                {
                    Console.StdErrWriteLine($"Failed to read hash from {dvcFilePath}");
                    Statistics.IncreaseFailedFiles();
                    return;
                }

                if (!DvcCache.ContainsFile(md5))
                {
                    Console.StdErrWriteLine($"File for {file} ({md5}) not found in the cache.");
                    Statistics.IncreaseNotCachedFiles();
                    return;
                }

                await UploadFileAsync(md5, dvcFilePath);
            }
            catch (Exception ex)
            {
                Console.StdErrWriteLine($"Failed to push {file}: {ex.Message}");
                Statistics.IncreaseFailedFiles();
            }
        }

        private async Task UploadFileAsync(string md5, string dvcFilePath)
        {
            var file = dvcFilePath.ToLower().EndsWith(".dvc") ? dvcFilePath[..^4] : dvcFilePath;

            var filePath = DvcCache!.GetCacheFilePath(md5);
            var targetUrl = $"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/{md5[..2]}/{md5[2..]}";

            var headRequest = new HttpRequestMessage(HttpMethod.Head, targetUrl);
            var headResponse = await HttpClient.SendAsync(headRequest);

            if (headResponse.IsSuccessStatusCode)
            {
                Console.StdOutWriteLine($"Existing {file}");
                Statistics.IncreaseAlreadyPushedFiles();
            }
            else
            {
                using var stream = FileSystem.File.OpenRead(filePath);
                var fileStream = new StreamContent(stream);
                var response = await HttpClient.PutAsync(targetUrl, fileStream);

                if (!response.IsSuccessStatusCode)
                    throw new Exception(response.StatusCode.ToString());

                Console.StdOutWriteLine($"Pushed {file}");
                Statistics.IncreasePushedFiles();
            }
        }

        private class PushStatistics
        {
            private volatile int _totalFiles;
            private volatile int _pushedFiles;
            private volatile int _untrackedFiles;
            private volatile int _failedFiles;
            private volatile int _alreadyPushedFiles;
            private volatile int _notCachedFiles;

            public int TotalFiles => _totalFiles;
            public int PushedFiles => _pushedFiles;
            public int UntrackedFiles => _untrackedFiles;
            public int FailedFiles => _failedFiles;
            public int AlreadyPushedFiles => _alreadyPushedFiles;
            public int NotCachedFiles => _notCachedFiles;

            internal void IncreaseTotalFiles() => Interlocked.Increment(ref _totalFiles);
            internal void IncreasePushedFiles() => Interlocked.Increment(ref _pushedFiles);
            internal void IncreaseUntrackedFiles() => Interlocked.Increment(ref _untrackedFiles);
            internal void IncreaseFailedFiles() => Interlocked.Increment(ref _failedFiles);
            internal void IncreaseAlreadyPushedFiles() => Interlocked.Increment(ref _alreadyPushedFiles);
            internal void IncreaseNotCachedFiles() => Interlocked.Increment(ref _notCachedFiles);

            public void Reset()
            {
                _totalFiles = 0;
                _pushedFiles = 0;
                _untrackedFiles = 0;
                _failedFiles = 0;
                _alreadyPushedFiles = 0;
                _notCachedFiles = 0;
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append($"Total files: {TotalFiles}");

                if (PushedFiles > 0)
                    sb.Append($", Pulled: {PushedFiles}");

                if (AlreadyPushedFiles > 0)
                    sb.Append($", Already pushed: {AlreadyPushedFiles}");

                if (NotCachedFiles > 0)
                    sb.Append($", Not cached: {NotCachedFiles}");

                if (UntrackedFiles > 0)
                    sb.Append($", Untracked: {UntrackedFiles}");

                if (FailedFiles > 0)
                    sb.Append($", Failed: {FailedFiles}");

                return sb.ToString();
            }
        }
    }
}
