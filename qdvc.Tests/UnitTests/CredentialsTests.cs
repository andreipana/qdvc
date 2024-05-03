using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace qdvc.Tests.UnitTests
{
    [TestClass]
    public class CredentialsTests
    {
        public CredentialsTests()
        {
            var fileSystem = TestData.FileSystem.CreateNewWithDvcConfigFiles();

            IOContext.Initialize(fileSystem);
        }

        [TestMethod]
        public void DetectFrom_CommandLine_ShouldReturnTheGivenCredentials()
        {
            var commandLineArgs = new CommandLineArguments("-u andrei -p asdfgh".Split(' '));
            var credentials = Credentials.DetectFrom(commandLineArgs, null);

            credentials.Username.Should().Be("andrei");
            credentials.Password.Should().Be("asdfgh");
            credentials.Source.Should().Be("command line arguments");
        }

        [TestMethod]
        public void DetectFrom_DvcConfigFile_ShouldReturnTheCredentialsFromTheDvcConfigFile()
        {
            var dvcConfig = DvcConfig.ReadConfigFromFolder(@"C:\work\MyRepo\.dvc");
            var credentials = Credentials.DetectFrom(null, dvcConfig);

            credentials.Username.Should().Be("andrew");
            credentials.Password.Should().Be("asdfgh");
            credentials.Source.Should().Be("config");
        }
    }
}
