using InfonetPOS.Core.Entities;

namespace InfonetPOS.Core.Helpers
{
    public interface IPrinter
    {
        void PrintReceipt(SaleStatus currentSale);
        void PrintReportReceipt(string data);
        void PrintDocument(string printString);
    }
}
