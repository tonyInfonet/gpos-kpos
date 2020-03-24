using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.SetPrice
{
    [XmlType("SetPriceResp")]
    public class SetPriceResponse
    {
        public string Result { get; set; }

        [XmlIgnore]
        public bool ResultOK => string.Equals(Result, "OK", StringComparison.OrdinalIgnoreCase);
    }
}
