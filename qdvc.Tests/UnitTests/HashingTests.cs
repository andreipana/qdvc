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
