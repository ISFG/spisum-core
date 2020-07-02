using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace ISFG.Common.Utils
{
    public static class Hashes
    {
        #region Static Methods

        public static string Sha256CheckSum(MemoryStream stream)
        {
            using SHA256 sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(stream);

            return hashBytes.Aggregate("", (current, b) => current + b.ToString("x2"));
        }

        #endregion
    }
}