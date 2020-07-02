using System;
using System.Collections.Generic;
using System.Linq;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;

namespace ISFG.Alfresco.Api.Services
{
    public class AlfrescoModelComparer : IAlfrescoModelComparer
    {
        #region Implementation of IAlfrescoModelComparer

        [Obsolete("Do NOT Use for now", true)]
        // ReSharper disable once ENC0036
        public List<ObjectDifference> CompareObjects<T>(T obj1, T obj2, string path = null) where T : class
        {
            var difference = new List<ObjectDifference>();

            if (obj1 == null && obj2 == null) return difference;
            if (obj1 == null || obj2 == null) throw new Exception("One of the object is null.");

            foreach (var property in obj1.GetType().GetProperties())
            {
                if (property.Name == "AdditionalProperties") continue;
                
                var localPath = path != null ? $"{path}.{property.Name}" : property.Name;
                
                object property1 = property.GetValue(obj1); 
                object property2 = property.GetValue(obj2);
                
                if (property1 == null && property2 == null) continue;
                if (property1 == null)
                {
                    difference.Add(new ObjectDifference(Operations.New, localPath, property2));
                    continue;
                }
                if (property2 == null)
                {
                    difference.Add(new ObjectDifference(Operations.Deleted, localPath, null, property1));
                    continue;
                }

                Type type = property.PropertyType;
                if (!(type.IsPrimitive || type.IsValueType || type == typeof(string) || type == typeof(decimal)))
                {
                    if (!ReferenceEquals(obj1, property1) && !ReferenceEquals(obj2, property2))
                    {
                        var result = CompareObjects(property1, property2, localPath);
                        if (result != null && result.Any())
                            difference.AddRange(result);
                    } 
                }
                else if (!property1.Equals(property2))
                {
                    difference.Add(new ObjectDifference(Operations.Edit, localPath, property2, property1));
                }
            }
            
            return difference;
        }

        public List<ObjectDifference> CompareProperties(Dictionary<string, object> dict1, Dictionary<string, object> dict2, string path = null)
        {
            var difference = new List<ObjectDifference>();
            
            if (dict1 == null && dict2 == null) return difference;;
            if (dict1 == null || dict2 == null) throw new Exception("One of the object is null.");

            difference.AddRange(dict1.Keys.Except(dict2.Keys).ToList().Select(key => new ObjectDifference(Operations.Deleted, path != null ? $"{path}.{key}" : key, null, dict1[key])));
            difference.AddRange(dict2.Keys.Except(dict1.Keys).ToList().Select(key => new ObjectDifference(Operations.New, path != null ? $"{path}.{key}" : key, dict2[key])));

            foreach (var key in dict1.Keys.Intersect(dict2.Keys).ToList())
            {
                var valueDict1 = dict1[key];
                var valueDict2 = dict2[key];
                
                if (valueDict1 is Dictionary<string, object> valueDict1Nested && valueDict2 is Dictionary<string, object> valueDict2Nested)
                    difference.AddRange(CompareProperties(valueDict1Nested, valueDict2Nested, key));
                else
                if (!valueDict1.Equals(valueDict2))
                    difference.Add(new ObjectDifference(Operations.Edit, path != null ? $"{path}.{key}" : key, valueDict2, valueDict1)); 
            }
            
            return difference;
        }

        #endregion
    }
}