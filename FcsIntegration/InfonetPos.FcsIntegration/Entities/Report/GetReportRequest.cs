using System;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.Report
{
    [XmlType("GetReport")]
    public class GetReportRequest
    {
        [XmlAttribute("type")]
        public string ReportType
        {
            get
            {
                return Type.ToString();
            }
            set
            {
                Type = (ReportType)Enum.Parse(typeof(ReportType), value);
            }
        }
        public GetReportCriteria Criteria { get; set; }
        [XmlIgnore]
        public ReportType Type { get; set; }
    }
}
