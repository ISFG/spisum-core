using System;

namespace ISFG.Exceptions.Exceptions
{
    public class NotFoundTException<T> : NotFoundException
    {
        #region Constructors

        public NotFoundTException(object id) : base(id, typeof(T))
        {
        }

        public NotFoundTException(object id, Exception innerException) : base(id, innerException, typeof(T))
        {
        }

        #endregion
    }
}