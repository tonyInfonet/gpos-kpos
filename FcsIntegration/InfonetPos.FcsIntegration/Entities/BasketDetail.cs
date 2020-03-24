using InfonetPos.FcsIntegration.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities
{
    [XmlType("BasketDetail")]
    public class BasketDetail
    {
        public string BasketID { get; set; }

        public string PumpID { get; set; }
        public bool ShouldSerializePumpID()
        {
            return Type == BasketRequestType.Create;
        }

        public string InvoiceID { get; set; }
        public bool ShouldSerializeInvoiceID()
        {
            return Type == BasketRequestType.Remove;
        }

        public string InvoiceType { get; set; }
        public bool ShouldSerializeInvoiceType()
        {
            return Type == BasketRequestType.Remove;
        }

        public string PayType { get; set; }
        public bool ShouldSerializePayType()
        {
            return Type == BasketRequestType.Create ||
                Type == BasketRequestType.Remove;
        }

        private double unitPrice;
        public double UnitPrice
        {
            get => Math.Round(unitPrice, 3);
            set { unitPrice = value; }
        }
        public bool ShouldSerializeUnitPrice()
        {
            return (Type == BasketRequestType.Create || Type == BasketRequestType.Remove);
        }

        private double totalPaid;
        public double TotalPaid
        {
            get => Math.Round(totalPaid, 2);
            set { totalPaid = value; }
        }
        public bool ShouldSerializeTotalPaid()
        {
            return Type == BasketRequestType.Remove;
        }

        private double change;
        public double Change
        {
            get => Math.Round(change, 2);
            set { change = value; }
        }
        public bool ShouldSerializeChange()
        {
            return Type == BasketRequestType.Remove;
        }

        public string PayDesc { get; set; }
        public bool ShouldSerializePayDesc()
        {
            return Type == BasketRequestType.Remove;
        }

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
        public bool ShouldSerializeData()
        {
            return Type == BasketRequestType.Remove;
        }

        public string Grade { get; set; }
        public bool ShouldSerializeGrade()
        {
            return Type == BasketRequestType.Create;
        }

        private double amount;
        public double Amount
        {
            get => Math.Round(amount, 2);
            set { amount = value; }
        }
        public bool ShouldSerializeAmount()
        {
            return Type == BasketRequestType.Create;
        }

        public double Volume { get; set; }
        public bool ShouldSerializeVolume()
        {
            return Type == BasketRequestType.Create;
        }

        public PrepayDetailInBasket Prepay { get; set; }
        public bool ShouldSerializePrepay()
        {
            return Type == BasketRequestType.Create;
        }


        [XmlIgnore]
        public string Receipt { get; set; }
        [XmlIgnore]
        public BasketRequestType Type { get; set; }


        public bool IsRefundAvailable()
        {
            return Prepay?.PrepayAmount > Amount;
        }

        public override string ToString()
        {
            if (Type == BasketRequestType.Remove)
            {
                return string.Format("Type:{0},BasketID:{1},InvoiceID:{2},InvoiceType:{3},UnitPrice:{4},TotalPaid:{5},Change:{6},Receipt:{7} ", this.Type, this.BasketID, this.InvoiceID, this.InvoiceType, this.unitPrice, this.TotalPaid, this.Change, this.Receipt);
            }
            else
            {
                return string.Format("Type:{0},BasketID:{1} ", this.Type, this.BasketID);
            }
        }

        public void SetPayTenderInfo(KeyValuePair<string, string> tenderInfo)
        {
            PayType = tenderInfo.Key;
            PayDesc = tenderInfo.Value;
        }
    }
}
