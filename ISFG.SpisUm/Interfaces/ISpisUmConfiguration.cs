using ISFG.SpisUm.Configurations.Models;

namespace ISFG.SpisUm.Interfaces
{
    public interface ISpisUmConfiguration
    {
        #region Properties

        public DownloadConfiguration Download { get; set; }
        public SsidConfiguration Ssid { get; set; }
        public ComponentPIDConfiguration ComponentPID { get; set; }
        public Shipments Shipments { get; set; }

        public string Address { get; }
        public string Name { get; }
        public string Originator { get; }

        #endregion
    }
}
