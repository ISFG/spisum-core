using System;

namespace ISFG.Common.Utils
{
    public static class IdGenerator
    {
        #region Static Methods

        public static string GenerateId()
        {
            return $"{GetTimestamp()}-{ShortGuid()}";
        }

        public static string ShortGuid()
        {
            long i = 1;
            foreach (byte b in Guid.NewGuid().ToByteArray())
                i *= b + 1;

            return $"{i - DateTime.UtcNow.Ticks:x}";
        }

        private static string GetTimestamp() => DateTime.UtcNow.ToString("yyyyMMddHHmmssffff");

        #endregion
    }    
}
