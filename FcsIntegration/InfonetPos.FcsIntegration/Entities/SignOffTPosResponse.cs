using System;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities
{
    [XmlType("SignOffTPOSResp")]
    public class SignOffTPosResponse
    {
        [XmlElement("POSID")]
        public string PosId { get; set; }
        public string Result { get; set; }

        [XmlIgnore]
        public bool ResultOk => string.Equals(Result, "OK", StringComparison.OrdinalIgnoreCase);
    }
}
