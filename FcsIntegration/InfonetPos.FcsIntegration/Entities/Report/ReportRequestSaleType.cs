using System;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.Report
{
    public class ReportRequestSaleType
    {
        [XmlAttribute("type")]
        public string SaleType
        {
            get
            {
                return Type.ToString();
            }
            set
            {
                Type = (ReportCriteriaSaleType)Enum.Parse(typeof(ReportCriteriaSaleType), value);
            }
        }
        public int Quantity { get; set; }

        [XmlIgnore]
        public ReportCriteriaSaleType Type { get; set; }
    }
}
