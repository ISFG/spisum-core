namespace ISFG.Common.Extensions
{
    public static class LongExt
    {
        #region Static Methods

        public static double? ConvertBytesToMegabytes(this long bytes) =>
            bytes / 1024f / 1024f;

        public static double? ConvertBytesToKilobytes(this long bytes) =>
            bytes / 1024f;

        public static double? ConvertKilobytesToMegabytes(this long kilobytes) =>
            kilobytes / 1024f;

        #endregion
    }
}
