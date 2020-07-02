using System;
using System.Linq;
using System.Reflection;
using ISFG.SpisUm.ClientSide.Attributes;

namespace ISFG.SpisUm.ClientSide.Extensions
{
    public static class EnumExt
    {
        #region Static Methods

        public static string GetContentModel(this Enum genericEnum)
        {
            Type genericEnumType = genericEnum.GetType();
            MemberInfo[] memberInfo = genericEnumType.GetMember(genericEnum.ToString());
            if(memberInfo.Length > 0)
            {
                var attribs = memberInfo[0].GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
                if(attribs != null && attribs.Any()) return ((ContentModelAttribute)attribs.ElementAt(0)).ContentModelType;
            }
            
            return genericEnum.ToString();
        }

        #endregion
    }
}