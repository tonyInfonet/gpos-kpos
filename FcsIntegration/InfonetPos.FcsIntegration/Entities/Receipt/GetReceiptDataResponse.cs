using System;
using System.Xml;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.Receipt
{
    [XmlType("GetReceiptDataResp")]
    public class GetReceiptDataResponse
    {
        [XmlAttribute("type")]
        public string Type { get; set; }
        public string Result { get; set; }
        public XmlCDataSection Data
        {
            get
            {
                return new System.Xml.XmlDocument().CreateCDataSection(Receipt);
            }
            set
            {
                Receipt = value.Value;
            }
        }
        [XmlIgnore]
        public ReceiptType ReceiptType
        {
            get
            {
                return (ReceiptType)Enum.Parse(typeof(ReceiptType), Type);
            }
        }
        [XmlIgnore]
        public bool ResultOK => string.Equals(Result, "OK", StringComparison.OrdinalIgnoreCase);
        [XmlIgnore]
        public string Receipt { get; set; }
    }
}
