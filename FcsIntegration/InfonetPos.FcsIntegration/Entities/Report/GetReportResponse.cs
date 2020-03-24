using System;
using System.Xml;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.Report
{
    [XmlType("GetReportResp")]
    public class GetReportResponse
    {
        public string Result { get; set; }
        public XmlCDataSection Data
        {
            get
            {
                return new System.Xml.XmlDocument().CreateCDataSection(Report);
            }
            set
            {
                Report = value.Value;
            }
        }

        [XmlIgnore]
        public string Report { get; set; }
        [XmlIgnore]
        public bool ResultOK => string.Equals(Result, "OK", StringComparison.OrdinalIgnoreCase);
    }
}
