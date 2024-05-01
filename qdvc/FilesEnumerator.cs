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
        public static IEnumerable<string> EnumerateDvcFilesFromPath(string path, string fileNamePattern = "*.*")
        {
            var hasWildcards = (string p) => p.Contains('*') || p.Contains('?');

            if (FileSystem.File.Exists(path))
            {
                return new[] { path };
            }

            if (hasWildcards(path))
            {
                if (path.EndsWith("\\"))
                {
                    var parts = path.Split(FileSystem.Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

                    if (hasWildcards(parts.Last()))
                    {
                        var baseDir = FileSystem.Path.Combine(parts.Take(parts.Length - 1).ToArray());
                        var pattern = parts.Last();
                        var dirs = FileSystem.Directory.EnumerateDirectories(baseDir, pattern, SearchOption.AllDirectories).ToList();
                        var files = Enumerable.Empty<string>();
                        foreach (var dir in dirs)
                            files = files.Concat(EnumerateDvcFilesFromPath(dir));
                        return files;
                    }
                    else
                    {
                        Console.WriteLine($"Too many wildcards in path {path}");
                    }
                }
                else
                {
                    return FileSystem.Directory.EnumerateFiles(FileSystem.Path.GetDirectoryName(path) ?? "", FileSystem.Path.GetFileName(path), SearchOption.AllDirectories);
                }
            }
            else
            {
                if (FileSystem.Directory.Exists(path))
                    return FileSystem.Directory.EnumerateFiles(path, fileNamePattern, SearchOption.AllDirectories);
            }

            return Enumerable.Empty<string>();
        }
    }
}
