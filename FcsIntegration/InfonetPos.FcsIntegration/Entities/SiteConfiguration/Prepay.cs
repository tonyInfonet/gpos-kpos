using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.SiteConfiguration
{
    [XmlType("Prepay")]
    public class Prepay
    {
        public double WarningAmount { get; set; }
        public double MaximumAmount { get; set; }
    }
}
