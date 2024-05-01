using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static qdvc.IOContext;

namespace qdvc
{
    public static class FilesEnumerator
    {
        public static IEnumerable<string> EnumerateFilesFromPath(string path)
        {
            if (path.EndsWith(".dvc", StringComparison.OrdinalIgnoreCase))
            {
                if (path.Contains("*") || path.Contains("?"))
                    return FileSystem.Directory.EnumerateFiles(FileSystem.Path.GetDirectoryName(path) ?? "", FileSystem.Path.GetFileName(path), SearchOption.AllDirectories);
                else
                    return new[] { path };
            }

            path = path.TrimEnd('\\');

            if (path.Contains("*") || path.Contains("?"))
            {
                var dirs = FileSystem.Directory.EnumerateDirectories(FileSystem.Path.GetDirectoryName(path) ?? "", FileSystem.Path.GetFileName(path), SearchOption.AllDirectories).ToList();
                var files = Enumerable.Empty<string>();
                foreach (var dir in dirs)
                    files = files.Concat(EnumerateFilesFromPath(dir));
                return files;
            }
            else
            {
                if (FileSystem.Directory.Exists(path))
                    return FileSystem.Directory.EnumerateFiles(path, "*.dvc", SearchOption.AllDirectories);
            }

            return Enumerable.Empty<string>();
        }
    }
}
