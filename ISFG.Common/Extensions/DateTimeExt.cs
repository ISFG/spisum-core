using System;
using System.Globalization;

namespace ISFG.Common.Extensions
{
    public static class DateTimeExt
    {
        #region Static Methods

        // Format ISO 8601 - that is used by Alfresco
        public static string ToAlfrescoDateTimeString(this DateTime input)
        {
            return input.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture);
        }

        public static bool IsBetween(this DateTime dt, DateTime start, DateTime end) => 
            dt >= start && dt <= end;

        #endregion
    }
}
