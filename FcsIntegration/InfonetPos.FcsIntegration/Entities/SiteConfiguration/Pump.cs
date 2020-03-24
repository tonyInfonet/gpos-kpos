using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.SiteConfiguration
{
    [XmlType("Pump")]
    public class Pump
    {
        public string PumpID { get; set; }
        public string CashierAuthorize { get; set; }
        public string AllowPostpay { get; set; }

        [XmlArray("Grades"), XmlArrayItem(typeof(Grade))]
        public List<Grade> Grades { get; set; }
    }
}
