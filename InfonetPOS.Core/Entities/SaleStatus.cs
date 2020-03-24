using InfonetPos.FcsIntegration.Entities;
using InfonetPos.FcsIntegration.Entities.SiteConfiguration;
using InfonetPOS.Core.Enums;
using InfonetPOS.Core.TPS.Entities;
using System;
using System.Xml.Serialization;
using InfonetPOS.Core.DB.Entities;

namespace InfonetPOS.Core.Entities
{
    public enum SaleType
    {
        None,
        Prepay,
        Postpay,
        PrepayFromCashier
    };

    public class SaleStatus
    {
        public int PumpId { get; set; }
        public SaleType SaleType { get; set; }
        public double Amount { get; set; }
        public PaymentApprovalStatus PaymentStatus { get; set; }
        public RefundApprovalStatus RefundStatus { get; set; }

        [XmlIgnore]
        public BasketDetail SoldBasket { get; set; }

        public TpsResponse SaleResponse { get; set; }
        public TpsResponse RefundResponse { get; set; }
        public RefundReason RefundReason { get; set; }
        public bool IsPrepayHoldRemove { get; set; }
        public bool IsPrepaySet { get; set; }
        public bool IsPrepayHold { get; set; }
        public DateTime SaleTime { get; set; }
        public Grade SaleGrade { get; set; }
        public Tender SaleTender { get; set; }
        public Tender RefundTender { get; set; }
        public string InvoiceNo { get; set; }
        public double TotalPaid { get; set; }
        public double Change { get; set; }
        public string Receipt { get; set; }

        public void InitiateSale()
        {
            SaleType = SaleType.None;
            Amount = 0.0;
            PaymentStatus = PaymentApprovalStatus.PaymentNotApproved;
            RefundStatus = RefundApprovalStatus.None;
            SoldBasket = null;
            SaleResponse = null;
            RefundResponse = null;
            RefundReason = RefundReason.None;
            IsPrepayHoldRemove = false;
            IsPrepayHold = false;
            IsPrepaySet = false;
            SaleGrade = null;
            SaleTender = null;
            RefundTender = null;
        }

        public bool IsRefundAvailable
        {
            get
            {
                if (SaleType == SaleType.Prepay)
                {
                    return SoldBasket?.Prepay?.PrepayAmount > SoldBasket?.Amount;
                }
                else
                {
                    return SoldBasket?.Amount > 0;
                }
            }
        }

        public bool IsRefundPossibleForThisBasket(BasketDetail possibleBasket)
        {
            return SaleType == SaleType.Prepay
                && possibleBasket?.Prepay?.PrepayAmount > possibleBasket?.Amount;
        }

        public double GetAvailableRefund()
        {
            if (SoldBasket != null)
            {
                return Amount - SoldBasket.Amount;
            }
            else
            {
                return Amount;
            }
        }
    }
}
