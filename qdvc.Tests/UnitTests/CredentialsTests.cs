using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using qdvc.Infrastructure;
using qdvc.Utilities;

namespace qdvc.Tests.UnitTests
{
    [TestClass]
    public class CredentialsTests
    {
        [TestMethod]
        public void DetectFrom_CommandLine_ShouldReturnTheGivenCredentials()
        {
            IOContext.Initialize(TestData.FileSystem.CreateNewWithDvcConfigAndConfigLocalFiles());

            var commandLineArgs = CommandLineArguments.Parse("-u andrei -p asdfgh".Split(' '));
            var credentials = Credentials.DetectFrom(commandLineArgs, null);

            credentials.Should().NotBeNull();
            credentials!.Username.Should().Be("andrei");
            credentials.Password.Should().Be("asdfgh");
            credentials.Source.Should().Be("command line arguments");
        }

        [TestMethod]
        public void DetectFrom_DvcConfigFile_ShouldReturnTheCredentialsFromTheDvcConfigFile()
        {
            IOContext.Initialize(TestData.FileSystem.CreateNewWithDvcConfigAndConfigLocalFiles());

            var dvcConfig = DvcConfig.ReadConfigFromFolder(@"C:\work\MyRepo\.dvc");
            var credentials = Credentials.DetectFrom(null, dvcConfig);

            credentials.Should().NotBeNull();
            credentials!.Username.Should().Be("andrew");
            credentials.Password.Should().Be("asdfgh");
            credentials.Source.Should().Be("config");
        }

        [TestMethod]
        public void DetectFrom_DvcConfigFile_ShouldNotCrash_When_ConfigLocalIsMissing()
        {
            IOContext.Initialize(TestData.FileSystem.CreateNewWithDvcConfigFile());

            var dvcConfig = DvcConfig.ReadConfigFromFolder(@"C:\work\MyRepo\.dvc");
            
            FluentActions.Invoking(() => Credentials.DetectFrom(null, dvcConfig)).Should().NotThrow();
        }
    }
}
