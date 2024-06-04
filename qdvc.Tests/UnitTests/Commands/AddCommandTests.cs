using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using qdvc.Commands;
using qdvc.Infrastructure;
using qdvc.Tests.TestInfrastructure;
using qdvc.Utilities;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace qdvc.Tests.UnitTests.Commands
{
    [TestClass]
    public class AddCommandTests : CommandTests
    {
        private readonly DvcCache dvcCache;
        private readonly IFileSystem fileSystem;

        public AddCommandTests()
        {
            fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\work\MyRepo\Data\file.txt"] = "The quick brown fox jumps over the lazy dog.",
            });

            IOContext.Initialize(fileSystem);

            dvcCache = new DvcCache(@"C:\work\MyRepo\.dvc\cache\");
        }

        [TestMethod]
        public async Task Execute_Should_Create_TheDvcFileForTheGivenFile()
        {
            await new AddCommand(dvcCache).ExecuteAsync(new[] { @"C:\work\MyRepo\Data\file.txt" });

            fileSystem.File.Exists(@"C:\work\MyRepo\Data\file.txt.dvc").Should().BeTrue();

            var dvcFileContent = fileSystem.File.ReadAllText(@"C:\work\MyRepo\Data\file.txt.dvc");
            dvcFileContent.Should().Be(
                """
                outs:
                - md5: e4d909c290d0fb1ca068ffaddf22cbd0
                  size: 44
                  hash: md5
                  path: file.txt

                """);
        }

        [TestMethod]
        public async Task Execute_Should_Copy_TheGivenFileToTheCache()
        {
            await new AddCommand(dvcCache).ExecuteAsync(new[] { @"C:\work\MyRepo\Data\file.txt" });

            fileSystem.File.Exists(@"C:\work\MyRepo\.dvc\cache\files\md5\e4\d909c290d0fb1ca068ffaddf22cbd0").Should().BeTrue();
        }

        [TestMethod]
        public async Task Execute_Should_ReaddExistingFile_ThatHasChanged()
        {
            await new AddCommand(dvcCache).ExecuteAsync(new[] { @"C:\work\MyRepo\Data\file.txt" });
            fileSystem.File.WriteAllText(@"C:\work\MyRepo\Data\file.txt", "New file content");

            await new AddCommand(dvcCache).ExecuteAsync(new[] { @"C:\work\MyRepo\Data\file.txt" });

            var dvcFileContent = fileSystem.File.ReadAllText(@"C:\work\MyRepo\Data\file.txt.dvc");
            dvcFileContent.Should().Be(
                """
                outs:
                - md5: 3d65efc8ca18c21f7beb78e47ae94206
                  size: 16
                  hash: md5
                  path: file.txt

                """);
            fileSystem.File.Exists(@"C:\work\MyRepo\.dvc\cache\files\md5\3d\65efc8ca18c21f7beb78e47ae94206").Should().BeTrue();
            fileSystem.File.ReadAllText(@"C:\work\MyRepo\.dvc\cache\files\md5\3d\65efc8ca18c21f7beb78e47ae94206").Should().Be("New file content");
        }

        [TestMethod]
        public async Task Execute_Should_NotReaddExistingFile_ThatHasNotChanged()
        {
            await new AddCommand(dvcCache).ExecuteAsync(new[] { @"C:\work\MyRepo\Data\file.txt" });

            await new AddCommand(dvcCache).ExecuteAsync(new[] { @"C:\work\MyRepo\Data\file.txt" });

            var dvcFileContent = fileSystem.File.ReadAllText(@"C:\work\MyRepo\Data\file.txt.dvc");
            dvcFileContent.Should().Be(
                """
                outs:
                - md5: e4d909c290d0fb1ca068ffaddf22cbd0
                  size: 44
                  hash: md5
                  path: file.txt

                """);
            fileSystem.File.Exists(@"C:\work\MyRepo\.dvc\cache\files\md5\e4\d909c290d0fb1ca068ffaddf22cbd0").Should().BeTrue();
            fileSystem.File.ReadAllText(@"C:\work\MyRepo\.dvc\cache\files\md5\e4\d909c290d0fb1ca068ffaddf22cbd0").Should().Be("The quick brown fox jumps over the lazy dog.");
        }

        [TestMethod]
        public async Task Execute_ShowStats_ForAllAddedFilesInFolder()
        {
            fileSystem.File.WriteAllText(@"C:\work\MyRepo\Data\tracked-not-cached.txt", "Code is poetry");

            fileSystem.File.WriteAllText(@"C:\work\MyRepo\Data\up-to-date.txt", "Code is poetry");
            await WriteDvcFileFor(@"C:\work\MyRepo\Data\up-to-date.txt");

            fileSystem.File.WriteAllText(@"C:\work\MyRepo\Data\tracked-cached-modified.txt", "Code is poetry");
            await WriteDvcFileFor(@"C:\work\MyRepo\Data\tracked-cached-modified.txt");
            fileSystem.File.WriteAllText(@"C:\work\MyRepo\Data\tracked-cached-modified.txt", "Code is poetry - Modified");

            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data");
            await new AddCommand(dvcCache).ExecuteAsync(files);

            Console.StdOut.ToString().Should().Contain(@"Added C:\work\MyRepo\Data\tracked-not-cached.txt (Cached)");
            Console.StdOut.ToString().Should().Contain(@"Added C:\work\MyRepo\Data\file.txt (Cached)");
            Console.StdOut.ToString().Should().Contain(@"Re-added C:\work\MyRepo\Data\tracked-cached-modified.txt (Cached)");
            Console.StdOut.ToString().Should().Contain(@"Total files: 4, Added: 2, Re-added: 1, Up-to-date: 1");
        }

        private async Task WriteDvcFileFor(string file)
        {
            var md5 = await Hashing.ComputeMD5HashForFileAsync(file);
            var dvcFilePath = $"{file}.dvc";
            var fi = fileSystem.FileInfo.New(file);

            fileSystem.File.WriteAllText(dvcFilePath,
                $"""
                outs:
                - md5: {md5}
                  size: {fi.Length}
                  hash: md5
                  path: {fi.Name}

                """);
        }
    }
}
