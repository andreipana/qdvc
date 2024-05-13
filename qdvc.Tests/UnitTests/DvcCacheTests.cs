using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using qdvc.Infrastructure;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;

namespace qdvc.Tests.UnitTests
{
    [TestClass]
    public class DvcCacheTests
    {
        [TestInitialize()]
        public void TestInitialize()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\work\MyRepo\.dvc\cache"] = new MockDirectoryData(),
                [@"C:\work\MyRepo\.dvc\cache\files\md5\08\ceb558030fd2f1c89a017ab6b84fb1"] = new MockFileData("A simple file with some text inside"),
                [@"C:\work\MyRepo\Data\Assets"] = new MockDirectoryData(),
                [@"C:\work\MyRepo\Data\Assets\datafile.txt.dvc"] = new MockFileData(""),
                [@"C:\some\other\folder"] = new MockDirectoryData(),
            });

            IOContext.Initialize(fileSystem);
        }

        [TestMethod]
        public void Constructor_CreatesAllNecessarySubfolders()
        {
            IOContext.FileSystem.Directory.Exists(@"C:\inexistent\cache\folder\files\md5").Should().BeFalse();

            var dvcCache = new DvcCache(@"C:\inexistent\cache\folder\");

            IOContext.FileSystem.Directory.Exists(@"C:\inexistent\cache\folder\files\md5").Should().BeTrue();
        }

        [TestMethod]
        public void FindDvcRootForRepositorySubPath_ReturnsTheCacheFolder_ForValidRepository()
        {
            var dvcFolder = DvcCache.FindDvcRootForRepositorySubPath(@"C:\work\MyRepo\Data\Assets");
            dvcFolder.Should().Be(@"C:\work\MyRepo\.dvc");
        }

        [TestMethod]
        public void FindDvcRootForRepositorySubPath_ReturnsTheCacheFolder_ForFileInsideValidRepository()
        {
            var dvcFolder = DvcCache.FindDvcRootForRepositorySubPath(@"C:\work\MyRepo\Data\Assets\datafile.txt.dvc");
            dvcFolder.Should().Be(@"C:\work\MyRepo\.dvc");
        }

        [TestMethod]
        public void FindDvcRootForRepositorySubPath_ReturnsNull_ForFolderOutsideRepository()
        {
            var dvcFolder = DvcCache.FindDvcRootForRepositorySubPath(@"C:\work\");
            dvcFolder.Should().BeNull();
        }        

        [TestMethod]
        public void CreateFromFolder_ReturnsNull_WhenFolderIsNull()
        {
            var dvcCache = DvcCache.CreateFromFolder(null);
            dvcCache.Should().BeNull();
        }

        [TestMethod]
        public void CreateFromFolder_Returns_DvcCacheInThatFolder()
        {
            var dvcCache = DvcCache.CreateFromFolder(@"C:\global\MyRepo\cache");

            dvcCache.Should().NotBeNull();
            dvcCache!.DvcCacheFolder.Should().Be(@"C:\global\MyRepo\cache");
            IOContext.FileSystem.Directory.Exists(@"C:\global\MyRepo\cache\files\md5").Should().BeTrue();
        }

        [TestMethod]
        public void CreateFromRepositorySubFolder_Retuns_DvcCacheInDefaultDvcCacheLocation_ForSubfolder()
        {
            var dvcCache = DvcCache.CreateFromRepositorySubFolder(@"C:\work\MyRepo\Data\Assets");

            dvcCache.Should().NotBeNull();
            dvcCache!.DvcCacheFolder.Should().Be(@"C:\work\MyRepo\.dvc\cache");
            IOContext.FileSystem.Directory.Exists(@"C:\work\MyRepo\.dvc\cache\files\md5").Should().BeTrue();
        }

        [TestMethod]
        public void CreateFromRepositorySubFolder_Retuns_DvcCacheInDefaultDvcCacheLocation_ForRepositorysRootFolder()
        {
            var dvcCache = DvcCache.CreateFromRepositorySubFolder(@"C:\work\MyRepo");

            dvcCache.Should().NotBeNull();
            dvcCache!.DvcCacheFolder.Should().Be(@"C:\work\MyRepo\.dvc\cache");
            IOContext.FileSystem.Directory.Exists(@"C:\work\MyRepo\.dvc\cache\files\md5").Should().BeTrue();
        }

        [TestMethod]
        public void CreateFromRepositorySubFolder_ReturnsNull_ForForderThatDoesNotExist()
        {
            var dvcCache = DvcCache.CreateFromRepositorySubFolder(@"C:\inexistent\folder");
            dvcCache.Should().BeNull();
        }

        [TestMethod]
        public void CreateFromRepositorySubFolder_ReturnsNull_ForForderThatIsNotADvcRepository()
        {
            var dvcCache = DvcCache.CreateFromRepositorySubFolder(@"C:\some\other");
            dvcCache.Should().BeNull();
        }

        [TestMethod]
        public void GetCacheFilePath_Returns_FilePathInsideDvcCacheFolder()
        {
            var dvcCache = DvcCache.CreateFromRepositorySubFolder(@"C:\work\MyRepo");

            var path = dvcCache!.GetCacheFilePath("08ceb558030fd2f1c89a017ab6b84fb1");

            path.Should().Be(@"C:\work\MyRepo\.dvc\cache\files\md5\08\ceb558030fd2f1c89a017ab6b84fb1");
        }

        [TestMethod]
        public void ContainsFile_ReturnsTrue_WhenCacheContainsFileWithGivenMD5()
        {
            var dvcCache = DvcCache.CreateFromRepositorySubFolder(@"C:\work\MyRepo");

            dvcCache!.ContainsFile("08ceb558030fd2f1c89a017ab6b84fb1").Should().BeTrue();
        }

        [TestMethod]
        public void ContainsFile_ReturnsFalse_WhenCacheDoesNotContainFileWithGivenMD5()
        {
            var dvcCache = DvcCache.CreateFromRepositorySubFolder(@"C:\work\MyRepo");

            dvcCache!.ContainsFile("d6814fc69befc5d736058c0f05568bc4").Should().BeFalse();
        }

    }
}