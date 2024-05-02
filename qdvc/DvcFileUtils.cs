using System;
using System.IO;
using System.Threading.Tasks;

namespace qdvc
{
    internal class DvcFileUtils
    {
        public static async Task<string?> ReadHashFromDvcFile(string dvcFilePath)
        {
            try
            {
                var dvcFileContent = await File.ReadAllTextAsync(dvcFilePath);
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
