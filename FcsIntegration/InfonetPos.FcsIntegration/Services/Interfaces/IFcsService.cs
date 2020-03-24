using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InfonetPos.FcsIntegration.Entities;
using InfonetPos.FcsIntegration.Entities.Receipt;
using InfonetPos.FcsIntegration.Entities.Report;
using InfonetPos.FcsIntegration.Entities.SetPrice;
using InfonetPos.FcsIntegration.Entities.SetPump;
using InfonetPos.FcsIntegration.Entities.SiteConfiguration;
using InfonetPos.FcsIntegration.Enums;

namespace InfonetPos.FcsIntegration.Services.Interfaces
{
    public interface IFcsService
    {
        bool IsConnected { get; }
        FcsStatus CurrentFcsStatus { get; set; }
        ConfigurationRequest FCSConfig { get; set; }
        event EventHandler<bool> ConnectionStatusChanged;
        event EventHandler<FcsStatus> FcsStatusChanged;
        event EventHandler<PriceChangedEvent> FuelPriceChanged;
        event EventHandler<BasketRequest> FcsReceivedBasketEvent; // Event Handler that subscribes the receiceved basket event from fcs.
        event EventHandler<ConfigurationRequest> FcsReceivedConfigurationEvent;

        Task ConnectAsync(string ipAddress, int port);
        Task<SignOnResponse> SignOnAsync(string posID, string version);
        Task<SignOnTPosResponse> SignOnTPosAsync(string posId, string version, POSType posType = POSType.TPOS, string userId = "", int tillId = 0, int shift = 0);
        Task<SignOffTPosResponse> SignOffTPosAsync(string posId);
        Task<PrepayResponse> PrepaySwitchAsync(int pumpID, int oldPumpId);
        Task<PrepayResponse> PrepayHoldAsync(int pumpID);
        Task<PrepayResponse> PrepaySetAsync(int pumpID, double amount, string invoiceID, string posID, double totalPaid = 0.0, double change = 0.0, string receipt = "");
        Task<PrepayResponse> PrepayCancelHoldAsync(int pumpID);
        Task<PrepayResponse> PrepayHoldRemoveAsync(int pumpID);
        Task<PrepayResponse> PrepayRemoveAsync(int pumpID);
        Task<PrepayResponse> PrepayCancelHoldRemoveAsync(int pumpID);
        Task<BasketResponse> RemoveBasket(BasketDetail basketDetail, double totalPaid = 0.0, double change = 0.0, string receipt = "", string invoiceNo = null);
        Task<BasketResponse> HoldBasket(string basketID);
        Task<BasketResponse> CancelHoldBasket(string basketID);
        Task<BasketResponse> HoldAndRemoveBasket(BasketDetail basketDetail, double totalPaid = 0.0, double change = 0.0, string receipt = "", string invoiceNo = null);
        Task<SetPumpResponse> StartPump(int pumpID);
        Task<SetPumpResponse> StopPump(int pumpID);
        Task<SetPumpResponse> SetPumpOnNow(int pumpID);
        Task<SetPumpResponse> AuthorizePump(int pumpID, string grade = "All", string payType = "Credit");
        Task<GetReportResponse> GetReport(ReportType reportType, GetReportCriteria criteria);
        Task<GetReceiptResponse> GetReceipt(ReceiptType receiptType, GetReceiptCriteria criteria);
        Task<GetReceiptDataResponse> GetReceiptData(ReceiptType receiptType, string invoiceNumber);
        Task<SetPriceResponse> SetPrice(List<PriceChange> changes);
    }
}
