using System;
using System.Xml;
using System.Xml.Serialization;

using InfonetPos.FcsIntegration.Enums;

namespace InfonetPos.FcsIntegration.Entities
{
    [XmlType("Prepay")]
    public class PrepayRequest
    {
        [XmlAttribute("type")]
        public string Type { get; set; }
        public int PumpID { get; set; }
        public int OldPumpID { get; set; }
        public bool ShouldSerializeOldPumpID() => Type == PrepayRequestType.Switch.ToString();
        public string InvoiceID { get; set; }

        private double amount;
        public double Amount
        {
            get => Math.Round(amount, 2);
            set { amount = value; }
        }
        public bool ShouldSerializeAmount() => Type == PrepayRequestType.Set.ToString();

        public string PosID { get; set; }
        public bool ShouldSerializePosID() => Type == PrepayRequestType.Set.ToString();
        public string PayType { get; set; }
        public bool ShouldSerializePayType() => Type == PrepayRequestType.Set.ToString();

        private double totalPaid;
        public double TotalPaid
        {
            get => Math.Round(totalPaid, 2);
            set { totalPaid = value; }
        }
        public bool ShouldSerializeTotalPaid() => Type == PrepayRequestType.Set.ToString();

        private double change;
        public double Change
        {
            get => Math.Round(change, 2);
            set { change = value; }
        }
        public bool ShouldSerializeChange() => Type == PrepayRequestType.Set.ToString();

        [XmlElement("Receipt")]
        public XmlCDataSection Data
        {
            get
            {
                return string.IsNullOrEmpty(Receipt) ? null : new System.Xml.XmlDocument().CreateCDataSection(Receipt);
            }
            set
            {
                Receipt = value.Value;
            }
        }
        public bool ShouldSerializeData() => Type == PrepayRequestType.Set.ToString();
        [XmlIgnore]
        public string Receipt { get; set; }
    }
}
