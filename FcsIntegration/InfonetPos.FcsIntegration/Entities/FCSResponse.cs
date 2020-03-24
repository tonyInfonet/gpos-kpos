using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities
{
    [XmlRoot("FCSResponse")]
    public class FcsResponse<TCommand> : FcsMessageBase<TCommand>
        where TCommand : class, new()
    {
    }
}
