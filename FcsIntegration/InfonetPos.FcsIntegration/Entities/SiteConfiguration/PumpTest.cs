using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.SiteConfiguration
{
    [XmlType("PumpTest")]
    public class PumpTest
    {
        public string AllowPumpTest { get; set; }
        public double MaximumVolume { get; set; }
    }
}
