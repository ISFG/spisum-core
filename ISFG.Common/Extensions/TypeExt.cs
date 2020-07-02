using System;
using System.Linq;

namespace ISFG.Common.Extensions
{
    public static class TypeExt
    {
        #region Static Methods

        public static TValue GetAttributeValue<TAttribute, TValue>(this Type type, Func<TAttribute,TValue> valueSelector)
            where TAttribute : Attribute
        {
            var att = type.GetCustomAttributes(typeof(TAttribute), true).FirstOrDefault() as TAttribute;

            return att == null ? default : valueSelector(att);
        }

        #endregion
    }
}
