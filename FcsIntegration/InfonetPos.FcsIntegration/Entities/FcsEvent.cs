using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities
{
    [XmlRoot("FCSEvent")]
    public class FcsEvent<TCommand> : FcsMessageBase<TCommand>
        where TCommand : class, new()
    {
    }
}
