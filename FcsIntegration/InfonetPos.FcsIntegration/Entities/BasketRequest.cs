using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities
{
    [XmlType("Basket")]
    public class BasketRequest
    {
        [XmlAttribute("type")]
        public string Type { get; set; }
        public BasketDetail BasketDetail { get; set; }
    }
}
