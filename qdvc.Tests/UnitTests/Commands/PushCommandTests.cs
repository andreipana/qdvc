using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using qdvc.Commands;
using qdvc.Tests.TestInfrastructure;
using qdvc.Utilities;
using RichardSzalay.MockHttp;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using static qdvc.Infrastructure.IOContext;

namespace qdvc.Tests.UnitTests.Commands
{
    [TestClass]
    public class PushCommandTests : CommandTests
    {
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

            var testRepository = new TestRepository();

            httpClient = testRepository.CreateClient();
        }

        [TestMethod]
        public async Task PushCommand_UploadsToRemoteRepo_TheSpecifiedFile()
        {
            await new PushCommand(dvcCache, httpClient).ExecuteAsync([@"C:\work\MyRepo\Data\Assets\file.txt"]);

            httpClient
                .GetStringAsync(
                    "https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/85/626f0d045734ec369864a51e37393f")
                .Result.Should().Be("“Let there be light”, and there was light.");
        }

        [TestMethod]
        public async Task PushCommand_UploadsToRemoteRepo_TheTrackedFile_WhenSpecifiedAsDvcFile()
        {
            await new PushCommand(dvcCache, httpClient).ExecuteAsync([@"C:\work\MyRepo\Data\Assets\file.txt.dvc"]);

            httpClient
                .GetStringAsync(
                    "https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/85/626f0d045734ec369864a51e37393f")
                .Result.Should().Be("“Let there be light”, and there was light.");
        }

        [TestMethod]
        public async Task PushCommand_DoesntUpload_IfTheFileIsAlreadyUploaded()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Head, "*").Respond(HttpStatusCode.OK);
            mockHttp.When(HttpMethod.Put, "*").Respond(_ => throw new System.Exception());
            var httpClient = new HttpClient(mockHttp);

            await new PushCommand(dvcCache, httpClient).ExecuteAsync([@"C:\work\MyRepo\Data\Assets\file.txt.dvc"]);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task PushCommand_OutputsErrorCode_IfPushingFailed()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Head, "*").Respond(HttpStatusCode.NotFound);
            mockHttp.When(HttpMethod.Put, "*").Respond(HttpStatusCode.Unauthorized);
            var httpClient = new HttpClient(mockHttp);

            await new PushCommand(dvcCache, httpClient).ExecuteAsync([@"C:\work\MyRepo\Data\Assets\file.txt.dvc"]);

            Console.StdErr.Should().Contain(@"Failed to push C:\work\MyRepo\Data\Assets\file.txt: Unauthorized");
        }

        [TestMethod]
        public async Task PushCommand_Outputs_Statistics()
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data");
            await new PushCommand(dvcCache, httpClient)
                .ExecuteAsync(files);

            Console.StdOut.Should().Contain(@"Pushed C:\work\MyRepo\Data\Assets\file.txt");
            Console.StdOut.Should().Contain("Total files: 1, Pulled: 1");
        }
    }
}