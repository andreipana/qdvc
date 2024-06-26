﻿using FluentAssertions;
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
    public class PullCommandTests : CommandTests
    {
        private DvcCache dvcCache;
        private Credentials credentials;
        private HttpClient httpClient;

        public PullCommandTests()
        {
            Initialize(new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\work\MyRepo\Data\untracked-file.txt"] = "The quick brown fox jumps over the lazy dog.",

                [@"C:\work\MyRepo\.dvc\cache"] = new MockDirectoryData(),
                [@"C:\work\MyRepo\Data\Assets\file.txt.dvc"] = new MockFileData(
                    """
                    outs:
                    - md5: 463002689330bae2f4adf13f4c7d333c
                      size: 14
                      hash: md5
                      path: file.txt
                    
                    """),
                [@"C:\work\MyRepo\.dvc\cache\files\md5\8b\5dc2bafbe03346676bd13095d02cec"] = new MockFileData("Cached file"),
                [@"C:\work\MyRepo\Data\Assets\cached_file.txt.dvc"] = new MockFileData(
                    """
                    outs:
                    - md5: 8b5dc2bafbe03346676bd13095d02cec
                      size: 11
                      hash: md5
                      path: cached_file.txt
                    
                    """),
            }));

            dvcCache = new DvcCache(@"C:\work\MyRepo\.dvc\cache");
            credentials = new Credentials("andrei", "asdfgh", "hardcoded");

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/46/3002689330bae2f4adf13f4c7d333c")
                    .Respond("application/octet-stream", "Code is poetry");

            httpClient = new HttpClient(mockHttp);
        }

        [TestMethod]
        public async Task PullCommand_Downloads_TheSpecifiedFile()
        {
            var filePath = @"C:\work\MyRepo\Data\Assets\file.txt";

            await new PullCommand(dvcCache, httpClient)
                .ExecuteAsync([$"{filePath}"]);

            FileSystem.File.ReadAllText(filePath)
                .Should().Be("Code is poetry");
        }

        [TestMethod]
        public async Task PullCommand_Downloads_TheTrackedFile_WhenSpecifiedAsDvcFile()
        {
            var filePath = @"C:\work\MyRepo\Data\Assets\file.txt";

            await new PullCommand(dvcCache, httpClient)
                .ExecuteAsync([$"{filePath}.dvc"]);

            FileSystem.File.ReadAllText(filePath)
                .Should().Be("Code is poetry");
        }

        [TestMethod]
        public async Task PullCommand_CopiesFromCache_TheSpecifiedFile_WhenTheFileAlreadyExistsInCache()
        {
            var filePath = @"C:\work\MyRepo\Data\Assets\cached_file.txt";

            await new PullCommand(dvcCache, httpClient)
                .ExecuteAsync([$"{filePath}.dvc"]);

            FileSystem.File.ReadAllText(filePath)
                .Should().Be("Cached file");
        }

        [TestMethod]
        public async Task PullCommand_Outputs_TheNameOfTheFilePulled()
        {
            var filePath = @"C:\work\MyRepo\Data\Assets\file.txt";

            await new PullCommand(dvcCache, httpClient)
                .ExecuteAsync([$"{filePath}.dvc"]);

            Console.StdOut.Should().Contain(filePath);
        }

        [TestMethod]
        public async Task PullCommand_Outputs_Cache_WhenFileIsPulledFromCache()
        {
            var filePath = @"C:\work\MyRepo\Data\Assets\file.txt";

            await new PullCommand(dvcCache, httpClient)
                .ExecuteAsync([$"{filePath}.dvc"]);

            Console.StdOut.Should().Contain("REPO  => ");
        }

        [TestMethod]
        public async Task PullCommand_Outputs_Repo_WhenFileIsPulledFromRepo()
        {
            var filePath = @"C:\work\MyRepo\Data\Assets\cached_file.txt";

            await new PullCommand(dvcCache, httpClient)
                .ExecuteAsync([$"{filePath}.dvc"]);

            Console.StdOut.Should().Contain("CACHE => ");
        }

        [TestMethod]
        public async Task PullCommand_Outputs_Statistics()
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data");
            await new PullCommand(dvcCache, httpClient)
                .ExecuteAsync(files);

            Console.StdOut.Should().Contain("Total files: 3, Pulled: 2, Untracked: 1");
        }

        [TestMethod]
        public async Task PullCommand_Outputs_Statistics_OnFailedDownload_NotFound()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, "*").Respond(HttpStatusCode.NotFound);
            var httpClient = new HttpClient(mockHttp);

            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data");
            await new PullCommand(dvcCache, httpClient)
                .ExecuteAsync(files);

            Console.StdOut.Should().Contain("Total files: 3, Pulled: 1, Untracked: 1, Failed: 1");
            Console.StdErr.Should().Contain("Failed to pull");
            Console.StdErr.Should().Contain(": NotFound");
        }

        [TestMethod]
        public async Task PullCommand_Outputs_Statistics_OnFailedDownload_Unauthorized()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, "*").Respond(HttpStatusCode.Unauthorized);
            var httpClient = new HttpClient(mockHttp);

            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data");
            await new PullCommand(dvcCache, httpClient)
                .ExecuteAsync(files);

            Console.StdOut.Should().Contain("Total files: 3, Pulled: 1, Untracked: 1, Failed: 1");
            Console.StdErr.Should().Contain("Failed to pull");
            Console.StdErr.Should().Contain(": Unauthorized");
        }
    }
}
