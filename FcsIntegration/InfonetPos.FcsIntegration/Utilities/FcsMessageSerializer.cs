using MvvmCross.Logging;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Linq;
using InfonetPos.FcsIntegration.Entities;
using InfonetPos.FcsIntegration.Entities.SiteConfiguration;
using InfonetPos.FcsIntegration.Helpers;
using InfonetPos.FcsIntegration.Entities.SetPump;
using InfonetPos.FcsIntegration.Entities.Report;
using InfonetPos.FcsIntegration.Entities.Receipt;
using System.Xml;
using InfonetPos.FcsIntegration.Entities.SetPrice;

namespace InfonetPos.FcsIntegration.Utilities
{
    public class FcsMessageSerializer
    {
        private static FcsMessageSerializer instance;
        private byte sequenceNumber;
        private readonly IMvxLog log;
        private FcsMessageSerializer()
        {
            this.sequenceNumber = 1;//FCS expects sequence number from 1.
        }

        //public static FcsMessageSerializer Instance
        //{
        //    get
        //    {
        //        if (instance == null)
        //        {
        //            instance = new FcsMessageSerializer();
        //        }
        //        return instance;
        //    }
        //}

        public FcsMessageSerializer(IMvxLog log)
        {
            this.log = log;
        }

        public byte[] Serialize(string xmlCommand)
        {
            log.Debug("FcsMessageSerializer: Serializing xml command.");
            log.Debug("FcsMessageSerializer:Sending xml Message to Fcs: {0}", xmlCommand);
            byte[] bytes = null;

            BinaryFormatter binFmt = new BinaryFormatter();
            using (MemoryStream memStream = new MemoryStream(xmlCommand.Length + 4))
            {
                BinaryWriter binWriter = new BinaryWriter(memStream, Encoding.UTF8);
                byte headerStart = 0X01;
                binWriter.Write(headerStart);
                binWriter.Write(sequenceNumber++);
                if (sequenceNumber == 128)
                {
                    // Maximum sequence number is 127
                    sequenceNumber = 1;
                }
                binWriter.Write(xmlCommand.Length);
                binWriter.Write(xmlCommand);
                binWriter.Write(0X04);

                bytes = memStream.GetBuffer();
            }
            return bytes;
        }

        public byte[] Serialize<TFcsMessage>(TFcsMessage command)
            where TFcsMessage : class, new()
        {
            log.Debug("FcsMessageSerializer: Serializing Fcs Message..");
            var xmlCommand = XmlHelper.Serialize<TFcsMessage>(command);

            log.Debug("FcsMessageSerializer: Return serialized data");
            return Serialize(xmlCommand);
        }

        private void ReplaceDescendantAttributeName(XElement root, string descendantName,
            string oldAttributeName, string newAttributeName)
        {
            var descendants = root.Descendants(descendantName).ToList();
            descendants.ForEach(element =>
            {
                var attList = element.Attributes().ToList();
                var oldAtt = attList.Where(p => p.Name == oldAttributeName).SingleOrDefault();
                if (oldAtt != null)
                {
                    XAttribute newAtt = new XAttribute(newAttributeName, oldAtt.Value);
                    attList.Add(newAtt);
                    attList.Remove(oldAtt);
                    element.ReplaceAttributes(attList);
                }
            });
        }

