using System.Collections.Immutable;
using ISFG.Alfresco.Api.Models;
using RestSharp;

namespace ISFG.Alfresco.Api.Extensions
{
    public static class ImmutableListExt
    {
        #region Static Methods

        public static ImmutableList<Parameter> AddIfNotNull(this ImmutableList<Parameter> list, string name, object value, ParameterType type)
        {
            return value == null ? list : list.Add(new Parameter(name, value, type));
        }

        public static ImmutableList<Parameter> AddQueryParams<T>(this ImmutableList<Parameter> list, T queryParams)
        {
            if (typeof(T) == typeof(SimpleQueryParams) || typeof(T).IsSubclassOf(typeof(SimpleQueryParams)))
            {
                var parameters = queryParams as SimpleQueryParams;

                list = list.AddIfNotNull(AlfrescoNames.Headers.MaxItems, parameters.MaxItems, ParameterType.QueryString)
                    .AddIfNotNull(AlfrescoNames.Headers.SkipCount, parameters.SkipCount, ParameterType.QueryString);
            }

            if (typeof(T) == typeof(BasicQueryParams) || typeof(T).IsSubclassOf(typeof(BasicQueryParams))) 
            {
                var parameters = queryParams as BasicQueryParams;

                list = list.AddIfNotNull(AlfrescoNames.Headers.Fields, parameters.Fields, ParameterType.QueryString)
                    .AddIfNotNull(AlfrescoNames.Headers.OrderBy, parameters.OrderBy, ParameterType.QueryString);
            }

            if (typeof(T) == typeof(AdvancedBasicQueryParams) || typeof(T).IsSubclassOf(typeof(AdvancedBasicQueryParams)))
            {
                var parameters = queryParams as AdvancedBasicQueryParams;

                list = list.AddIfNotNull(AlfrescoNames.Headers.Include, parameters.Include, ParameterType.QueryString)
                    .AddIfNotNull(AlfrescoNames.Headers.Where, parameters.Where, ParameterType.QueryString);
            }

            if (typeof(T) == typeof(IncludeFieldsQueryParams) || typeof(T).IsSubclassOf(typeof(IncludeFieldsQueryParams)))
            {
                var parameters = queryParams as IncludeFieldsQueryParams;

                list = list.AddIfNotNull(AlfrescoNames.Headers.Fields, parameters.Fields, ParameterType.QueryString)
                    .AddIfNotNull(AlfrescoNames.Headers.Include, parameters.Include, ParameterType.QueryString);
            }

            if (typeof(T) == typeof(BasicNodeQueryParams) || typeof(T).IsSubclassOf(typeof(BasicNodeQueryParams)))
            {
                var parameters = queryParams as BasicNodeQueryParams;

                list = list.AddIfNotNull(AlfrescoNames.Headers.IncludeSource, parameters.IncludeSource, ParameterType.QueryString);
            }

            if (typeof(T) == typeof(BasicNodeQueryParamsWithRelativePath) || typeof(T).IsSubclassOf(typeof(BasicNodeQueryParamsWithRelativePath)))
            {
                var parameters = queryParams as BasicNodeQueryParamsWithRelativePath;

                list = list.AddIfNotNull(AlfrescoNames.Headers.RelativePath, parameters.RelativePath, ParameterType.QueryString);
            }

            if (typeof(T) == typeof(ContentQueryParams) || typeof(T).IsSubclassOf(typeof(ContentQueryParams)))
            {
                var parameters = queryParams as ContentQueryParams;

                list = list.AddIfNotNull(AlfrescoNames.Headers.Attachment, parameters.Attachment, ParameterType.QueryString);
            }

            return list;
        }

        #endregion
    }
}