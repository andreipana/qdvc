using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;

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

                [@"c:\work\MyRepo\Data\Assets\Images64\edit.png"] = new MockFileData(""),
                [@"c:\work\MyRepo\Data\Assets\Images64\new.png"] = new MockFileData(""),
                [@"c:\work\MyRepo\Data\Assets\Images64\new.bmp"] = new MockFileData(""),

                [@"c:\work\MyRepo\Data\Assets\Strings\edit.txt.dvc"] = new MockFileData(""),
                [@"c:\work\MyRepo\Data\Assets\Strings\add.txt.dvc"] = new MockFileData(""),

                [@"c:\work\MyRepo\Data\Old\Images32\v2\new.bmp.dvc"] = new MockFileData(""),
                [@"c:\work\MyRepo\Data\Old\Images32\v2\add.bmp.dvc"] = new MockFileData(""),
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
        public void EnumerateFilesFromPath_ShouldSupportWildcards_InFileName2()
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data\Assets\Images64\*.png");

            files.Should().BeEquivalentTo(
            [
                @"c:\work\MyRepo\Data\Assets\Images64\edit.png",
                @"c:\work\MyRepo\Data\Assets\Images64\new.png",
            ]);
        }

        [TestMethod]
        public void EnumerateFilesFromPath_ShouldSupportWildcards_InFileName3()
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data\Assets\Images64\*png*");

            files.Should().BeEquivalentTo(
            [
                @"c:\work\MyRepo\Data\Assets\Images64\edit.png.dvc",
                @"c:\work\MyRepo\Data\Assets\Images64\new.png.dvc",
                @"c:\work\MyRepo\Data\Assets\Images64\edit.png",
                @"c:\work\MyRepo\Data\Assets\Images64\new.png",
            ]);
        }

        [TestMethod]
        public void EnumerateFilesFromPath_ShouldSupportWildcards_InFileName4()
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data\Assets\Images64\*new*.dvc");

            files.Should().BeEquivalentTo(
            [
                @"c:\work\MyRepo\Data\Assets\Images64\new.png.dvc",
                @"c:\work\MyRepo\Data\Assets\Images64\new.bmp.dvc",
            ]);
        }

        [TestMethod]
        [DataRow(@"C:\work\MyRepo\Data\Assets\Images*\")]
        public void EnumerateFilesFromPath_ShouldSupportWildcards_InFirstParentFolderName(string path)
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(path);

            files.Should().BeEquivalentTo(
            [
                @"c:\work\MyRepo\Data\Assets\Images32\edit.png.dvc",
                @"c:\work\MyRepo\Data\Assets\Images64\edit.png.dvc",
                @"c:\work\MyRepo\Data\Assets\Images64\new.png.dvc",
                @"c:\work\MyRepo\Data\Assets\Images64\new.bmp.dvc",
                @"c:\work\MyRepo\Data\Assets\Images64\edit.png",
                @"c:\work\MyRepo\Data\Assets\Images64\new.png",
                @"c:\work\MyRepo\Data\Assets\Images64\new.bmp",
            ]);
        }

        [TestMethod]
        public void EnumerateFilesFromPath_ShouldSupportWildcards_InParentFolderName()
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data\*\Images32");

            files.Should().BeEquivalentTo(
            [
                @"c:\work\MyRepo\Data\Assets\Images32\edit.png.dvc",
                @"c:\work\MyRepo\Data\Old\Images32\v2\new.bmp.dvc",
                @"c:\work\MyRepo\Data\Old\Images32\v2\add.bmp.dvc",
            ]);
        }

        [TestMethod]
        public void EnumerateFilesFromPath_ShouldSupportWildcards_InFolderAndFileName()
        {
            var files = FilesEnumerator.EnumerateFilesFromPath(@"C:\work\MyRepo\Data\Assets\*Images*\*.png.dvc");

            files.Should().BeEquivalentTo(
            [
                @"c:\work\MyRepo\Data\Assets\Images32\edit.png.dvc",
                @"c:\work\MyRepo\Data\Assets\Images64\edit.png.dvc",
                @"c:\work\MyRepo\Data\Assets\Images64\new.png.dvc",
            ]);
        }
    }
}
