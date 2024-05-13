using System.Diagnostics;
using System.IO;
using qdvc.Infrastructure;

namespace qdvc
{
    public class DvcCache
    {
        public string DvcCacheFolder { get; }
        private string DvcCacheFilesFolder { get; }

        public DvcCache(string dvcCacheFolder)
        {
            DvcCacheFolder = dvcCacheFolder;
            DvcCacheFilesFolder = IOContext.FileSystem.Path.Combine(DvcCacheFolder, "files", "md5");
            EnsureFolderStructureExists();
        }

        private void EnsureFolderStructureExists()
        {
            var CreateDirectoryIfNeeded = (string path) =>
            {
                if (!IOContext.FileSystem.Directory.Exists(path))
                    IOContext.FileSystem.Directory.CreateDirectory(path);
            };

            var sw = Stopwatch.StartNew();
            CreateDirectoryIfNeeded(IOContext.FileSystem.Path.Combine(DvcCacheFolder, "files"));
            CreateDirectoryIfNeeded(DvcCacheFilesFolder);
            for (var i = 0; i < 256; i++)
                CreateDirectoryIfNeeded(IOContext.FileSystem.Path.Combine(DvcCacheFilesFolder, i.ToString("X2")));
        }

        public static DvcCache? CreateFromFolder(string? cacheDir)
        {
            var dvcCacheFolder = cacheDir;
            if (dvcCacheFolder == null)
                return null;

            return new DvcCache(dvcCacheFolder);
        }

        public static DvcCache? CreateFromRepositorySubFolder(string repositorySubPath)
        {
            var dvcCacheFolder = FindDvcCacheFolder(repositorySubPath);
            if (dvcCacheFolder == null)
                return null;

            return new DvcCache(dvcCacheFolder);
        }

        public static string? FindDvcRootForRepositorySubPath(string path)
        {
            string? directory = null;
            if (IOContext.FileSystem.File.Exists(path))
                directory = IOContext.FileSystem.Path.GetDirectoryName(path);
            else if (IOContext.FileSystem.Directory.Exists(path))
                directory = path;

            while (directory != null)
            {
                var dvcFolder = IOContext.FileSystem.Path.Combine(directory, ".dvc");
                if (IOContext.FileSystem.Directory.Exists(dvcFolder))
                    return dvcFolder;

                directory = IOContext.FileSystem.Path.GetDirectoryName(directory);
            }

            return null;
        }

        private static string? FindDvcCacheFolder(string path)
        {
            var dvcFolder = FindDvcRootForRepositorySubPath(path);
            if (dvcFolder == null)
                return null;
            return IOContext.FileSystem.Path.Combine(dvcFolder, "cache");
        }

        public bool ContainsFile(string md5)
        {
            return IOContext.FileSystem.File.Exists(GetCacheFilePath(md5));
        }

        public string GetCacheFilePath(string md5)
        {
            return IOContext.FileSystem.Path.Combine(DvcCacheFilesFolder, md5[..2], md5[2..]);
        }
    }
}
