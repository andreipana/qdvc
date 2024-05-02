using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static qdvc.IOContext;

namespace qdvc
{
    public static class FilesEnumerator
    {
        public static IEnumerable<string> EnumerateFilesFromPath(string path)
        {
            char separator = FileSystem.Path.DirectorySeparatorChar;
            string[] parts = path.Split(separator);

            if (HasWildcards(parts[0]))
                throw new ArgumentException("Path root must not have a wildcard", nameof(path));

            return GetAllMatchingPathsInternal(string.Join(separator, parts.Skip(1)), parts[0]);
        }

        private static IEnumerable<string> GetAllMatchingPathsInternal(string pattern, string root)
        {
            char separator = Path.DirectorySeparatorChar;
            string[] parts = pattern.Split(separator);

            for (int i = 0; i < parts.Length; i++)
            {
                // if this part of the path is a wildcard that needs expanding
                if (HasWildcards(parts[i]))
                {
                    // create an absolute path up to the current wildcard and check if it exists
                    var combined = root + separator + string.Join(separator, parts.Take(i));
                    if (!FileSystem.Directory.Exists(combined))
                        return Array.Empty<string>();

                    if (i == parts.Length - 1) // if this is the end of the path (a file name)
                    {
                        return FileSystem.Directory.EnumerateFiles(combined, parts[i], SearchOption.TopDirectoryOnly);
                    }
                    else // if this is in the middle of the path (a directory name)
                    {
                        var directories = FileSystem.Directory.EnumerateDirectories(combined, parts[i], SearchOption.TopDirectoryOnly);
                        var paths = directories.SelectMany(dir =>
                            GetAllMatchingPathsInternal(string.Join(separator, parts.Skip(i + 1)), dir));
                        return paths;
                    }
                }
            }

            // if pattern ends in an absolute path with no wildcards in the filename
            var absolute = root + separator + string.Join(separator, parts);
            if (FileSystem.File.Exists(absolute))
                return new[] { absolute };

            if (FileSystem.Directory.Exists(absolute))
            {
                return FileSystem.Directory.EnumerateFiles(absolute, "*.*", SearchOption.AllDirectories);
            }

            return Array.Empty<string>();
        }

        private static bool HasWildcards(string path) => path.Contains('*') || path.Contains('?');
    }
}
