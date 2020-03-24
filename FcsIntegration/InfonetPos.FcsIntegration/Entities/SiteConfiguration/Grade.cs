using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.SiteConfiguration
{
    [XmlType("Grade")]
    public class Grade
    {
        [XmlAttribute("type")]
        public string Type { get; set; }
        public Price Price { get; set; }

        public bool IsType(string gradeType)
        {
            return string.Equals(Type, gradeType, StringComparison.OrdinalIgnoreCase);
        }
    }
}
