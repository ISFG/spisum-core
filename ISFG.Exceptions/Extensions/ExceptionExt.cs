using System;
using ISFG.Exceptions.Interfaces;

namespace ISFG.Exceptions.Extensions
{
    internal static class ExceptionExt
    {
        #region Static Methods

        public static IExceptionHandler ResolveHandler(this Exception exception, IServiceProvider serviceProvider)
        {
            var type = exception?.GetType();
            while (type != null && type != typeof(Exception).BaseType)
            {
                var typeHandler = typeof(IExceptionHandler<>).MakeGenericType(type);

                var handler = (IExceptionHandler) serviceProvider.GetService(typeHandler);
                if (handler != null)
                    return handler;

                type = type.BaseType;
            }

            return null;
        }

        #endregion
    }
}