using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.Receipt
{
    [XmlType("GetReceiptResp")]
    public class GetReceiptResponse
    {
        [XmlAttribute("type")]
        public string Type { get; set; }
        public string Result { get; set; }

        public List<ReceiptInfo> Data { get; set; }

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
    }
}
