using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities
{
    [XmlType("Prepay")]
    public class PrepayEvent
    {
        public int PumpID { get; set; }
        public float Amount { get; set; }
    }
}
