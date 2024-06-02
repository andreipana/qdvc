using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using qdvc.Commands;
using qdvc.Infrastructure;
using qdvc.Tests.TestInfrastructure;
using qdvc.Utilities;
using RichardSzalay.MockHttp;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace qdvc.Tests.UnitTests.Commands
{
    [TestClass]
    public class StatusRepoCommandTests : CommandTests
    {
        private readonly MockFileSystem fileSystem;
        private readonly DvcCache dvcCache;

        public StatusRepoCommandTests()
        {
            fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                //Untracked file
                [@"C:\work\MyRepo\Data\untracked-file.txt"] = "The quick brown fox jumps over the lazy dog.",

                //Tracked, not-cached file
                [@"C:\work\MyRepo\Data\file_tracked_not-cached.txt"] = "Code is poetry",
                [@"C:\work\MyRepo\Data\file_tracked_not-cached.txt.dvc"] = new MockFileData(
                    """
                    outs:
                    - md5: 463002689330bae2f4adf13f4c7d333c
                      size: 14
                      hash: md5
                      path: file_tracked_not-cached.txt
                    
                    """),

                //Tracked, modified
                [@"C:\work\MyRepo\Data\file_tracked_modified.txt"] = "Code is poetry - Modified",
                [@"C:\work\MyRepo\Data\file_tracked_modified.txt.dvc"] = new MockFileData(
                    """
                    outs:
                    - md5: 463002689330bae2f4adf13f4c7d333c
                      size: 14
                      hash: md5
                      path: file_tracked_modified.txt
                    
                    """),

                //Tracked, cached file
                [@"C:\work\MyRepo\Data\file_tracked_cached.txt"] = "Cached file",
                [@"C:\work\MyRepo\Data\file_tracked_cached.txt.dvc"] = new MockFileData(
                    """
                    outs:
                    - md5: 8b5dc2bafbe03346676bd13095d02cec
                      size: 11
                      hash: md5
                      path: file_tracked_cached.txt
                    
                    """),
                [@"C:\work\MyRepo\.dvc\cache\files\md5\8b\5dc2bafbe03346676bd13095d02cec"] = new MockFileData("Cached file"),

            });

            IOContext.Initialize(fileSystem);

            dvcCache = new DvcCache(@"C:\work\MyRepo\.dvc\cache\");
        }

        [TestMethod]
        public async Task Outputs_Untracked_ForFileWhich_IsNotTracked()
        {
            var mockHttp = new MockHttpMessageHandler();
            var httpClient = new HttpClient(mockHttp);

            await new StatusRepoCommand(dvcCache, httpClient).ExecuteAsync([@"C:\work\MyRepo\Data\untracked-file.txt"]);

            Console.StdOut.Should().Contain(@"Untracked: C:\work\MyRepo\Data\untracked-file.txt");
        }

        [TestMethod]
        public async Task Outputs_NotPushed_ForFileWhich_IsInCache_But_NotOnTheRemote()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/8b/5dc2bafbe03346676bd13095d02cec")
                    .Respond(HttpStatusCode.NotFound);
            var httpClient = new HttpClient(mockHttp);

            await new StatusRepoCommand(dvcCache, httpClient).ExecuteAsync([@"C:\work\MyRepo\Data\file_tracked_cached.txt"]);

            Console.StdOut.Should().Contain(@"Not pushed: C:\work\MyRepo\Data\file_tracked_cached.txt");
        }

        [TestMethod]
        public async Task Outputs_NotInCache_ForFileWhich_IsNotInCache_But_OnTheRemote()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/46/3002689330bae2f4adf13f4c7d333c")
                    .Respond("application/octet-stream", "Code is poetry");
            var httpClient = new HttpClient(mockHttp);

            await new StatusRepoCommand(dvcCache, httpClient).ExecuteAsync([@"C:\work\MyRepo\Data\file_tracked_not-cached.txt"]);

            Console.StdOut.Should().Contain(@"Not cached: C:\work\MyRepo\Data\file_tracked_not-cached.txt");
        }

        [TestMethod]
        public async Task Outputs_Nothing_ForUpToDateFiles()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/8b/5dc2bafbe03346676bd13095d02cec")
                    .Respond("application/octet-stream", "Cached file");
            var httpClient = new HttpClient(mockHttp);

            await new StatusRepoCommand(dvcCache, httpClient).ExecuteAsync([@"C:\work\MyRepo\Data\file_tracked_cached.txt"]);

            Console.StdOut.Should().Contain(@"Up-to-date: C:\work\MyRepo\Data\file_tracked_cached.txt");
        }

        [TestMethod]
        public async Task Outputs_Status_OfAllProvidedFiles()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/8b/5dc2bafbe03346676bd13095d02cec")
                    .Respond("application/octet-stream", "Cached file");
            mockHttp.When($"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/*")
                    .Respond(HttpStatusCode.NotFound);
            var httpClient = new HttpClient(mockHttp);

            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data\");
            await new StatusRepoCommand(dvcCache, httpClient).ExecuteAsync(files);

            Console.StdOut.Should().Contain(@"Total files: 3");
            Console.StdOut.Should().Contain(@"Up to date: 1");
            Console.StdOut.Should().Contain(@"Not pushed: 2");
        }
    }
}
