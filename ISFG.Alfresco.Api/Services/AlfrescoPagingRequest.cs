using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi;
using RestSharp;

namespace ISFG.Alfresco.Api.Services
{
    public class AlfrescoPagingRequest<T, V, U> where T : IListPagination<V, U> where V : IAlfrescoList<U>
    {
        #region Fields

        private IImmutableList<Parameter> _parameters;
        private readonly Func<IImmutableList<Parameter>, Task<T>> _request;

        #endregion

        #region Constructors

        public AlfrescoPagingRequest(Func<IImmutableList<Parameter>, Task<T>> request)
        {
            _request = request;
            _parameters = ImmutableList<Parameter>.Empty;
        }

        #endregion

        #region Properties

        private T ResponseProp { get; set; }

        private bool HasMoreItems { get; set; } = true;
        private long I { get; set; }
        private long SaveLimit { get; } = 9999999;
        private long SkipCount { get; set; }

        #endregion

        #region Public Methods

        public async Task<bool> Next()
        {
            if (HasMoreItems && I < SaveLimit)
            {
                I++;

                MakeParameters();
                ResponseProp = await _request(_parameters);

                HasMoreItems = ResponseProp?.List?.Pagination?.HasMoreItems == true;
                SkipCount = (ResponseProp?.List?.Pagination?.SkipCount ?? 0) + (ResponseProp?.List?.Pagination?.Count ?? 0);

                return true;
            }

            return false;
        }

        public T Response() => ResponseProp;

        #endregion

        #region Private Methods

        private void MakeParameters()
        {
            var parametersList = _parameters.ToList();

            var skipCountIndex = parametersList.FindIndex(x => x.Name == AlfrescoNames.Headers.SkipCount && x.Type == ParameterType.QueryString);
            if (skipCountIndex != -1)
                parametersList.RemoveAt(skipCountIndex);

            parametersList.Add(new Parameter(AlfrescoNames.Headers.SkipCount, SkipCount, ParameterType.QueryString));

            _parameters = parametersList.ToImmutableList();
        }

        #endregion
    }
}
