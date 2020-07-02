using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ISFG.Common.Extensions
{
    public static class FormFileExt
    {
        #region Static Methods

        public static async Task<byte[]> GetBytes(this IFormFile formFile)
        {
            if (formFile.Length <= 0) 
                return null;
            
            await using var memoryStream = new MemoryStream();
            await formFile.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        #endregion
    }
}