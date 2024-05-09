using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qdvc.Tests.UnitTests
{
    [TestClass]
    public class DvcConfigTests
    {
        [TestInitialize()]
        public void TestInitialize()
        {
            var fileSystem = TestData.FileSystem.CreateNewWithDvcConfigFiles();

            IOContext.Initialize(fileSystem);
        }

        [TestMethod]
        public void ReadConfigFromFolder_ReadsProperConfigFiles()
        {
            var dvcConfig = DvcConfig.ReadConfigFromFolder(@"C:\work\MyRepo\.dvc");

            dvcConfig.ProjectConfigFile.Should().Be(@"C:\work\MyRepo\.dvc\config");
            dvcConfig.LocalConfigFile.Should().Be(@"C:\work\MyRepo\.dvc\config.local");
        }

        [TestMethod]
        public void ReadConfigFromFolder_IgnoresInvalidLines()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\work\MyRepo\.dvc\config"] =
                    new MockFileData(
                        """
                        [invalid]
                            line
                        [core]
                            remote = MyRepo-artifactory
                        """)
            });
            IOContext.Initialize(fileSystem);

            var dvcConfig = DvcConfig.ReadConfigFromFolder(@"C:\work\MyRepo\.dvc");
            dvcConfig.GetProperty("invalid.line").Should().BeNull();
            dvcConfig.GetProperty("core.remote")?.Value.Should().Be("MyRepo-artifactory");
        }

        [TestMethod]
        public void ReadConfigFromFolder_ReadsAllPropertiesFrom_ConfigAndConfigLocal()
        {
            var dvcConfig = DvcConfig.ReadConfigFromFolder(@"C:\work\MyRepo\.dvc");

            dvcConfig.GetPropertyCount().Should().Be(8);
            dvcConfig.GetProperty("core.remote")?.Name.Should().Be("core.remote");
            dvcConfig.GetProperty("core.remote")?.Value.Should().Be("MyRepo-artifactory");
            dvcConfig.GetProperty("core.remote")?.Source.Should().Be(DvcConfigPropertySource.Project);
            dvcConfig.GetProperty("'remote \"MyRepo-artifactory\"'.user")?.Name.Should().Be("'remote \"MyRepo-artifactory\"'.user");
            dvcConfig.GetProperty("'remote \"MyRepo-artifactory\"'.user")?.Value.Should().Be("andrew");
            dvcConfig.GetProperty("'remote \"MyRepo-artifactory\"'.user")?.Source.Should().Be(DvcConfigPropertySource.Local);
        }

        [TestMethod]
        public void ReadConfigFromFolder_WhenPropertyExistInConfigLocal_ShouldOverrideTheSamePropertyFromConfig()
        {
            var dvcConfig = DvcConfig.ReadConfigFromFolder(@"C:\work\MyRepo\.dvc");
            dvcConfig.GetProperty("cache.dir")?.Value.Should().Be(@"..\..\local\MyRepo");
            dvcConfig.GetProperty("cache.dir")?.Source.Should().Be(DvcConfigPropertySource.Local);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(@"C:\some\inexistent\folder")]
        [DataRow(@"C:\work\MyRepo")] // not pointing to the .dvc folder
        [DataRow(@"C:\work\MyRepo\.dvc\cache")] // too deep into the .dvc folder
        public void ReadConfigFromFolder_ReturnsEmptyConfig_WhenFolderIsInvalid(string dvcFolder)
        {
            var dvcConfig = DvcConfig.ReadConfigFromFolder(dvcFolder);
            
            dvcConfig.Should().NotBeNull();
            dvcConfig.GetPropertyCount().Should().Be(0);
        }

        [TestMethod]
        public void GetCacheDirAbsolutePath_Returns_AbsoluteCacheDirPath_WhenConfigHasRelativePath()
        {
            var dvcConfig = DvcConfig.ReadConfigFromFolder(@"C:\work\MyRepo\.dvc");
            dvcConfig.GetCacheDirAbsolutePath().Should().Be(@"C:\work\local\MyRepo");
        }

        [TestMethod]
        public void GetCacheDirAbsolutePath_ReturnsNull_WhenConfigHasNoCacheDir()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\work\MyRepo\.dvc\config"] =
                    new MockFileData(
                        """
                        [core]
                            remote = MyRepo-artifactory
                        """)
            });
            IOContext.Initialize(fileSystem);

            var dvcConfig = DvcConfig.ReadConfigFromFolder(@"C:\work\MyRepo\.dvc");
            dvcConfig.GetCacheDirAbsolutePath().Should().BeNull();
        }

        [TestMethod]
        public void GetCacheDirAbsolutePath_ReturnsAbsolutePath_WhenConfigIsMissingAndConfigLocalIsPresent()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\work\MyRepo\.dvc\config.local"] =
                    new MockFileData(
                        """
                        [cache]
                            dir = ..\cache
                        """)
            });
            IOContext.Initialize(fileSystem);

            var dvcConfig = DvcConfig.ReadConfigFromFolder(@"C:\work\MyRepo\.dvc");
            dvcConfig.GetCacheDirAbsolutePath().Should().Be(@"C:\work\MyRepo\cache");
        }

        [TestMethod]
        public void GetCacheDirAbsolutePath_ReturnsAbsolutePath_WhenAbsolutePathIsGivenInConfig()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\work\MyRepo\.dvc\config.local"] =
                    new MockFileData(
                        """
                        [cache]
                            dir = C:\global\cache
                        """)
            });
            IOContext.Initialize(fileSystem);

            var dvcConfig = DvcConfig.ReadConfigFromFolder(@"C:\work\MyRepo\.dvc");
            dvcConfig.GetCacheDirAbsolutePath().Should().Be(@"C:\global\cache");
        }

        [TestMethod]
        public void Properties_ReturnsNull_ForMissingProperty()
        {
            var dvcConfig = DvcConfig.ReadConfigFromFolder(@"C:\work\MyRepo\.dvc");
            dvcConfig.GetProperty("missingProperty").Should().BeNull();
        }
    }
}
