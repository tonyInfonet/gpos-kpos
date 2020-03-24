using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.SiteConfiguration
{
    [XmlType("Price")]
    public class Price
    {
        public string UsePriceIncrement { get; set; }
        public double UnitPrice { get; set; }
        public double BasePrice { get; set; }
        public double PriceIncrement { get; set; }
    }
}
