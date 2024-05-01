using qdvc;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

var cacheDir = dvcConfig.GetCacheDirAbsolutePath();
var dvcCache = DvcCache.CreateFromFolder(cacheDir) ??
               DvcCache.CreateFromRepositorySubFolder(files.First());

Console.WriteLine($"DVC cache folder: {dvcCache?.DvcCacheFolder}");

switch (Args.Command)
{
    case "pull":
        await new PullCommand(dvcCache, credentials).ExecuteAsync(files);
        break;
    default:
        Console.WriteLine($"Invalid command '{Args.Command}'");
        break;
}

Console.WriteLine($"Finished in {sw.Elapsed}");