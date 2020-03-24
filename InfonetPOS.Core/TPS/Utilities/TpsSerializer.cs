using CsvHelper;
using CsvHelper.Configuration.Attributes;
using InfonetPOS.Core.TPS.Entities;
using MvvmCross.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.TPS.Utilities
{
    public class TpsSerializer
    {
        #region Singleton implementation
        private static TpsSerializer tpsSerializer;
        private readonly IMvxLog log;
        #endregion
       
        public TpsSerializer(IMvxLog log)
        {
            this.log = log;
        }

        public string SerializeTpsRequest(TpsRequest tpsRequest, bool appendEndData = true)
        {
            log.Debug("TpsSerializer: Serializing tps request.");
            StringBuilder csvBuilder = new StringBuilder();

            using (var writer = new StringWriter(csvBuilder))
            using (var csvWriter = new CsvWriter(writer))
            {
                csvWriter.Configuration.HasHeaderRecord = false;
                csvWriter.WriteRecord(tpsRequest);
            }

            if (appendEndData)
            {
                csvBuilder.Append(",END-DATA");
            }

            log.Debug("TpsSerializer: Returning serialized data.");
            return csvBuilder.ToString();
        }

        public TpsResponse DeSerializeTpsResponse(string csvTpsResponse)
        {
            log.Debug("TpsSerializer: DeSerializing tps response.");
            var tpsResponse = new TpsResponse();
            string value = null;

            if (!string.IsNullOrWhiteSpace(csvTpsResponse))
            {
                var fields = csvTpsResponse.Split(',');
                log.Debug("TpsSerializer: Using reflection to get properties.");
                PropertyInfo[] properties = typeof(TpsResponse).GetProperties();
                foreach (PropertyInfo property in properties)
                {
                    if (Attribute.GetCustomAttribute(property, typeof(IndexAttribute)) is IndexAttribute attribute) // This property has an IndexAttribute
                    {
                        if (attribute.Index < fields.Length)
                        {
                            value = fields[attribute.Index];

                            // Set the value of the property
                            property.SetValue(tpsResponse, value, null);
                        }
                    }
                }
            }

            log.Debug("TpsSerializer: Returning DeSerialized tps response.");
            return tpsResponse;
        }
    }
}
