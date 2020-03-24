using System;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities
{

    [XmlType(TypeName = "SignOn")]
    public class SignOnRequest
    {
        [XmlElement(ElementName = "POSID")]
        public string PosId { get; set; }
        
        public string Version { get; set; }
    }
}
