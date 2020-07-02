using System.Collections.Immutable;
using RestSharp;

namespace ISFG.Alfresco.Api.Interfaces
{
    public class AlfrescoRequest<T>
    {
        #region Properties

        public T QueryParams { get; set; }

        #endregion

        #region Public Methods

        public void FillQueryParams()
        {
            if (QueryParams == null) 
                return;

            var parameters = ImmutableList<Parameter>.Empty;
            
            foreach (var property in QueryParams.GetType().GetProperties())
            {
                var value = property.GetValue(QueryParams);
                if (value != null)
                    parameters.Add(new Parameter(property.Name, value, ParameterType.QueryString));
            }
        }

        #endregion
    }
}
