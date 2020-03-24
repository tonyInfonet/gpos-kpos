using System;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.Receipt
{
    [XmlType("GetReceipt")]
    public class GetReceiptRequest
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
        public GetReceiptCriteria Criteria { get; set; }
        [XmlIgnore]
        public ReceiptType Type { get; set; }
    }
}
