using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static qdvc.Infrastructure.IOContext;
using Console = qdvc.Infrastructure.SystemContext.Console;

namespace qdvc.Commands
{
    public class PushCommand(DvcCache? dvcCache, HttpClient httpClient)
    {
        public DvcCache? DvcCache { get; } = dvcCache;

        public HttpClient HttpClient { get; } = httpClient;

        public async Task ExecuteAsync(IEnumerable<string> files)
        {
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

            return string.Empty;
        }

        private async Task PushDvcFile(string dvcFilePath)
        {
            if (!FileSystem.File.Exists(dvcFilePath))
            {
                Console.StdErrWriteLine($"File {dvcFilePath} does not exist.");
                return;
            }

            if (DvcCache == null)
            {
                // TODO: look for the file in current folder instead?
                Console.StdErrWriteLine("No DVC cache to take the file content from.");
                return;
            }

            try
            {
                var md5 = await DvcFileUtils.ReadHashFromDvcFile(dvcFilePath);
                if (md5 == null)
                {
                    Console.StdErrWriteLine($"Failed to read hash from {dvcFilePath}");
                    return;
                }

                if (!DvcCache.ContainsFile(md5))
                {
                    Console.StdErrWriteLine($"File md5 {md5} not found in the cache.");
                    return;
                }

                await UploadFileAsync(md5, dvcFilePath);
            }
            catch (Exception ex)
            {
                Console.StdErrWriteLine($"Failed to push {dvcFilePath}: {ex.Message}");
            }
        }

        private async Task UploadFileAsync(string md5, string dvcFilePath)
        {
            var filePath = DvcCache!.GetCacheFilePath(md5);
            var targetUrl = $"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/{md5[..2]}/{md5[2..]}";

            var headRequest = new HttpRequestMessage(HttpMethod.Head, targetUrl);
            var headResponse = await HttpClient.SendAsync(headRequest);
            var logLine = $"Pushing {dvcFilePath}";

            if (headResponse.IsSuccessStatusCode)
            {
                Console.StdOutWriteLine($"{logLine} FILE EXISTS");
            }
            else
            {
                using var stream = FileSystem.File.OpenRead(filePath);
                var fileStream = new StreamContent(stream);
                var response = await HttpClient.PutAsync(targetUrl, fileStream);

                Console.StdOutWriteLine(!response.IsSuccessStatusCode ? $"{logLine} ERROR ({response.StatusCode})" : $"{logLine} SUCCESS");
            }
        }
    }
}
