using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static qdvc.Infrastructure.IOContext;

namespace qdvc.Utilities
{
    public class Hashing
    {
        public static async Task<string> ComputeMD5HashForFileAsync(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = FileSystem.File.OpenRead(filePath))
                {
                    byte[] hashBytes = await md5.ComputeHashAsync(stream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }
        }
    }
}
