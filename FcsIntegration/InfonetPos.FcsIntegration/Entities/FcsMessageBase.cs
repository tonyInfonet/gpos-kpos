using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using InfonetPos.FcsIntegration.Utilities;

namespace InfonetPos.FcsIntegration.Entities
{
    public class FcsMessageBase<TCommand>
        where TCommand : class, new()
    {
        [XmlIgnore]
        public TCommand Command { get; set; }

        [XmlAnyElement]
        public XElement XmlCommand
        {
            get
            {
                return (Command == null ? null : XObjectExtensions.SerializeToXElement(Command, true));
            }
            set
            {
                Command = (value == null ? null : XObjectExtensions.Deserialize<TCommand>(value));
            }
        }
    }
}
