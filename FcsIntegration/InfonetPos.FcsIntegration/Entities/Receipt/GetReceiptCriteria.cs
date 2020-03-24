using System;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.Receipt
{
    public class GetReceiptCriteria
    {
        [XmlElement(DataType = "date")]
        public DateTime Date { get; set; }
        public ReceiptStartInvoiceNumber StartInvoiceNumber { get; set; }
        public int Count { get; set; }
    }
}
