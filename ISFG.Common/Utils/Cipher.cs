using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ISFG.Common.Utils
{
    public static class Cipher
    {
        #region Static Methods

        public static string Encrypt(string plainText, string password)
        {
            if (plainText == null)
                return null;

            if (password == null)
                password = string.Empty;

            var bytesToBeEncrypted = Encoding.UTF8.GetBytes(plainText);
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            var bytesEncrypted = Encrypt(bytesToBeEncrypted, passwordBytes);

            return Convert.ToBase64String(bytesEncrypted);
        }

        public static string Decrypt(string encryptedText, string password)
        {
            if (encryptedText == null)
                return null;

            if (password == null)
                password = string.Empty;

            var bytesToBeDecrypted = Convert.FromBase64String(encryptedText);
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            var bytesDecrypted = Decrypt(bytesToBeDecrypted, passwordBytes);

            return Encoding.UTF8.GetString(bytesDecrypted);
        }

        private static byte[] Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {
            byte[] encryptedBytes = null;

            var saltBytes = new byte[] { 11, 34, 78, 12, 65, 107, 19, 39 };

            using (MemoryStream ms = new MemoryStream())
            {
                using RijndaelManaged aes = new RijndaelManaged();
                var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);

                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);

                aes.Mode = CipherMode.CBC;

                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                    cs.Close();
                }

                encryptedBytes = ms.ToArray();
            }

            return encryptedBytes;
        }

        private static byte[] Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] decryptedBytes = null;

            var saltBytes = new byte[] { 11, 34, 78, 12, 65, 107, 19, 39 };

            using (MemoryStream ms = new MemoryStream())
            {
                using RijndaelManaged aes = new RijndaelManaged();
                var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);

                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);
                aes.Mode = CipherMode.CBC;

                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                    cs.Close();
                }

                decryptedBytes = ms.ToArray();
            }

            return decryptedBytes;
        }

        #endregion
    }
}