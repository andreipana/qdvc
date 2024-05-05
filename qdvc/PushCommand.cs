using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static qdvc.IOContext;

namespace qdvc
{
    public class PushCommand(DvcCache? dvcCache, HttpClient httpClient)
    {
        public DvcCache? DvcCache { get; } = dvcCache;

        public HttpClient HttpClient { get; } = httpClient;

        public async Task ExecuteAsync(IEnumerable<string> files)
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = -1
            };

            var dvcFiles = files.Where(f => f.EndsWith(".dvc", StringComparison.OrdinalIgnoreCase));

            await Parallel.ForEachAsync(dvcFiles, options, async (dvcFilePath, _) =>
            {
                await PushDvcFile(dvcFilePath);
            });
        }

        private async Task PushDvcFile(string dvcFilePath)
        {
            if (!FileSystem.File.Exists(dvcFilePath))
            {
                Console.WriteLine($"File {dvcFilePath} does not exist.");
                return;
            }

            if (DvcCache == null)
            {
                // TODO: look for the file in current folder instead?
                Console.WriteLine("No DVC cache to take the file content from.");
                return;
            }

            try
            {
                var md5 = await DvcFileUtils.ReadHashFromDvcFile(dvcFilePath);
                if (md5 == null)
                {
                    Console.WriteLine($"Failed to read hash from {dvcFilePath}");
                    return;
                }

                if (!DvcCache.ContainsFile(md5))
                {
                    Console.WriteLine($"File md5 {md5} not found in the cache.");
                    return;
                }

                await UploadFileAsync(md5);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to push {dvcFilePath}: {ex.Message}");
            }
        }

        async Task UploadFileAsync(string md5)
        {
            var filePath = DvcCache.GetCacheFilePath(md5);
            var artifactName = md5[2..];
            var targetPath = $"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/{md5[..2]}/{artifactName}";

            var headRequest = new HttpRequestMessage(HttpMethod.Head, targetPath);
            var headResponse = await HttpClient.SendAsync(headRequest);
            var logLine = $"Pushing     {filePath} to artifactory in `{md5[..2]}/{md5[2..]}` ... ";

            if (!headResponse.IsSuccessStatusCode)
            {
                var fileData = await FileSystem.File.ReadAllBytesAsync(filePath);
                var fileContent = new ByteArrayContent(fileData);
                var multipartContent = new MultipartFormDataContent();
                multipartContent.Add(fileContent, "file", artifactName);
                var response = await HttpClient.PutAsync(targetPath, multipartContent);

                Console.WriteLine(!response.IsSuccessStatusCode ? $"{logLine}ERROR ({response.StatusCode})" : $"{logLine}SUCCESS");
            }
            else
            {
                Console.WriteLine($"{logLine}FILE EXISTS");
            }
        }
    }
}
