using System;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.Receipt
{
    [XmlType("SingleData")]
    public class ReceiptInfo
    {
        public string InvoiceNumber { get; set; }
        public float Quantity { get; set; }
        public float Amount { get; set; }
        public int PumpNumber { get; set; }
        public string CardType { get; set; }
        public DateTime DateTime { get; set; }

        [XmlIgnore]
        public string Date => DateTime.ToShortDateString();

        [XmlIgnore]
        public string Time => DateTime.ToShortTimeString();
    }
}
