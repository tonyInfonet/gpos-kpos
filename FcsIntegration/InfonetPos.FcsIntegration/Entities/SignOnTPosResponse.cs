using System;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities
{
    [XmlType("SignOnTPOSResp")]
    public class SignOnTPosResponse
    {
        [XmlElement("POSID")]
        public string PosId { get; set; }
        public string Version { get; set; }
        public string Result { get; set; }

        [XmlIgnore]
        public bool ResultOk => string.Equals(Result, "OK", StringComparison.OrdinalIgnoreCase);
    }
}
