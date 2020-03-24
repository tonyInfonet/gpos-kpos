using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.SetPrice
{
    [XmlType("Price")]
    public class PriceChange
    {
        private bool isRequest;

        [XmlIgnore]
        public int DecimalPlaces { get; set; }

        public PriceChange() { isRequest = false; DecimalPlaces = 2; }
        public PriceChange(bool isRequest) { this.isRequest = isRequest; DecimalPlaces = 2; }

        public string Grade { get; set; }
        public string TierLevel { get; set; }

        private double cashPrice;
        public double CashPrice
        {
            get => Math.Round(cashPrice, DecimalPlaces);
            set { cashPrice = value; }
        }
        public bool ShouldSerializeCashPrice() => isRequest;

        private double creditPrice;
        public double CreditPrice
        {
            get => Math.Round(creditPrice, DecimalPlaces);
            set { creditPrice = value; }
        }
        public bool ShouldSerializeCreditPrice() => isRequest;

        private double currentPrice;
        public double CurrentPrice
        {
            get => Math.Round(currentPrice, DecimalPlaces);
            set { currentPrice = value; }
        }
        public bool ShouldSerializeCurrentPrice() => !isRequest;
    }
}
