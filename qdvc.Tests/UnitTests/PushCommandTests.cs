using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using static qdvc.IOContext;

namespace qdvc.Tests.UnitTests
{
    [TestClass]
    public class PushCommandTests
    {
        private readonly IFileSystem fileSystem;
        private Credentials credentials;
        private DvcCache dvcCache;
        private HttpClient httpClient;

        public PushCommandTests()
        {
            fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\work\MyRepo\Data\file.txt"] = "“Let there be light”, and there was light."
            });

            Initialize(fileSystem);

            dvcCache = new DvcCache(@"C:\work\MyRepo\.dvc\cache\");

            credentials = new Credentials("ghst", "21232f297a57a5a743894a0e4a801fc3", "hardcoded");

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(
                    $"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/85/626f0d045734ec369864a51e37393f")
                .Respond("application/octet-stream", "“Let there be light”, and there was light.");

            httpClient = new HttpClient(mockHttp);
        }

        [TestMethod]
        public async Task AddPushPull_Workflow()
        {
            var filePath = @"C:\work\MyRepo\Data\file.txt";

            await new AddCommand(dvcCache).ExecuteAsync(new[] { filePath });

            fileSystem.File.Exists($"{filePath}.dvc").Should().BeTrue();

            var dvcFileContent = fileSystem.File.ReadAllText($"{filePath}.dvc");
            dvcFileContent.Should().Be(
                """
                outs:
                - md5: 85626f0d045734ec369864a51e37393f
                  size: 46
                  hash: md5
                  path: file.txt

                """);

            fileSystem.File.Exists(dvcCache.GetCacheFilePath("85626f0d045734ec369864a51e37393f")).Should().BeTrue();

            // TODO: check more error codes
            await new PushCommand(dvcCache, httpClient).ExecuteAsync([$"{filePath}.dvc"]);

            fileSystem.File.Delete(dvcCache.GetCacheFilePath("85626f0d045734ec369864a51e37393f"));
            fileSystem.File.Delete(filePath);

            await new PullCommand(dvcCache, httpClient).ExecuteAsync([$"{filePath}.dvc"]);

            fileSystem.File.ReadAllText(filePath).Should().Be("“Let there be light”, and there was light.");
        }
    }
}