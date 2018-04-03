using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Arts.Web.Core
{
    /// <summary>
    /// Provides various cryptographic extension methods.
    /// </summary>
    public static class CryptoExtensions
    {
        /// <summary>
        /// Computes the hash value for the specified file path.
        /// </summary>
        /// <param name="path">The input file path to compute the hash code for.</param>
        /// <returns></returns>
        public static string ComputeHash(string path)
        {
            return ComputeHash(File.ReadAllBytes(path));
        }

        /// <summary>
        /// Computes the hash value for the specified stream.
        /// </summary>
        /// <param name="stream">The input stream to compute the hash code for.</param>
        /// <returns></returns>
        public static string ComputeHash(this Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                if (stream.CanRead && stream.Position > 0L)
                {
                    stream.Seek(0L, SeekOrigin.Begin);
                }
                stream.CopyTo(ms);
                return ComputeHash(ms.ToArray());
            }
        }

        /// <summary>
        /// Computes the hash value for the specified byte array.
        /// </summary>
        /// <param name="data">The input to compute the hash code for.</param>
        /// <returns></returns>
        public static string ComputeHash(this byte[] data)
        {
            using (var sha256 = SHA256.Create())
            { 
                return BitConverter.ToString(sha256.ComputeHash(data)).Replace("-", "");
            }
        }
    }
}
