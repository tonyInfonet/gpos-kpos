using InfonetPOS.Core.DB.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.DB.Interface
{
    public interface IDBAccess
    {
        List<string> GetPosIpAddresses();
        List<int> GetAllShifts();
        List<string> GetAllUsers();
        List<Till> GetAvailableTills();
        List<Till> GetActiveTills(string userName);
        bool StartTill(int tillNo);
        bool StopTill(int tillNo);
        bool OpenTill(string userName, int shiftNo, int tillNo, DateTime? shiftDate);
        bool CloseTill(int tillNo);
        bool CSCTillsMoveRecords(DateTime? shiftDate, int tillNo);
        List<Tender> GetTenders();
        Company GetCompany();
        string GetInvoiceNo();
        string GetPassword(string U_CODE);
    }
}
