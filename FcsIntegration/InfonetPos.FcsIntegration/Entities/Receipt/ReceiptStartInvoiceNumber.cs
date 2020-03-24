using System;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.Receipt
{
    public class ReceiptStartInvoiceNumber
    {
        [XmlAttribute]
        public string Order
        {
            get
            {
                return ReceiptOrder.ToString();
            }
            set
            {
                ReceiptOrder = (ReceiptOrder)Enum.Parse(typeof(ReceiptOrder), value);
            }
        }
        [XmlText]
        public string InvoiceNumber { get; set; }
        [XmlIgnore]
        public ReceiptOrder ReceiptOrder { get; set; }
    }
}
