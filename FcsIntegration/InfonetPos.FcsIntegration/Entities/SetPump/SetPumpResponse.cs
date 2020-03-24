using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.SetPump
{
    [XmlType("SetPumpResp")]
    public class SetPumpResponse
    {
        [XmlAttribute("type")]
        public string Type { get; set; }
        public int PumpID { get; set; }
        public string Result { get; set; }

        [XmlIgnore]
        public bool ResultOK => string.Equals(Result, "OK", StringComparison.OrdinalIgnoreCase);
    }
}
