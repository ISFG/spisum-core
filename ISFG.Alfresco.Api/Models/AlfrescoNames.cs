namespace ISFG.Alfresco.Api.Models
{
    public static class AlfrescoNames
    {
        #region Fields

        public static readonly string DocumentLibrary = "documentlibrary";

        #endregion

        #region Nested Types, Enums, Delegates

        public static class Aliases
        {
            #region Fields

            public static readonly string Group = "-group-";
            public static readonly string My = "-my-";
            public static readonly string Shared = "-shared-";
            public static readonly string Root = "-root-";
            public static readonly string Me = "-me-";
            public static readonly string FilePlan = "-filePlan-";

            #endregion
        }

        public static class Capabilities
        {
            #region Fields

            public static readonly string IsAdmin = "isAdmin";

            #endregion
        }

        public static class ContentModel
        {
            #region Fields

            public static readonly string Owner = "cm:owner";
            public static readonly string Folder = "cm:folder";
            public static readonly string Content = "cm:content";
            public static readonly string ModelActive = "cm:modelActive";
            public static readonly string Title = "cm:title";
            public static readonly string VersionLabel = "cm:versionLabel";
            public static readonly string CutOffDate = "rma:cutOffDate";

            #endregion
        }

        public static class Includes
        {
            #region Fields

            public static readonly string AllowableOperations = "allowableOperations";
            public static readonly string Association = "association";
            public static readonly string HasRetentionSchedule = "hasRetentionSchedule";
            public static readonly string IsLink = "isLink";
            public static readonly string IsFavorite = "isFavorite";
            public static readonly string IsLocked = "isLocked";
            public static readonly string Path = "path";
            public static readonly string Permissions = "permissions";
            public static readonly string Properties = "properties";

            #endregion
        }

        public static class Headers
        {
            #region Fields

            public static readonly string Attachment = "attachment";
            public static readonly string Fields = "fields";
            public static readonly string Include = "include";
            public static readonly string IncludeSource = "includeSource";
            public static readonly string MaxItems = "maxItems";
            public static readonly string NodeType = "nodeType";
            public static readonly string Name = "name";
            public static readonly string OrderBy = "orderBy";
            public static readonly string OverWrite = "overwrite";
            public static readonly string Permanent = "permanent";
            public static readonly string RelativePath = "relativePath";
            public static readonly string SkipCount = "skipCount";
            public static readonly string Term = "term";
            public static readonly string Where = "where";

            #endregion
        }

        public static class Query
        {
            #region Fields
            
            public static readonly string Force = "force";
            public static readonly string C = "c";
            public static readonly string NoCache = "noCache";   
            
            #endregion
        }

        public static class MemberType
        {
            #region Fields

            public static readonly string Group = "(memberType='GROUP')";

            #endregion
        }

        public static class Permissions
        {
            #region Fields

            public static readonly string Consumer = "Consumer";
            public static readonly string Editor = "Editor";
            public static readonly string SiteManager = "SiteManager";

            #endregion
        }

        public static class Prefixes
        {
            #region Fields

            public static readonly string Path = "/Company Home/";

            #endregion
        }

        public static class Aspects
        {
            #region Fields

            public static readonly string Versionable = "cm:versionable";
            public static readonly string Ownable = "cm:ownable";

            #endregion
        }

        public static class Versions
        {
            #region Fields

            public static readonly string MajorVersion = "majorVersion";            

            #endregion
        }
        #endregion
    }
}