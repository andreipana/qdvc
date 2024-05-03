using qdvc;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

Console.WriteLine($"Quick DVC v{VersionUtils.GetAssemblyInformationalVersion()}");

var sw = Stopwatch.StartNew();

IOContext.Initialize();

CommandLineArguments Args = new(args);

var paths = Args.Paths.Select(Path.GetFullPath);

var dvcFolder = DvcCache.FindDvcRootForRepositorySubPath(paths.First());
var dvcConfig = DvcConfig.ReadConfigFromFolder(dvcFolder);

var credentials = Credentials.DetectFrom(Args, dvcFolder);

if (credentials == null)
{
    Console.WriteLine("Failed to detect credentials.");
    //TODO: let it run, but bring data only from cache? might be useful, but needs a big warning that this is the case.
    Environment.Exit(2);
}
else
    Console.WriteLine($"Credentials loaded from {credentials.Source}");

var files = paths.SelectMany(FilesEnumerator.EnumerateFilesFromPath);

if (files.Any() == false)
{
    Console.WriteLine("No files found.");
    Environment.Exit(3);
}

//foreach (var file in files)
//    Console.WriteLine($"File: {file}");

var cacheDir = dvcConfig.GetCacheDirAbsolutePath();
var dvcCache = DvcCache.CreateFromFolder(cacheDir) ??
               DvcCache.CreateFromRepositorySubFolder(files.First());

Console.WriteLine($"DVC cache folder: {dvcCache?.DvcCacheFolder}");

using var httpClient = new HttpClient();
httpClient.Timeout = TimeSpan.FromMinutes(10);
if (credentials != null)
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{credentials.Username}:{credentials.Password}")));

switch (Args.Command)
{
    case "pull":
        await new PullCommand(dvcCache, httpClient).ExecuteAsync(files);
        break;
    case "add":
        await new AddCommand(dvcCache).ExecuteAsync(files);
        break;
    default:
        Console.WriteLine($"Invalid command '{Args.Command}'");
        break;
}

Console.WriteLine($"Finished in {sw.Elapsed}");