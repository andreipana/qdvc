using qdvc;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

Console.WriteLine("Quick DVC");

CommandLineArguments Args = new(args);

//return;

var sw = Stopwatch.StartNew();

var paths = Args.Paths.Select(Path.GetFullPath);

var dvcCache = DvcCache.GetDvcCacheForFolder(paths.First());
Console.WriteLine($"DVC cache folder: {dvcCache?.DvcCacheFolder}");

var files = paths.SelectMany(GetFilesFromPath);

var options = new ParallelOptions
{
    MaxDegreeOfParallelism = -1
};

await Parallel.ForEachAsync(files, options, async (dvcFilePath, _) =>
{
    await PullDvcFile(dvcFilePath);
});

Console.WriteLine($"Finished in {sw.Elapsed}");

return;

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

        if (dvcCache != null)
        {
            var cacheFilePath = dvcCache.GetCacheFilePath(md5);
            var cacheFilePathTemp = $"{cacheFilePath}.{Guid.NewGuid()}";
            var isFileInCache = dvcCache.ContainsFile(md5);

            if (!isFileInCache)
            {
                isFileInCache = false;
                await DownloadFileAsync(md5, cacheFilePathTemp);

                Console.WriteLine($"REPO  => {dvcFilePath}");

                if (File.Exists(cacheFilePath))
                {
                    // TODO: check if the file is the same
                    Console.WriteLine($"CLASH    {md5} pulling {dvcFilePath}");
                    Console.WriteLine($"{new FileInfo(cacheFilePath).Length} {new FileInfo(cacheFilePathTemp).Length}");
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
        Console.WriteLine($"Failed to pull {dvcFilePath}: {ex}");
    }
}

async Task DownloadFileAsync(string md5, string filePath)
{
    using var client = new HttpClient();

    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Args.Username}:{Args.Password}")));
    string url = $"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/{md5.Substring(0, 2)}/{md5.Substring(2)}";
    //Console.WriteLine(url);
    var response = await client.GetAsync(url);
    //Console.WriteLine(targetFilePath);

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

IEnumerable<string> GetFilesFromPath(string path)
{
    if (path.EndsWith(".dvc", StringComparison.OrdinalIgnoreCase))
        return new[] { path };

    if (Directory.Exists(path))
        return Directory.EnumerateFiles(path, "*.dvc", SearchOption.AllDirectories);

    return Enumerable.Empty<string>();
}