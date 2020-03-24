using System;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities
{
    [XmlType("PrepayResp")]
    public class PrepayResponse
    {
        [XmlAttribute("type")]
        public string Type { get; set; }
        public int PumpID { get; set; }
        public string OldPumpID { get; set; }
        public string Result { get; set; }

        [XmlIgnore]
        public bool ResultOk => string.Equals(Result, "OK", StringComparison.OrdinalIgnoreCase);
    }
}
