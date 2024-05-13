using System;
using System.Threading.Tasks;
using static qdvc.Infrastructure.IOContext;

namespace qdvc
{
    internal class DvcFileUtils
    {
        public static async Task<string?> ReadHashFromDvcFile(string dvcFilePath)
        {
            try
            {
                var dvcFileContent = await FileSystem.File.ReadAllTextAsync(dvcFilePath);
                return ReadHashFromDvcFileContent(dvcFileContent);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string? ReadHashFromDvcFileContent(string dvcFileContent)
        {
            const string marker = "md5: ";
            var index = dvcFileContent.IndexOf(marker) + marker.Length;
            return dvcFileContent.Substring(index, 32);
        }
    }
}
