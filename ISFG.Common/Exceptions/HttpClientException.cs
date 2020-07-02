using System;
using System.Net;

namespace ISFG.Common.Exceptions
{
    public class HttpClientException : Exception
    {
        #region Constructors

        public HttpClientException(HttpStatusCode httpStatusCode, string content)
        {
            HttpStatusCode = httpStatusCode;
            Content = content;
        }

        #endregion

        #region Properties

        public HttpStatusCode HttpStatusCode { get; }
        public string Content { get; }

        #endregion
    }
}