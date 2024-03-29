using System.Net.Http.Headers;
using System.Text;

Console.WriteLine("Quick DVC");


//const string path = @"c:\work\infinity.2nd\Data\Projects\rtc\low_40scans_no_images-Vist_AutoBlackAndWhiteTarget";
//Directory.EnumerateFiles(path, "*.dvc", SearchOption.AllDirectories)
//    .ToList()
//    .ForEach(dvc => Console.WriteLine(dvc));

var user = Environment.GetEnvironmentVariable("ARTIFACTORY_USERNAME");
var pass = Environment.GetEnvironmentVariable("ARTIFACTORY_TOKEN");

const string md5 = "2bd0312ed1d8c8e9c718029a4940fb3c";
var client = new HttpClient();
// add credentials to the client
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{pass}")));
string dlpath = $"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/{md5.Substring(0, 2)}/{md5.Substring(2)}";
Console.WriteLine(dlpath);
var response = await client.GetAsync(dlpath);

using (var fs = new FileStream("test.db", FileMode.Create, FileAccess.Write))
{
    await response.Content.CopyToAsync(fs);
}

Console.WriteLine(response.StatusCode);