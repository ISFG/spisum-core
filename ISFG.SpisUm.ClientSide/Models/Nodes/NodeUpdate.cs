using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Nodes
{
    public class NodeUpdate
    {
        #region Properties

        [Required]
        [FromRoute] 
        public string NodeId { get; set; }

        [FromBody]
        public NodeUpdateBody Body { get; set; }

        #endregion

        #region Nested Types, Enums, Delegates

        public class NodeUpdateBody
        {
            #region Properties

            public string Name { get; set; }

            public string NodeType { get; set; }

            public List<string> AspectNames { get; set; }

            public Dictionary<string, object> Properties { get; set; }

            #endregion
        }

        #endregion
    }

    public class ComponentUpdate : NodeUpdate
    {
        #region Properties

        [Required]
        [FromRoute]
        public string ComponentId { get; set; }

        #endregion
    }
}