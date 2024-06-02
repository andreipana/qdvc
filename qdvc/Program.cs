using qdvc.Commands;
using qdvc.Infrastructure;
using qdvc.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static qdvc.Infrastructure.IOContext;


namespace qdvc;

class Program
{
    static async Task<int> Main(string[] args)
    {
        SystemContext.Initialize();

        Console.WriteLine($"Quick DVC v{VersionUtils.GetAssemblyInformationalVersion()}");

        var sw = Stopwatch.StartNew();

        IOContext.Initialize();

        CommandLineArguments Args = CommandLineArguments.ParseUsingSCL(args);

        if (Args.Command == null)
            return 0;

        if (!Args.Paths.Any())
        {
            Console.WriteLine("ERROR: No paths provided.");
            return 2;
        }

        var paths = Args.Paths.Select(Path.GetFullPath);

        var currentDirectory = FileSystem.Directory.GetCurrentDirectory();
        var dvcFolder = DvcCache.FindDvcRootForRepositorySubPath(currentDirectory) ??
                        DvcCache.FindDvcRootForRepositorySubPath(paths.First());

        if (dvcFolder == null)
        {
            Console.WriteLine("ERROR: No DVC repository found.");
            return 3;
        }

        var dvcConfig = DvcConfig.ReadConfigFromFolder(dvcFolder);

        var credentials = Credentials.DetectFrom(Args, dvcConfig);

        if (credentials == null)
        {
            Console.WriteLine("Failed to detect credentials.");
            //TODO: let it run, but bring data only from cache? might be useful, but needs a big warning that this is the case.
            return 4;
        }
        else
            Console.WriteLine($"Credentials loaded from {credentials.Source}");

        var files = paths.SelectMany(FilesEnumerator.EnumerateFilesFromPath);

        if (files.Any() == false)
        {
            Console.WriteLine("No files found.");
            return 5;
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
            case "push":
                await new PushCommand(dvcCache, httpClient).ExecuteAsync(files);
                break;
            case "add":
                await new AddCommand(dvcCache).ExecuteAsync(files);
                break;
            case "status":
                await new StatusCommand(dvcCache).ExecuteAsync(files);
                break;
            default:
                Console.WriteLine($"Invalid command '{Args.Command}'");
                break;
        }

        Console.WriteLine($"Finished in {sw.Elapsed}");

        return 0;
    }
}