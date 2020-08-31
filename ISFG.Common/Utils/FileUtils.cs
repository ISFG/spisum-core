using System.IO;

namespace ISFG.Common.Utils
{
    public static class FileUtils
    {
        #region Static Methods

        public static bool IsFileEquals(byte[] file1, string path2)
        {
            byte[] file2 = File.ReadAllBytes(path2);            

            return IsFileEquals(file1, file2);
        }

        public static bool IsFileEquals(string path1, string path2)
        {
            byte[] file1 = File.ReadAllBytes(path1);
            byte[] file2 = File.ReadAllBytes(path2);

            return IsFileEquals(file1, file2);
        }

        public static bool IsFileEquals(byte[] file1, byte[] file2)
        {
            if (file1.Length == file2.Length)
            {
                for (int i = 0; i < file1.Length; i++)
                    if (file1[i] != file2[i]) return false;
                return true;
            }
            return false;
        }

        #endregion
    }
}
