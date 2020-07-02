using System;
using System.Text;

namespace ISFG.Common.Extensions
{
    public static class StringExt
    {
        #region Static Methods

        public static string FirstCharToLower(this string input)
        {
            if (!string.IsNullOrEmpty(input) && char.IsUpper(input[0]))
                return char.ToLower(input[0]) + input.Substring(1);
            return input;
        }

        public static string ToStringEmptyIfNull(this string str) => str ?? string.Empty;

        public static string ToBase64(this string str) =>  
            str != null ? Convert.ToBase64String(Encoding.UTF8.GetBytes(str)) : string.Empty;

        public static string CutLength(this string str, int length) => 
            str.Length > length ? str.Substring(0, length) : str;

        #endregion
    }
}