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
    public class CleanCommandTests : CommandTests
    {
        private readonly MockFileSystem fileSystem;

        public CleanCommandTests()
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
        }

        [TestMethod]
        public async Task Clean_DoesNothing_WhenDataFileDoesNotExist()
        {
            var file = @"C:\work\MyRepo\Data\file_tracked_cached.txt";
            fileSystem.File.Delete(file);
            fileSystem.File.Exists(file).Should().BeFalse();

            await new CleanCommand().ExecuteAsync([file]);

            fileSystem.File.Exists(file).Should().BeFalse();
        }

        [TestMethod]
        public async Task Clean_Removes_TrackedUpToDateFiles()
        {
            var file = @"C:\work\MyRepo\Data\file_tracked_cached.txt";
            fileSystem.File.Exists(file).Should().BeTrue();

            await new CleanCommand().ExecuteAsync([file]);

            fileSystem.File.Exists(file).Should().BeFalse();
        }

        [TestMethod]
        public async Task Clean_DoesNotRemove_TrackedModifiedFiles()
        {
            var file = @"C:\work\MyRepo\Data\file_tracked_modified.txt";
            fileSystem.File.Exists(file).Should().BeTrue();

            await new CleanCommand().ExecuteAsync([file]);

            fileSystem.File.Exists(file).Should().BeTrue();
            Console.StdOut.Should().Contain($"Skip (modified): {file}");
        }

        [TestMethod]
        public async Task Clean_DoesNotRemove_UntrackedFiles()
        {
            var file = @"C:\work\MyRepo\Data\untracked-file.txt";
            fileSystem.File.Exists(file).Should().BeTrue();

            await new CleanCommand().ExecuteAsync([file]);

            fileSystem.File.Exists(file).Should().BeTrue();
            Console.StdOut.Should().NotContain($"Untracked:");
        }

        [TestMethod]
        public async Task CleanForce_Removes_TrackedModifiedFiles()
        {
            var file = @"C:\work\MyRepo\Data\file_tracked_modified.txt";
            fileSystem.File.Exists(file).Should().BeTrue();

            await new CleanCommand(force: true).ExecuteAsync([file]);

            fileSystem.File.Exists(file).Should().BeFalse();
            Console.StdOut.Should().Contain($"Removed: {file}");
        }

        [TestMethod]
        public async Task Clean_Output()
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data");
            await new CleanCommand().ExecuteAsync(files);

            Console.StdOut.Should().Contain($"Total files: 4, Removed: 2, Skipped (modified): 1");
        }

        [TestMethod]
        public async Task CleanForce_Output()
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data");
            await new CleanCommand(force: true).ExecuteAsync(files);

            Console.StdOut.Should().Contain($"Total files: 4, Removed: 3");
        }

        [TestMethod]
        public async Task CleanForce_Output_WhenAllClean()
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data");
            await new CleanCommand(force: true).ExecuteAsync(files);

            await new CleanCommand().ExecuteAsync(files);

            Console.StdOut.Should().Contain($"Total files: 4, all clean");
        }
    }
}
