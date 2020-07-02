using ISFG.Common.Attributes;
using ISFG.SpisUm.Configurations.Models;
using ISFG.SpisUm.Interfaces;

namespace ISFG.SpisUm.Configurations
{
    [Settings("SpisUm")]
    public class SpisUmConfiguration : ISpisUmConfiguration
    {
        #region Implementation of ISpisUmConfiguration

        public string Address { get; set; }
        public ComponentPIDConfiguration ComponentPID { get; set; }
        public DownloadConfiguration Download { get; set; }
        public string Name { get; set; }
        public string Originator { get; set; }
        public Shipments Shipments { get; set; }
        public SsidConfiguration Ssid { get; set; }

        #endregion
    }
}
