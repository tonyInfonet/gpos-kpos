using System;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities
{

    [XmlType(TypeName = "SignOnTPOS")]
    public class SignOnTPosRequest
    {
        [XmlElement(ElementName = "POSID")]
        public string PosId { get; set; }
        
        public string Version { get; set; }
        public string POSType { get; set; }
        public string UserID { get; set; }
        public int TillID { get; set; }
        public int Shift { get; set; }
    }
}
