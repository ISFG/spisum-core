using System.Linq;

namespace ISFG.SpisUm.ClientSide.Models.Signer
{
    public class SignerCreateResponse
    {
        #region Constructors

        public SignerCreateResponse(string signer, string batchId, params string[] components)
        {
            Signer = signer;
            Components = components.ToArray();
            BatchId = batchId;
        }

        #endregion

        #region Properties

        public string Signer { get; }
        public string[] Components { get; }
        public string BatchId { get; }

        
        #endregion
    }
}