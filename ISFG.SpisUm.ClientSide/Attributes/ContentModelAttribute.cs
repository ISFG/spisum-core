using System;

namespace ISFG.SpisUm.ClientSide.Attributes
{
    [AttributeUsage(AttributeTargets.All)]  
    public class ContentModelAttribute : Attribute  
    {
        #region Constructors

        public ContentModelAttribute(string contentModelType)
        {
            ContentModelType = contentModelType;
        }

        #endregion

        #region Properties

        public string ContentModelType { get; }

        #endregion
    }  
}