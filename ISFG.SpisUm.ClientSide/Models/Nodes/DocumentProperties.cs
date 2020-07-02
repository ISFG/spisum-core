namespace ISFG.SpisUm.ClientSide.Models.Nodes
{
    public class DocumentProperties
    {
        #region Constructors

        public DocumentProperties(string nodeId) => NodeId = nodeId;

        #endregion

        #region Properties

        public string NodeId { get; }

        #endregion
    }
}