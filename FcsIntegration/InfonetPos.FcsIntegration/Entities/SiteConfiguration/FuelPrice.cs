using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.SiteConfiguration
{
    [XmlType("FuelPrice")]
    public class FuelPrice
    {
        public string Use3DigitPrice { get; set; }
        public int ThreeDigitPriceNumber { get; set; }

        [XmlArray("Grades"), XmlArrayItem(typeof(Grade))]
        public List<Grade> Grades { get; set; }
    }
}
