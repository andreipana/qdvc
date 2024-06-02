using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using qdvc.Commands;
using qdvc.Infrastructure;
using qdvc.Tests.TestInfrastructure;
using qdvc.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qdvc.Tests.UnitTests.Commands
{
    [TestClass]
    public class StatusCommandTests : CommandTests
    {
        private readonly MockFileSystem fileSystem;
        private readonly DvcCache dvcCache;

        public StatusCommandTests()
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

                //Missing, cached file
                [@"C:\work\MyRepo\Data\file_missing_cached.txt.dvc"] = new MockFileData(
                    """
                    outs:
                    - md5: 8b5dc2bafbe03346676bd13095d02cec
                    size: 11
                    hash: md5
                    path: file_missing_cached.txt
                        
                    """),
                [@"C:\work\MyRepo\.dvc\cache\files\md5\8b\5dc2bafbe03346676bd13095d02cec"] = new MockFileData("Cached file"),
            });

            IOContext.Initialize(fileSystem);

            dvcCache = new DvcCache(@"C:\work\MyRepo\.dvc\cache\");
        }

        [TestMethod]
        public async Task Outputs_Uptodate_ForFilesWhichAreUptodate()
        {
            await new StatusCommand(dvcCache, null).ExecuteAsync(new[] { @"C:\work\MyRepo\Data\file_tracked_cached.txt" });

            Console.StdOut.Should().NotContain(@"Data\file-tracked-cached.txt");

            Console.StdOut.Should().Contain(@"Total files: 1, Everything is up to date");
        }

        [TestMethod]
        public async Task Outputs_Nothing_ForUntrackedFile()
        {
            await new StatusCommand(dvcCache, null).ExecuteAsync(new[] { @"C:\work\MyRepo\Data\untracked-file.txt" });

            Console.StdOut.Should().NotContain(@"Data\untracked-file.txt");

            Console.StdOut.Should().Contain(@"Total files: 1, Untracked: 1");
        }

        [TestMethod]
        public async Task Outputs_Nothing_ForFileThat_Matches_AssociatedDvcFile_AndIsCached()
        {
            await new StatusCommand(dvcCache, null).ExecuteAsync(new[] { @"C:\work\MyRepo\Data\file_tracked_cached.txt" });
            
            Console.StdOut.Should().NotContain(@"Data\file_tracked_cached.txt");

            Console.StdOut.Should().Contain(@"Total files: 1, Everything is up to date");
        }

        [TestMethod]
        public async Task Outputs_Modified_ForFileThat_DoesNotMatch_AssociatedDvcFile()
        {
            await new StatusCommand(dvcCache, null).ExecuteAsync(new[] { @"C:\work\MyRepo\Data\file_tracked_modified.txt" });

            Console.StdOut.Should().Contain(@"Modified: C:\work\MyRepo\Data\file_tracked_modified.txt");

            Console.StdOut.Should().Contain(@"Total files: 1, Modified: 1");
            Console.StdOut.Should().NotContain("Untracked: ");
            Console.StdOut.Should().NotContain("Not in cache: ");
        }

        [TestMethod]
        public async Task Outputs_NotInCache_ForTrackedFile_ThatIsNotInCache()
        {
            await new StatusCommand(dvcCache, null).ExecuteAsync(new[] { @"C:\work\MyRepo\Data\file_tracked_not-cached.txt" });

            Console.StdOut.Should().Contain(@"Not in cache: C:\work\MyRepo\Data\file_tracked_not-cached.txt");

            Console.StdOut.Should().Contain(@"Total files: 1, Not in cache: 1");
            Console.StdOut.Should().NotContain("Untracked: ");
            Console.StdOut.Should().NotContain("Modified: ");
        }

        [TestMethod]
        public async Task Outputs_Status_OfAllFilesInTheFolder()
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data\");
            await new StatusCommand(dvcCache, null).ExecuteAsync(files);

            Console.StdOut.Should().Contain(@"Not in cache: C:\work\MyRepo\Data\file_tracked_not-cached.txt");
            Console.StdOut.Should().Contain(@"Modified: C:\work\MyRepo\Data\file_tracked_modified.txt");
            
            Console.StdOut.Should().Contain(@"Total files: 5");
            Console.StdOut.Should().Contain(@"Up to date: 1");
            Console.StdOut.Should().Contain(@"Untracked: 1");
            Console.StdOut.Should().Contain(@"Modified: 1");
            Console.StdOut.Should().Contain(@"Not in cache: 1");
            Console.StdOut.Should().Contain(@"Missing: 1");
        }

        [TestMethod]
        public async Task Outputs_Missing_ForMissingTrackedFile()
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data\");
            await new StatusCommand(dvcCache, null).ExecuteAsync(files);

            Console.StdOut.Should().Contain(@"Missing: C:\work\MyRepo\Data\file_missing_cached.txt");
        }
    }
}
