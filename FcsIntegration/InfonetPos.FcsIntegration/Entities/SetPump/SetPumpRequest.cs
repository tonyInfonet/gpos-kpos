using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.SetPump
{
    [XmlType("SetPump")]
    public class SetPumpRequest
    {
        [XmlAttribute("type")]
        public string Type { get; set; }
        public int PumpID { get; set; }
        public string Grade { get; set; }
        public string PayType { get; set; }
    }
}
