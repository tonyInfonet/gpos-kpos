﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities.SiteConfiguration
{
    [XmlType("PayAtPump")]
    public class PayAtPump
    {
        public string AutoDeactivate { get; set; }
        public string AutoDeactivation { get; set; }
        public string AutoActivation { get; set; }
        public string AllowManualOveride { get; set; }
    }
}
