using System.Collections.Generic;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Common.Extensions;

namespace ISFG.SpisUm.ClientSide.Models.Shipments
{
    public class ShipmentComponentValidation
    {
        #region Fields

        public List<NodeEntry> ComponentsInfo = new List<NodeEntry>();

        #endregion

        #region Properties

        public long? TotalSizeBytes { get; set; }
        public double? TotalSizeKiloBytes { get => TotalSizeBytes?.ConvertBytesToKilobytes(); }
        public double? TotalSizeMegaBytes { get => TotalSizeBytes?.ConvertBytesToMegabytes(); }

        #endregion
    }
}
