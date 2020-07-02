using System;
using System.Collections.Generic;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;

namespace AuditLogService.UnitTest.Models
{
    public static class AlfrescoComparerModels
    {
        #region Static Methods

        public static NodeEntry SameObject() => new NodeEntry
        {
            Entry = new Node
            {
                Id = "Id",
                Name = "Name"
            }
        };

        public static NodeEntry EditedObject1() => new NodeEntry
        {
            Entry = new Node
            {
                Id = "Id",
                IsFile = true,
                NodeType = "NodeType",
                ModifiedAt = GetDateTimeOffset(),
                IsFavorite = true
            }
        };

        public static NodeEntry EditedObject2() => new NodeEntry
        {
            Entry = new Node
            {
                Id = "Different",
                IsFile = true,
                NodeType = "Different",
                ModifiedAt = GetDateTimeOffset(),
                IsFavorite = false
            }
        };

        public static NodeEntry NewObject1() => new NodeEntry
        {
            Entry = new Node
            {
                IsFile = true,
                NodeType = "NodeType",
                ModifiedAt = GetDateTimeOffset()
            }
        };

        public static NodeEntry NewObject2() => new NodeEntry
        {
            Entry = new Node
            {
                Id = "Different",
                IsFile = true,
                NodeType = "NodeType",
                ModifiedAt = GetDateTimeOffset(),
                IsFavorite = true
            }
        };

        public static NodeEntry DeletedObject1() => new NodeEntry
        {
            Entry = new Node
            {
                Id = "Different",
                IsFile = true,
                NodeType = "NodeType",
                ModifiedAt = GetDateTimeOffset(),
                IsFavorite = true
            }
        };

        public static NodeEntry DeletedObject2() => new NodeEntry
        {
            Entry = new Node
            {
                IsFile = true,
                NodeType = "NodeType",
                ModifiedAt = GetDateTimeOffset()
            }
        };


        public static NodeEntry PrimitiveTypes1() => new NodeEntry
        {
            Entry = new Node
            {
                IsFile = true,
                NodeType = "NodeType",
                ModifiedAt = GetDateTimeOffset(),
                IsFavorite = true
            }
        };

        public static NodeEntry PrimitiveTypes2() => new NodeEntry
        {
            Entry = new Node
            {
                Id = "Different",
                IsFile = false,
                ModifiedAt = GetDateTimeOffset(),
                IsFavorite = true
            }
        };

        public static NodeEntry ReferenceTypes1() => new NodeEntry
        {
            Entry = new Node
            {
                Id = "Id",
                NodeType = "NodeType",
                Content = new ContentInfo
                {
                    Encoding = "Encoding",
                    MimeType = "MimeType"
                },
                CreatedByUser = new UserInfo
                {
                    DisplayName = "DisplayName"
                }
            }
        };

        public static NodeEntry ReferenceTypes2() => new NodeEntry
        {
            Entry = new Node
            {
                Id = "Id",
                NodeType = "NodeType",
                Content = new ContentInfo
                {
                    Encoding = "Different"
                },
                CreatedByUser = new UserInfo
                {
                    Id = "Different",
                    DisplayName = "DisplayName"
                }
            }
        };

        public static Dictionary<string, object> Properties1() => new Dictionary<string, object>
        {
            {"ssl:pid", "123456"},
            {"ssl:customBool", true},
            {"ssl:deletedValue", "delete"}
        };

        public static Dictionary<string, object> Properties2() => new Dictionary<string, object>
        {
            {"ssl:pid", "123456"},
            {"ssl:customBool", false},
            {"ssl:newValue", "new"}
        };

        public static Dictionary<string, object> NestedProperties1() => new Dictionary<string, object>
        {
            {"ssl:pid", "123456"},
            {"ssl:customBool", true},
            {"ssl:deletedValue", "delete"},
            {"ssl:nestedValue", new Dictionary<string, object> {{"id", "martin"}, {"displayName", "martin"}}}
        };

        public static Dictionary<string, object> NestedProperties2() => new Dictionary<string, object>
        {
            {"ssl:pid", "123456"},
            {"ssl:customBool", false},
            {"ssl:newValue", "new"},
            {"ssl:nestedValue", new Dictionary<string, object> {{"id", "martin"}, {"displayName", "karel"}}}
        };

        private static DateTimeOffset GetDateTimeOffset() => new DateTimeOffset(2020, 5, 20, 0, 0, 0, TimeSpan.Zero);

        #endregion
    }
}