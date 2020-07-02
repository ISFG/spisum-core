using System;
using System.Collections.Generic;
using System.Text;

namespace ISFG.Alfresco.Api.Models.CoreApi
{
    public class FavouriteFile
    {
        public FavouriteBody File { get; set; }
    }
    public class FavouriteFolder
    {
        public FavouriteBody File { get; set; }
    }
    public class FavouriteSite
    {
        public FavouriteBody File { get; set; }
    }
    public class FavouriteBody
    {
        public string Guid { get; set; }
    }
}
