using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

Console.WriteLine("Quick DVC");

var user = Environment.GetEnvironmentVariable("ARTIFACTORY_USERNAME");
var pass = Environment.GetEnvironmentVariable("ARTIFACTORY_TOKEN");

//var sw = Stopwatch.StartNew();
//const string path = @"c:\work\infinity.2nd\Data\Projects\rtc\low_40scans_no_images-Vist_AutoBlackAndWhiteTarget";
//const string path = @"c:\work\infinity.2nd\Data\Projects\rtc";
//const string path = @"c:\work\infinity.2nd\Data\Projects\";
//const string path = @"c:\work\infinity.2nd\Data\";
//var files = Directory.EnumerateFiles(path, "*.dvc", SearchOption.AllDirectories)
//    .Take(10)
//    .ToList();
////files.ForEach(dvc => Console.WriteLine(dvc));
//Console.WriteLine(files.Count);
//Console.WriteLine(sw.Elapsed);

const string dvcFilePath = @"c:\work\infinity.2nd\Data\Projects\rtc\low_40scans_no_images-Vist_AutoBlackAndWhiteTarget\da2ada28-06eb-4844-a14e-2d3ff16e665f\project_version_1647868925000.db.dvc";
await PullDvcFile(dvcFilePath);

return;

async Task PullDvcFile(string dvcFilePath)
{
    var md5 = await ReadHashFromDvcFile(dvcFilePath);
    if (md5 == null)
    {
        Console.WriteLine($"Failed to read hash from {dvcFilePath}");
        return;
    }

    using var client = new HttpClient();

    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{pass}")));
    string url = $"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/{md5.Substring(0, 2)}/{md5.Substring(2)}";
    //Console.WriteLine(url);
    var response = await client.GetAsync(url);
    var targetFilePath = dvcFilePath.Substring(0, dvcFilePath.Length - 4);
    //Console.WriteLine(targetFilePath);

    using var fs = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write);
    
    await response.Content.CopyToAsync(fs);

    Console.WriteLine(response.StatusCode);
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