using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities
{
    [XmlType("BasketResp")]
    public class BasketResponse
    {
        [XmlAttribute("type")]
        public string Type { get; set; }
        public string BasketID { get; set; }
        public string Result { get; set; }

        [XmlIgnore]
        public bool ResultOK => string.Equals(Result, "OK", StringComparison.OrdinalIgnoreCase);
    }

}
