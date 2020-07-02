using System;

namespace ISFG.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class SettingsAttribute : Attribute
    {
        #region Constructors

        public SettingsAttribute(string selection) => Selection = selection;

        public SettingsAttribute(string selection, string value)
        {
            Selection = selection;
            Value = value;
        }

        #endregion

        #region Properties

        public string Selection { get; }

        public string Value { get; }

        #endregion
    }
}
