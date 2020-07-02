using System.Collections.Generic;
using System.Linq;

namespace ISFG.Common.Utils
{
    public static class Dn
    {
        #region Static Methods

        public static Dictionary<string, string> Parse(string input)
        {
            if (string.IsNullOrEmpty(input))
                return new Dictionary<string, string>();
            
            var inputSplit = input.Split(',');
            
            return !inputSplit.Any() ? new Dictionary<string, string>() : inputSplit
                .Select(item => item.Split('='))
                .Where(itemSplit => itemSplit.Any() && itemSplit.Length == 2)
                .ToDictionary(itemSplit => itemSplit[0].Trim(), itemSplit =>
                {
                    var itemTrim = itemSplit[1].Trim();
                    
                    if (itemTrim.StartsWith("\""))
                        itemTrim = itemTrim.Remove(0);
                    if (itemTrim.EndsWith("\""))
                        itemTrim = itemTrim.Remove(itemTrim.Length - 1);

                    return itemTrim;
                });
        }

        #endregion
    }
}