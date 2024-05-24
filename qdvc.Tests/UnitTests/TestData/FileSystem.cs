using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qdvc.Tests.UnitTests.TestData
{
    internal static class FileSystem
    {
        internal static IFileSystem CreateNewWithDvcConfigAndConfigLocalFiles()
        {
            return new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\work\MyRepo\.dvc\config"] =
                    new MockFileData(
                        """
                        [core]
                            remote = MyRepo-artifactory
                        ['remote "MyRepo-artifactory"']
                            url = https://artifactory.com/artifactory/MyRepo
                            auth = basic
                            method = PUT
                            jobs = 4
                        [cache]
                            dir = C:\global\dvc\cache\MyRepo
                        """),
                [@"C:\work\MyRepo\.dvc\config.local"] =
                    new MockFileData(
                        """
                        ['remote "MyRepo-artifactory"']
                            user = andrew
                            password = asdfgh
                        [cache]
                            dir = ..\..\local\MyRepo
                        """),
            });
        }

        internal static IFileSystem CreateNewWithDvcConfigFile()
        {
            return new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\work\MyRepo\.dvc\config"] =
                    new MockFileData(
                        """
                        [core]
                        remote = MyRepo-artifactory
                        ['remote "MyRepo-artifactory"']
                        url = https: //artifactory.com/artifactory/MyRepo
                        auth = basic
                        method = PUT
                        jobs = 4
                        [cache]
                        dir = C:\global\dvc\cache\MyRepo
                        """)
            });
        }
    }
}
