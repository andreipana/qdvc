﻿using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static qdvc.IOContext;

namespace qdvc.Tests.UnitTests
{
    [TestClass]
    public class PullCommandTests
    {
        private DvcCache dvcCache;
        private Credentials credentials;
        private HttpClient httpClient;

        public PullCommandTests()
        {
            Initialize(new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\work\MyRepo\.dvc\cache"] = new MockDirectoryData(),
                [@"C:\work\MyRepo\Data\Assets\file.txt.dvc"] = new MockFileData(
                    """
                    outs:
                    - md5: 463002689330bae2f4adf13f4c7d333c
                      size: 14
                      hash: md5
                      path: file.txt
                    
                    """),
            }));

            dvcCache = new DvcCache(@"C:\work\MyRepo\.dvc\cache");
            credentials = new Credentials("andrei", "asdfgh", "hardcoded");

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/46/3002689330bae2f4adf13f4c7d333c")
                    .Respond("application/octet-stream", "Code is poetry");

            httpClient = new HttpClient(mockHttp);
        }

        [TestMethod]
        public async Task PullCommand_Downloads_TheSpecifiedFile()
        {
            var filePath = @"C:\work\MyRepo\Data\Assets\file.txt";

            await new PullCommand(dvcCache, httpClient)
                .ExecuteAsync([$"{filePath}.dvc"]);

            FileSystem.File.ReadAllText(filePath)
                .Should().Be("Code is poetry");
        }
    }
}