        public FcsMessageDeserializationResult Deserialize(byte[] data)
        {
            log.Debug("FcsMessageSerializer: Deserializing Message..");
            FcsMessageDeserializationResult result = new FcsMessageDeserializationResult();
            int processedBytes = 0;

            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(memoryStream);

                try
                {
                    byte headerStart = reader.ReadByte();
                    processedBytes++;
                    if (headerStart != 0X01)
                    {
                        //Todo: handle error
                        log.Error("FcsMessageSerializer: Error in headerStart byte {0}", headerStart);
                    }
                    byte sequenceNumber = reader.ReadByte();
                    processedBytes++;
                    byte[] bytes = reader.ReadBytes(4);
                    processedBytes += 4;
                    bytes = bytes.Reverse().ToArray();
                    int xmlLength = BitConverter.ToInt32(bytes, 0);
                    log.Debug("FcsMessageSerializer: Seq#: {0}, XmlLength: {1}", sequenceNumber, xmlLength);
                    bytes = reader.ReadBytes(xmlLength);
                    processedBytes += xmlLength;
                    string xmlMessage = System.Text.Encoding.UTF8.GetString(bytes);
                    log.Debug("FcsMessageSerializer: Received xml message from Fcs: {0}", xmlMessage);

                    if (xmlLength != xmlMessage.Length)
                    {
                        //Todo: something wrong
                    }

                    byte messageEnd = reader.ReadByte();
                    processedBytes++;

                    if (messageEnd != 0x04)
                    {
                        //Todo: something wrong
                        log.Error("FcsMessageSerializer: Message end ({0} didn't match", messageEnd);
                    }

                    result.IsSuccessful = true;

                    XElement messageElement = XElement.Parse(xmlMessage);
                    XElement commandElement = messageElement.FirstNode as XElement;
                    if (messageElement.Name == "FCSCommand")
                    {
                        log.Debug("FcsMessageSerializer: Message element name:FcsCommand.");
                        if (commandElement.Name == "Basket")
                        {
                            log.Debug("FcsMessageSerializer: Command element name:Basket.");
                            FcsCommand<BasketRequest> fcsCommand = new FcsCommand<BasketRequest>();
                            fcsCommand.XmlCommand = commandElement;
                            result.MessageType = FcsMessageType.BasketCommand;
                            result.FcsMessage = fcsCommand;
                        }
                        else if (commandElement.Name == "Configuration")
                        {
                            log.Debug("FcsMessageSerializer: Command element name: Configuration");
                            FcsCommand<ConfigurationRequest> fcsCommand = new FcsCommand<ConfigurationRequest>();

                            //Todo: We need to remove this after FCS is fixed
                            ReplaceDescendantAttributeName(commandElement, "Grade", "Type", "type");

                            fcsCommand.XmlCommand = commandElement;
                            result.MessageType = FcsMessageType.ConfigurationCommand;
                            result.FcsMessage = fcsCommand;
                        }
                    }
                    else if (messageElement.Name == "FCSResponse")
                    {
                        log.Debug("FcsMessageSerializer: Message element name:FcsResponse.");
                        if (commandElement.Name == "SignOnResp")
                        {
                            log.Debug("FcsMessageSerializer: Command element name:SignOnResp.");
                            FcsResponse<SignOnResponse> fcsResponse = new FcsResponse<SignOnResponse>();
                            fcsResponse.XmlCommand = commandElement;
                            result.MessageType = FcsMessageType.SignOnResponse;
                            result.FcsMessage = fcsResponse;
                        }
                        else if (commandElement.Name == "SignOnTPOSResp")
                        {
                            log.Debug("FcsMessageSerializer: Command element name:SignOnTPOSResp.");
                            FcsResponse<SignOnTPosResponse> fcsResponse = new FcsResponse<SignOnTPosResponse>();
                            fcsResponse.XmlCommand = commandElement;
                            result.MessageType = FcsMessageType.SignOnTPosResponse;
                            result.FcsMessage = fcsResponse;
                        }
                        else if (commandElement.Name == "SignOffTPOSResp")
                        {
                            log.Debug("FcsMessageSerializer: Command element name:SignOffTPOSResp.");
                            FcsResponse<SignOffTPosResponse> fcsResponse = new FcsResponse<SignOffTPosResponse>();
                            fcsResponse.XmlCommand = commandElement;
                            result.MessageType = FcsMessageType.SignOffTPosResponse;
                            result.FcsMessage = fcsResponse;
                        }
                        else if (commandElement.Name == "PrepayResp")
                        {
                            log.Debug("FcsMessageSerializer: Command element name:PrepayResp.");
                            FcsResponse<PrepayResponse> fcsResponse = new FcsResponse<PrepayResponse>();
                            fcsResponse.XmlCommand = commandElement;
                            result.MessageType = FcsMessageType.PrepayResponse;
                            result.FcsMessage = fcsResponse;
                        }
                        else if (commandElement.Name == "BasketResp")
                        {
                            log.Debug("FcsMessageSerializer: Command element name:BasketResp.");
                            FcsResponse<BasketResponse> fcsResponse = new FcsResponse<BasketResponse>();
                            fcsResponse.XmlCommand = commandElement;
                            result.MessageType = FcsMessageType.BasketResponse;
                            result.FcsMessage = fcsResponse;
                        }
                        else if (commandElement.Name == "SetPumpResp")
                        {
                            log.Debug("FcsMessageSerializer: Command element name: SetPumpResp");
                            FcsResponse<SetPumpResponse> fcsResponse = new FcsResponse<SetPumpResponse>();
                            fcsResponse.XmlCommand = commandElement;
                            result.MessageType = FcsMessageType.SetPumpResponse;
                            result.FcsMessage = fcsResponse;
                        }
                        else if (commandElement.Name == "SetPriceResp")
                        {
                            log.Debug("FcsMessageSerializer: Command element name: SetPriceResp");
                            FcsResponse<SetPriceResponse> fcsResponse = new FcsResponse<SetPriceResponse>();
                            fcsResponse.XmlCommand = commandElement;
                            result.MessageType = FcsMessageType.SetPriceResponse;
                            result.FcsMessage = fcsResponse;
                        }
                        else if (commandElement.Name == "GetReportResp")
                        {
                            log.Debug("FcsMessageSerializer: Command element name: GetReportResp");
                            FcsResponse<GetReportResponse> fcsResponse = new FcsResponse<GetReportResponse>();
                            fcsResponse.XmlCommand = commandElement;
                            result.MessageType = FcsMessageType.GetReportResponse;
                            result.FcsMessage = fcsResponse;
                        }
                        else if (commandElement.Name == "GetReceiptResp")
                        {
                            log.Debug("FcsMessageSerializer: Command element name: GetReceiptResp");
                            FcsResponse<GetReceiptResponse> fcsResponse = new FcsResponse<GetReceiptResponse>();
                            fcsResponse.XmlCommand = commandElement;
                            result.MessageType = FcsMessageType.GetReceiptResponse;
                            result.FcsMessage = fcsResponse;
                        }
                        else if (commandElement.Name == "GetReceiptDataResp")
                        {
                            log.Debug("FcsMessageSerializer: Command element name: GetReceiptDataResp");
                            FcsResponse<GetReceiptDataResponse> fcsResponse = new FcsResponse<GetReceiptDataResponse>();
                            fcsResponse.XmlCommand = commandElement;
                            result.MessageType = FcsMessageType.GetReceiptDataResponse;
                            result.FcsMessage = fcsResponse;
                        }
                    }
                    else if (messageElement.Name == "FCSEvent")
                    {
                        log.Debug("FcsMessageSerializer: Message element name:FcsEvent.");
                        if (commandElement.Name == "Status")
                        {
                            log.Debug("FcsMessageSerializer: Command element name:Status.");
                            FcsEvent<FcsStatus> fcsEvent = new FcsEvent<FcsStatus>();
                            fcsEvent.XmlCommand = commandElement;
                            result.MessageType = FcsMessageType.EventStatus;
                            result.FcsMessage = fcsEvent;
                        }
                        else if(commandElement.Name == "PriceChanged")
                        {
                            log.Debug("FcsMessageSerializer: Command element name:PriceChanged.");
                            FcsEvent<PriceChangedEvent> fcsEvent = new FcsEvent<PriceChangedEvent>();
                            fcsEvent.XmlCommand = commandElement;
                            result.MessageType = FcsMessageType.EventPriceChanged;
                            result.FcsMessage = fcsEvent;
                        }
                        else if (commandElement.Name == "Prepay")
                        {
                            log.Debug("FcsMessageSerializer: Command element name:Prepay.");
                            FcsEvent<PrepayEvent> fcsEvent = new FcsEvent<PrepayEvent>();
                            fcsEvent.XmlCommand = commandElement;
                            result.MessageType = FcsMessageType.EventPrepay;
                            result.FcsMessage = fcsEvent;
                        }
                    }
                }
                catch (EndOfStreamException eos)
                {
                    log.DebugException("FcsMessageSerializer: exception{0}.", eos);
                    processedBytes = 0;
                    result.IsSuccessful = false;
                }
                catch(XmlException xmlException)
                {
                    log.DebugException("FcsMessageSerializer: exception{0}.", xmlException);
                    processedBytes = 0;
                    result.IsSuccessful = false;
                }
            }

            result.ByteProcessed = processedBytes;
            log.Debug("FcsMessageSerializer: Return DeSerialized data.");
            return result;
        }

    }
}
