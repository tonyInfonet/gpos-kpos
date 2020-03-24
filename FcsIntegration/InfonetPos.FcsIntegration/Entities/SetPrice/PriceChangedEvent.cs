using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.SetPrice
{
    [XmlType("PriceChanged")]
    public class PriceChangedEvent
    {
        [XmlElement("Price")]
        public List<PriceChange> PriceChanges { get; set; }
    }
}
