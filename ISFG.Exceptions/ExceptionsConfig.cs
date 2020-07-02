using System;
using ISFG.Exceptions.Exceptions;
using ISFG.Exceptions.Handlers;
using ISFG.Exceptions.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ISFG.Exceptions
{
    public static class ExceptionsConfig
    {
        #region Static Methods

        public static void AddExceptions(this IServiceCollection services)
        {
            //services.AddSingleton<IExceptionHandler<NotSupportedException>, NotSupportedExceptionHandler>(); //501 NotImplemented
            services.AddSingleton<IExceptionHandler<Exception>, ExceptionHandler>(); // 500 InternalServerError
            services.AddSingleton<IExceptionHandler<NotFoundException>, NotFoundExceptionHandler>(); // 404 NotFound
            services.AddSingleton<IExceptionHandler<ForbiddenException>, ForbiddenExceptionHandler>(); // 403 Forbidden
            services.AddSingleton<IExceptionHandler<NotAuthenticatedException>, UnauthorizedAccessExceptionHandler>(); // 401 Unauthorized
            services.AddSingleton<IExceptionHandler<BadRequestException>, BadRequestExceptionHandler>(); // 400 BadRequest
        }

        #endregion
    }
}