﻿using System.Xml.Linq;
using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Utilities
{
    public static class XObjectExtensions
    {
        public static T Deserialize<T>(this XContainer element)
        {
            using (var reader = element.CreateReader())
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                object result = serializer.Deserialize(reader);
                if (result is T)
                    return (T)result;
            }
            return default(T);
        }

        public static XElement SerializeToXElement<T>(this T obj, bool omitStandardNamespaces = false)
        {
            var doc = new XDocument();
            using (var writer = doc.CreateWriter())
            {
                XmlSerializerNamespaces ns = null;
                if (omitStandardNamespaces)
                    (ns = new XmlSerializerNamespaces()).Add("", ""); // Disable the xmlns:xsi and xmlns:xsd lines.
                XmlSerializer serializer = new XmlSerializer(obj.GetType());
                serializer.Serialize(writer, obj, ns);
            }
            var element = doc.Root;
            if (element != null)
                element.Remove();
            return element;
        }
    }
}
