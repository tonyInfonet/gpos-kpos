using System;

namespace InfonetPos.FcsIntegration.Utilities
{
    public enum FcsMessageType
    {
        None,
        SignOnResponse,
        EventStatus,
        EventPriceChanged,
        PrepayResponse,
        EventPrepay,
        BasketCommand,
        BasketResponse,
        SignOnTPosResponse,
        SignOffTPosResponse,
        ConfigurationCommand,
        SetPumpResponse,
        SetPriceResponse,
        GetReportResponse,
        GetReceiptResponse,
        GetReceiptDataResponse
    }

    public class FcsMessageDeserializationResult
    {
        public bool IsSuccessful { get; set; }
        public FcsMessageType MessageType { get; set; }
        public object FcsMessage { get; set; }
        public int ByteProcessed { get; set; }
    }
}