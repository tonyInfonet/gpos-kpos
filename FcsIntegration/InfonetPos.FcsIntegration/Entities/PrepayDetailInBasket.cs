using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities
{
    [XmlType("Prepay")]
    public class PrepayDetailInBasket
    {
        public string PrepayInvoice { get; set; }

        private double prepayAmount;
        public double PrepayAmount
        {
            get => Math.Round(prepayAmount, 2);
            set { prepayAmount = value; }
        }
    }
}
