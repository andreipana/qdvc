using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qdvc.Tests.UnitTests
{
    [TestClass]
    public class AddCommandTests
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
    }
}
