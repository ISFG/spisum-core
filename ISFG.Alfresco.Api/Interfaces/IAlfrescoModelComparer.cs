using System.Collections.Generic;
using ISFG.Alfresco.Api.Models;

namespace ISFG.Alfresco.Api.Interfaces
{
    public interface IAlfrescoModelComparer
    {
        #region Public Methods

        List<ObjectDifference> CompareObjects<T>(T obj1, T obj2, string path = null) where T : class;
        List<ObjectDifference> CompareProperties(Dictionary<string, object> dict1, Dictionary<string, object> dict2, string path = null);

        #endregion
    }
}