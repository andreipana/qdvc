using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace qdvc
{
    internal class PullCommand
    {
        public DvcCache? DvcCache { get; }
        public Credentials Credentials { get; }

        public PullCommand(DvcCache? dvcCache, Credentials credentials) 
        {
            DvcCache = dvcCache;
            Credentials = credentials;
        }

        public async Task ExecuteAsync(IEnumerable<string> files)
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = -1
            };

            await Parallel.ForEachAsync(files, options, async (dvcFilePath, _) =>
            {
                await PullDvcFile(dvcFilePath);
            });
        }


        async Task PullDvcFile(string dvcFilePath)
        {
            try
            {
                Console.WriteLine($"Pull     {dvcFilePath}");
                var md5 = await ReadHashFromDvcFile(dvcFilePath);
                if (md5 == null)
                {
                    Console.WriteLine($"Failed to read hash from {dvcFilePath}");
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

                        Console.WriteLine($"REPO  => {dvcFilePath}");

                        if (File.Exists(cacheFilePath))
                        {
                            // TODO: check if the file is the same?
                            Console.WriteLine($"CLASH    {md5} pulling {dvcFilePath} Sizes: {new FileInfo(cacheFilePath).Length} {new FileInfo(cacheFilePathTemp).Length}");

                            File.Delete(cacheFilePathTemp);
                        }
                        else
                        {
                            try
                            {
                                File.Move(cacheFilePathTemp, cacheFilePath);
                                new FileInfo(cacheFilePath).Attributes |= FileAttributes.ReadOnly;
                            }
                            catch
                            {
                                Console.WriteLine($"CLASH    MOVE {md5} pulling {cacheFilePath}");
                            }
                        }
                    }

                    {
                        File.Copy(cacheFilePath, targetFilePath, true);
                        new FileInfo(targetFilePath).Attributes &= ~FileAttributes.ReadOnly;

                        if (isFileInCache)
                            Console.WriteLine($"CACHE => {dvcFilePath}");
                    }
                }
                else
                {
                    await DownloadFileAsync(md5, targetFilePath);
                    Console.WriteLine($"REPO ->  {dvcFilePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to pull {dvcFilePath}: {ex.Message}");
            }
        }

        async Task DownloadFileAsync(string md5, string filePath)
        {
            if (Credentials == null)
                throw new SecurityException("No credentials provided");

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Credentials.Username}:{Credentials.Password}")));

            string url = $"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/{md5.Substring(0, 2)}/{md5.Substring(2)}";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new SecurityException("Unauthorized");
                }

                Console.WriteLine($"Failed to download {url}: {response.StatusCode}");
                return;
            }

            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await response.Content.CopyToAsync(fs);
        }

        async Task<string?> ReadHashFromDvcFile(string dvcFilePath)
        {
            try
            {
                var dvcFileContent = await File.ReadAllTextAsync(dvcFilePath);
                return ReadHashFromDvcFileContent(dvcFileContent);
            }
            catch (Exception)
            {
                return null;
            }
        }

        string? ReadHashFromDvcFileContent(string dvcFileContent)
        {
            const string marker = "md5: ";
            var index = dvcFileContent.IndexOf(marker) + marker.Length;
            return dvcFileContent.Substring(index, 32);
        }
    }
}
