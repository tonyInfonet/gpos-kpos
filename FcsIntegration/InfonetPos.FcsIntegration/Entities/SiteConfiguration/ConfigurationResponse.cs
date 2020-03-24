using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.SiteConfiguration
{
    [XmlType("ConfigurationResp")]
    public class ConfigurationResponse
    {
        public string Result { get; set; }
        public string ErrMsg { get; set; }

        [XmlIgnore]
        public bool ResultOK => string.Equals(Result, "OK", StringComparison.OrdinalIgnoreCase);
    }
}
