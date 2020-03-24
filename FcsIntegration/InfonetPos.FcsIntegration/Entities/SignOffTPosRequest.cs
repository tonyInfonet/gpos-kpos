using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities
{
    [XmlType("SignOffTPOS")]
    public class SignOffTPosRequest
    {
        [XmlElement("POSID")]
        public string PosId { get; set; }
    }
}
