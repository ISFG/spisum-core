using ISFG.Common.Utils;

namespace ISFG.Alfresco.Api.Models
{
    public class FormDataParam
    {
        #region Constructors

        public FormDataParam(byte[] file, string fileName = null, string name = null, string contentType = null)
        {
            Name = name ?? "filedata";
            FileName = fileName ?? IdGenerator.GenerateId();
            File = file ?? new byte[0];
            ContentType = contentType;
        }

        #endregion

        #region Properties

        public string Name { get; }
        public string FileName { get; }
        public byte[] File { get; }
        public string ContentType { get; }

        #endregion
    }
}