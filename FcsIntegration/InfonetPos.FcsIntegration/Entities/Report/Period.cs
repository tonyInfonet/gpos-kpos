using System;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.Report
{
    public class Period
    {
        [XmlElement(DataType = "date")]
        public DateTime StartDay { get; set; }
        [XmlElement(DataType = "date")]
        public DateTime EndDay { get; set; }
    }
}
