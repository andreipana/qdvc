using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qdvc
{
    public static class IOContext
    {
        private static IFileSystem? _FileSystem;

        public static IFileSystem FileSystem => _FileSystem ?? throw new InvalidOperationException("IOContext is not initialized.");


        public static void Initialize()
        {
            _FileSystem = new FileSystem();
        }

        public static void Initialize(IFileSystem fileSystem)
        {
            _FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }
    }
}
