using InfonetPos.FcsIntegration.Enums;
using InfonetPOS.Core.Enums;
using System.Collections.Generic;

namespace InfonetPOS.Core.Interfaces
{
    public interface IAppSettings
    {
        string FcsIpAddress { get; }
        int FcsPort { get; }
        string TpsIpAddress { get; }
        int TpsPort { get; }
        string PosId { get; }
        POSType PosType { get; }
        string Version { get; }
        List<int> PumpIds { get; }
        string DefaultLanguage { get; }
        string StoreName { get; }
        double Amount { get; }
        DecimalPlace FuelUnitPriceDecimal { get; }
        string PrinterFont { get; }
        int PrinterFontSize { get; }
        string CSCPumpDBConnectionString { get; }
        string CSCMasterDBConnectionString { get; }
        int TimerInterval { get; }
        string CSCAdminDBConnectionString { get; }
        string CSCTillsDBConnectionString { get; }
        string ReportReceiptFont { get; }
        float ReportReceiptFontSize { get; }
        bool CanChangeLanguageAlways { get; }
        int NoOfReceiptsToShow { get; }
        int DelayForTPSResponse { get; }
    }
}
