using System;

namespace ISFG.Exceptions.Exceptions
{
    public class NotFoundException : Exception
    {
        #region Constructors

        public NotFoundException(object key, Type entityType = null)
            : this(key, null, entityType)
        {
        }

        public NotFoundException(object key, Exception innerException, Type entityType = null)
            : base(entityType == null ? $"Key '{key}' was not found." : $"Key '{key}' type of '{entityType.FullName}' not found.", innerException)
        {
            Key = key;
            EntityType = entityType;
        }

        #endregion

        #region Properties

        public object Key { get; }
        public Type EntityType { get; }

        #endregion
    }
}