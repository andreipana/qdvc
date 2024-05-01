using FluentAssertions;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace qdvc.Tests.UnitTests
{
    [TestClass]
    public class FilesEnumeratorTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"c:\work\MyRepo\Data\Assets\Images32\edit.png.dvc"] = new MockFileData(""),

                [@"c:\work\MyRepo\Data\Assets\Images64\edit.png.dvc"] = new MockFileData(""),
                [@"c:\work\MyRepo\Data\Assets\Images64\new.png.dvc"] = new MockFileData(""),
                [@"c:\work\MyRepo\Data\Assets\Images64\new.bmp.dvc"] = new MockFileData(""),

                [@"c:\work\MyRepo\Data\Assets\Strings\edit.txt.dvc"] = new MockFileData(""),
                [@"c:\work\MyRepo\Data\Assets\Strings\add.txt.dvc"] = new MockFileData(""),

                [@"c:\work\MyRepo\Data\Old\Images64\v2\new.bmp.dvc"] = new MockFileData(""),
                [@"c:\work\MyRepo\Data\Old\Images64\v2\add.bmp.dvc"] = new MockFileData(""),
            });

            IOContext.Initialize(fileSystem);
        }

        [TestMethod]
        public void EnumerateFilesFromPath_ShouldReturn_AllTheFilesFromTheGivenPath()
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data\Assets\Strings");

            files.Should().BeEquivalentTo(
            [
                @"c:\work\MyRepo\Data\Assets\Strings\edit.txt.dvc",
                @"c:\work\MyRepo\Data\Assets\Strings\add.txt.dvc",
            ]);
        }

        [TestMethod]
        public void EnumerateFilesFromPath_ShouldSupportWildcards_InFileName()
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data\Assets\Images64\*.png.dvc");

            files.Should().BeEquivalentTo(
            [
                @"c:\work\MyRepo\Data\Assets\Images64\edit.png.dvc",
                @"c:\work\MyRepo\Data\Assets\Images64\new.png.dvc",
            ]);
        }

        [TestMethod]
        [DataRow(@"C:\work\MyRepo\Data\Assets\Images*")]
        [DataRow(@"C:\work\MyRepo\Data\Assets\Images*\")]
        public void EnumerateFilesFromPath_ShouldSupportWildcards_InFolderName(string path)
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(path);

            files.Should().BeEquivalentTo(
            [
                @"c:\work\MyRepo\Data\Assets\Images32\edit.png.dvc",
                @"c:\work\MyRepo\Data\Assets\Images64\edit.png.dvc",
                @"c:\work\MyRepo\Data\Assets\Images64\new.png.dvc",
                @"c:\work\MyRepo\Data\Assets\Images64\new.bmp.dvc",
            ]);
        }

        [TestMethod]
        [Ignore("Wildcards in multiple parts of the path are not implemented yet, as they are not supported by the OS functions.")]
        public void EnumerateFilesFromPath_ShouldSupportWildcards_InParentFolderName()
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data\*\Images64");

            files.Should().BeEquivalentTo(
                           [
                @"c:\work\MyRepo\Data\Assets\Images64\edit.png.dvc",
                @"c:\work\MyRepo\Data\Assets\Images64\new.png.dvc",
                @"c:\work\MyRepo\Data\Assets\Images64\new.bmp.dvc",
                @"c:\work\MyRepo\Data\Old\Images64\v2\add.bmp.dvc"
            ]);
        }

        [TestMethod]
        [Ignore("Wildcards in multiple parts of the path are not implemented yet, as they are not supported by the OS functions.")]
        public void EnumerateFilesFromPath_ShouldSupportWildcards_InFolderAndFileName()
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data\Assets\*Images*\*.png.dvc");

            files.Should().BeEquivalentTo(
                           [
                @"c:\work\MyRepo\Data\Assets\Images32\edit.png.dvc",
                @"c:\work\MyRepo\Data\Assets\Images64\new.png.dvc",
            ]);
        }
    }
}
