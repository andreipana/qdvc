using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using qdvc.Infrastructure;
using qdvc.Utilities;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;

namespace qdvc.Tests.UnitTests.Utilities
{
    [TestClass]
    public class HashingTests
    {
        [TestMethod]
        public void ComputeFileHashAsync_ComputesTheHash()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\work\MyRepo\Data\file.txt"] = "The quick brown fox jumps over the lazy dog.",
            });

            IOContext.Initialize(fileSystem);

            var hash = Hashing.ComputeMD5HashForFileAsync(@"C:\work\MyRepo\Data\file.txt").Result;
            hash.Should().Be("e4d909c290d0fb1ca068ffaddf22cbd0");
        }
    }
}
