using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using static qdvc.Infrastructure.IOContext;
using Console = qdvc.Infrastructure.SystemContext.Console;

namespace qdvc.Commands
{
    public class PullCommand
    {
        public DvcCache? DvcCache { get; }

        public HttpClient HttpClient { get; }

        public PullCommand(DvcCache? dvcCache, HttpClient httpClient)
        {
            DvcCache = dvcCache;
            HttpClient = httpClient;
        }

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
                await PullDvcFile(dvcFilePath);
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

        async Task PullDvcFile(string dvcFilePath)
        {
            try
            {
                Console.StdOutWriteLine($"Pull     {dvcFilePath}");
                var md5 = await DvcFileUtils.ReadHashFromDvcFile(dvcFilePath);
                if (md5 == null)
                {
                    Console.StdErrWriteLine($"Failed to read hash from {dvcFilePath}");
                    return;
                }

                var targetFilePath = dvcFilePath.Substring(0, dvcFilePath.Length - 4);

                if (DvcCache != null)
                {
                    var cacheFilePath = DvcCache.GetCacheFilePath(md5);
                    var cacheFilePathTemp = $"{cacheFilePath}.{Guid.NewGuid()}";
                    var isFileInCache = DvcCache.ContainsFile(md5);

                    if (!isFileInCache)
                    {
                        isFileInCache = false;
                        await DownloadFileAsync(md5, cacheFilePathTemp);

                        Console.StdOutWriteLine($"REPO  => {dvcFilePath}");

                        if (FileSystem.File.Exists(cacheFilePath))
                        {
                            // TODO: check if the file is the same?
                            Console.StdErrWriteLine($"CLASH    {md5} pulling {dvcFilePath} Sizes: {FileSystem.FileInfo.New(cacheFilePath).Length} {FileSystem.FileInfo.New(cacheFilePathTemp).Length}");

                            FileSystem.File.Delete(cacheFilePathTemp);
                        }
                        else
                        {
                            try
                            {
                                FileSystem.File.Move(cacheFilePathTemp, cacheFilePath);
                                FileSystem.FileInfo.New(cacheFilePath).Attributes |= FileAttributes.ReadOnly;
                            }
                            catch
                            {
                                Console.StdErrWriteLine($"CLASH    MOVE {md5} pulling {cacheFilePath}");
                            }
                        }
                    }

                    {
                        FileSystem.File.Copy(cacheFilePath, targetFilePath, true);
                        FileSystem.FileInfo.New(targetFilePath).Attributes &= ~FileAttributes.ReadOnly;

                        if (isFileInCache)
                            Console.StdOutWriteLine($"CACHE => {targetFilePath}");
                    }
                }
                else
                {
                    await DownloadFileAsync(md5, targetFilePath);
                    Console.StdOutWriteLine($"REPO ->  {targetFilePath}");
                }
            }
            catch (Exception ex)
            {
                Console.StdErrWriteLine($"Failed to pull {dvcFilePath}: {ex.Message}");
            }
        }

        async Task DownloadFileAsync(string md5, string filePath)
        {
            string url = $"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/{md5[..2]}/{md5[2..]}";
            var response = await HttpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new SecurityException("Unauthorized");
                }

                Console.StdErrWriteLine($"Failed to download {url}: {response.StatusCode}");
                return;
            }

            using var fs = FileSystem.FileStream.New(filePath, FileMode.Create, FileAccess.Write);
            await response.Content.CopyToAsync(fs);
        }
    }
}
