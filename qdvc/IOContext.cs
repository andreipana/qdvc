using System;
using System.IO.Abstractions;

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