using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using InfonetPOS.Core.DB.Enums;

namespace InfonetPOS.Core.DB.Entities
{
    public class Tender
    {
        public string Description { get; set; }
        public double ExchangeRate { get; set; }
        public TenderClass Class { get; set; }
        public bool? GiveAsRef { get; set; }

        public Tender()
        {

        }

        public Tender(string classStr)
        {
            switch (classStr.ToUpper())
            {
                case "CRCARD":
                    Class = TenderClass.CRCARD;
                    break;
                case "DBCARD":
                    Class = TenderClass.DBCARD;
                    break;
                default:
                    Class = TenderClass.CASH;
                    break;
            }
        }
    }
}
