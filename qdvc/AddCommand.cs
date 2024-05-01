using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static qdvc.IOContext;

namespace qdvc
{
    public class AddCommand
    {
        public AddCommand(DvcCache? dvcCache)
        {
            DvcCache = dvcCache;
        }

        public DvcCache? DvcCache { get; }

        public async Task ExecuteAsync(IEnumerable<string> files)
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = -1
            };

            var filesWithoutDvcFiles = files.Where(f => !f.EndsWith(".dvc", StringComparison.OrdinalIgnoreCase));

            await Parallel.ForEachAsync(filesWithoutDvcFiles, options, async (file, _) =>
            {
                await AddFileAsync(file);
            });
        }

        private async Task AddFileAsync(string file)
        {
            if (!FileSystem.File.Exists(file))
            {
                Console.WriteLine($"File {file} does not exist.");
                return;
            }

            var dvcFilePath = $"{file}.dvc";
            
            if (FileSystem.File.Exists(dvcFilePath))
            {
                Console.WriteLine($"File {dvcFilePath} already exists.");
                return;
            }

            var md5 = await Hashing.ComputeMD5HashForFileAsync(file);
            var fi = FileSystem.FileInfo.New(file);

            FileSystem.File.WriteAllText(dvcFilePath, 
                $"""
                outs:
                - md5: {md5}
                  size: {fi.Length}
                  hash: md5
                  path: {fi.Name}

                """);

            var result = await CopyFileToCacheAsync(file, md5);

            var copyResult = result switch
            {
                CopyToCacheResult.Success => "Cached",
                CopyToCacheResult.AlreadyInCache => "Already in cache",
                CopyToCacheResult.NoCache => "No cache",
                _ => "Failed to cache"
            };

            Console.WriteLine($"Added {file} ({copyResult})");
        }

        private Task<CopyToCacheResult> CopyFileToCacheAsync(string file, string md5)
        {
            var cacheFile = DvcCache?.GetCacheFilePath(md5);
            if (cacheFile == null)
                return Task.FromResult(CopyToCacheResult.NoCache);

            try
            {
                FileSystem.File.Copy(file, cacheFile);
            }
            catch
            {
                if (FileSystem.File.Exists(cacheFile))
                    return Task.FromResult(CopyToCacheResult.AlreadyInCache);
                return Task.FromResult(CopyToCacheResult.Failed);
            }

            return Task.FromResult(CopyToCacheResult.Success);
        }

        private enum CopyToCacheResult
        {
            Success,
            AlreadyInCache,
            NoCache,
            Failed
        }
    }
}
