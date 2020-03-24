using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Helpers
{
    public class XmlHelper
    {
        public static string Serialize<T>(T obj, bool omitXmlDeclaration = false) 
            where T : class
        {
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = omitXmlDeclaration,
                CheckCharacters = false
            };

            using (var memoryStream = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(memoryStream, settings))
                {
                    var serializer = new XmlSerializer(typeof(T));

                    // removes namespace
                    var xmlns = new XmlSerializerNamespaces();
                    xmlns.Add(string.Empty, string.Empty);

                    serializer.Serialize(writer, obj, xmlns);
                    return Encoding.UTF8.GetString(memoryStream.ToArray());
                }
            }
        }

        public static T Deserialize<T>(byte[] xmlAsBytes)
        {
            using (var stream = new MemoryStream(xmlAsBytes))
            {
                var serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(stream);
            }
        }

        public static T Deserialize<T>(string xml)
        {
            var buffer = Encoding.UTF8.GetBytes(xml);
            return Deserialize<T>(buffer);
        }
    }
}
