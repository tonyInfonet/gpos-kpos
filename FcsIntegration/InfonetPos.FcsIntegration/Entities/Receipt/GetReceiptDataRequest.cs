using System;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.Receipt
{
    [XmlType("GetReceiptData")]
    public class GetReceiptDataRequest
    {
        [XmlAttribute("type")]
        public string ReceiptType
        {
            get
            {
                return Type.ToString();
            }
            set
            {
                Type = (ReceiptType)Enum.Parse(typeof(ReceiptType), value);
            }
        }
        public GetReceiptDataCriteria Criteria { get; set; }
        [XmlIgnore]
        public ReceiptType Type { get; set; }
    }
}
