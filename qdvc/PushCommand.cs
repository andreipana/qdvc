using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using static qdvc.IOContext;

namespace qdvc
{
    internal class PushCommand(DvcCache? dvcCache, Credentials credentials)
    {
        public Credentials Credentials { get; } = credentials;

        public DvcCache? DvcCache { get; } = dvcCache;

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

                Console.Write($"Pushing     {dvcFilePath} to artifactory in `{md5[..2]}/{md5[2..]}` ... ");

                var cachedFilePath = DvcCache.GetCacheFilePath(md5);
                // put file to artifactory (if exists, do nothing - log message if ok/nok)

                await UploadFileAsync(md5, cachedFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to pull {dvcFilePath}: {ex.Message}");
            }
        }

        async Task UploadFileAsync(string md5, string filePath)
        {
            if (Credentials == null)
                throw new SecurityException("No credentials provided");

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Credentials.Username}:{Credentials.Password}")));

            var artifactName = md5[2..];
            var targetPath = $"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/_foo/{artifactName}";

            var headRequest = new HttpRequestMessage(HttpMethod.Head, targetPath);
            var headResponse = await client.SendAsync(headRequest);

            if (!headResponse.IsSuccessStatusCode)
            {
                var fileData = await FileSystem.File.ReadAllBytesAsync(filePath);
                var fileContent = new ByteArrayContent(fileData);
                var multipartContent = new MultipartFormDataContent();
                multipartContent.Add(fileContent, "file", artifactName);
                var response = await client.PutAsync(targetPath, multipartContent);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"ERROR ({response.StatusCode})");
                }
                else
                {
                    Console.WriteLine("SUCCESS");
                }
            }
            else
            {
                Console.WriteLine("FILE EXISTS");
            }
        }
    }
}
