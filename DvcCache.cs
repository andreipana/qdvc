using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qdvc
{
    internal class DvcCache
    {
        public string DvcCacheFolder { get; }
        private string DvcCacheFilesFolder { get; }

        public DvcCache(string dvcCacheFolder)
        {
            DvcCacheFolder = dvcCacheFolder;
            DvcCacheFilesFolder = Path.Combine(DvcCacheFolder, "files", "md5");
            EnsureFolderStructureExists();
        }

        private void EnsureFolderStructureExists()
        {
            var CreateDirectoryIfNeeded = (string path) =>
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            };

            var sw = Stopwatch.StartNew();
            CreateDirectoryIfNeeded(Path.Combine(DvcCacheFolder, "files"));
            CreateDirectoryIfNeeded(DvcCacheFilesFolder);
            for (var i = 0; i < 256; i++)
                CreateDirectoryIfNeeded(Path.Combine(DvcCacheFilesFolder, i.ToString("X2")));
            Console.WriteLine($"Ensuring folder structure took {sw.Elapsed}");
        }

        internal static DvcCache? GetDvcCacheForFolder(string path)
        {
            var dvcCacheFolder = FindDvcCacheFolder(path);
            if (dvcCacheFolder == null)
                return null;

            return new DvcCache(dvcCacheFolder);
        }

        private static string? FindDvcCacheFolder(string path)
        {
            string? directory = null;
            if (File.Exists(path))
                directory = Path.GetDirectoryName(path);
            else if (Directory.Exists(path))
                directory = path;

            while (directory != null)
            {
                var dvcFolder = Path.Combine(directory, ".dvc");
                if (Directory.Exists(dvcFolder))
                    return Path.Combine(dvcFolder, "cache");

                directory = Path.GetDirectoryName(directory);
            }

            return null;
        }

        public bool ContainsFile(string md5)
        {
            return File.Exists(GetCacheFilePath(md5));
        }

        public string GetCacheFilePath(string md5)
        {
            return Path.Combine(DvcCacheFilesFolder, md5.Substring(0, 2), md5.Substring(2));
        }
    }
}
