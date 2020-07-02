using System.Collections.Generic;

namespace ISFG.SpisUm.ClientSide.Extensions
{
    public static class ListExt
    {
        #region Static Methods

        public static void AddRangeUnique(this List<string> list, List<string> addItems)
        {
            if (addItems == null)
                return;

            foreach (var item in addItems)
                if (!list.Contains(item))
                    list.Add(item);
        }

        public static void AddRangeUnique(this List<string> list, string[] addItems)
        {
            if (addItems == null)
                return;

            foreach (var item in addItems)
                if (!list.Contains(item))
                    list.Add(item);
        }

        public static void AddUnique(this List<string> list, string item)
        {
            if (!list.Contains(item))
                list.Add(item);
        }

        #endregion
    }
}
