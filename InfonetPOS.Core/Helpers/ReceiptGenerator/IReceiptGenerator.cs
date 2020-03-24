using InfonetPOS.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.Helpers.ReceiptGenerator
{
    public interface IReceiptGenerator
    {
        string PostPayCash(SaleStatus sale);
        string PostPayCard(SaleStatus sale);
        string PrepayCash(SaleStatus sale);
        string PrepayCard(SaleStatus sale);
        string CardSaleFailure(SaleStatus sale);
        string FullPrepayRefundCard(SaleStatus sale);
        string FullPrepayRefundCash(SaleStatus sale);
        string PartialPrepayRefundCard(SaleStatus sale);
        string PartialPrepayRefundCash(SaleStatus sale);
        string DriveOffOrPumpTest(SaleStatus sale);
    }
}
