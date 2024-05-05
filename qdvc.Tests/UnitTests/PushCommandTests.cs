using System.Collections.Generic;
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
        private readonly Credentials credentials;
        private readonly DvcCache dvcCache;
        private readonly HttpClient httpClient;

        public PushCommandTests()
        {
            Initialize(new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\work\MyRepo\.dvc\cache\files\md5\85\"] = new MockDirectoryData(),
                [@"C:\work\MyRepo\Data\Assets\file.txt.dvc"] = new(
                    """
                    outs:
                    - md5: 85626f0d045734ec369864a51e37393f
                      size: 46
                      hash: md5
                      path: file.txt

                    """),
                [@"C:\work\MyRepo\.dvc\cache\files\md5\85\626f0d045734ec369864a51e37393f"] =
                    "“Let there be light”, and there was light."
            }));

            dvcCache = new DvcCache(@"C:\work\MyRepo\.dvc\cache\");

            credentials = new Credentials("ghst", "21232f297a57a5a743894a0e4a801fc3", "hardcoded");

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(
                    "https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/85/626f0d045734ec369864a51e37393f")
                .Respond("application/octet-stream", "“Let there be light”, and there was light.");

            httpClient = new HttpClient(mockHttp);
        }

        [TestMethod]
        public async Task PushCommand_CheckDownloadAfter()
        {
            // TODO: check more error codes
            await new PushCommand(dvcCache, httpClient).ExecuteAsync([@"C:\work\MyRepo\Data\Assets\file.txt.dvc"]);

            httpClient
                .GetStringAsync(
                    "https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/85/626f0d045734ec369864a51e37393f")
                .Result.Should().Be("“Let there be light”, and there was light.");
        }
    }
}