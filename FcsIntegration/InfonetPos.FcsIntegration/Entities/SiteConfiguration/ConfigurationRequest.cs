using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.SiteConfiguration
{
    [XmlType("Configuration")]
    public class ConfigurationRequest
    {
        public string Version { get; set; }
        public string AllowDriveOff { get; set; }
        public string AllowCarWash { get; set; }
        public string MeasureUnit { get; set; }
        public PumpTest PumpTest { get; set; }
        public Prepay Prepay { get; set; }
        public PostPay PostPay { get; set; }
        public PayAtPump PayAtPump { get; set; }
        public FuelPrice FuelPrice { get; set; }

        [XmlArray("Pumps"), XmlArrayItem(typeof(Pump))]
        public List<Pump> Pumps { get; set; }
    }
}
