using System.Xml.Linq;
using System.Xml.Serialization;
using InfonetPos.FcsIntegration.Utilities;


namespace InfonetPos.FcsIntegration.Entities
{
    [XmlRoot("FCSCommand")]
    public class FcsCommand<TCommand> : FcsMessageBase<TCommand>
        where TCommand : class, new()
    {
    }
}