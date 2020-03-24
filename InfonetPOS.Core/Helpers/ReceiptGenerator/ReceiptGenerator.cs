using InfonetPos.FcsIntegration.Entities;
using InfonetPOS.Core.Entities;
using InfonetPOS.Core.Enums;
using InfonetPOS.Core.Resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.Helpers.ReceiptGenerator
{
    public class ReceiptGenerator : IReceiptGenerator
    {
        private readonly PosManager posManager;
        private readonly AppLanguage language;
        private readonly CultureInfo english;
        private readonly CultureInfo arabic;
        private readonly DecimalPlace fuelUnitPriceDecimalPlace;
        public ReceiptGenerator(PosManager posManager,
            AppLanguage language,
            DecimalPlace fuelUnitPriceDecimalPlace)
        {
            this.posManager = posManager;
            this.language = language;
            this.fuelUnitPriceDecimalPlace = fuelUnitPriceDecimalPlace;
            this.english = new CultureInfo("en");
            this.arabic = new CultureInfo("ar");
        }

        private string Generate(bool setHeader, Action<StringWriter> printDetails)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (var streamWriter = new StringWriter(stringBuilder))
            {
                if (setHeader)
                {
                    posManager.PosCompany.Print(streamWriter);
                    streamWriter.WriteLine();
                }
                printDetails(streamWriter);
            }
            return stringBuilder.ToString();
        }

        private void Label(StringWriter receipt, string resourceKey)
        {
            if (language == AppLanguage.en)
            {
                receipt.Write("{0}: ",
                    AppResources.ResourceManager.GetString(resourceKey, english));
            }
            else if (language == AppLanguage.ar)
            {
                receipt.Write("{0}: ",
                    AppResources.ResourceManager.GetString(resourceKey, arabic));
            }
            else
            {
                receipt.WriteLine("{0} {1}:",
                    AppResources.ResourceManager.GetString(resourceKey, english),
                    AppResources.ResourceManager.GetString(resourceKey, arabic));
            }
        }

        private void FuelUnitPrice(StringWriter receipt, double price)
        {
            receipt.WriteLine(DecimalFormatter.FormatStr(price, fuelUnitPriceDecimalPlace));
        }

        private void FormatExchangeRate(StringWriter receipt, double exchangeRate)
        {
            if (exchangeRate != 1.00)
            {
                Label(receipt, "ExchangeRate");
                receipt.WriteLine("{0:0.00}", exchangeRate);
            }
        }

        private void FormatInvoiceNo(StringWriter receipt,string invoiceNo)
        {
            Label(receipt, "Invoice");
            receipt.WriteLine("{0}", invoiceNo);
        }

        private void FormatSaleDateTime(StringWriter receipt)
        {
            Label(receipt, "DateTime");
            receipt.WriteLine("{0}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
        }

        private void PostPay(StringWriter receipt, BasketDetail basket)
        {
            Label(receipt, "Pump");
            receipt.WriteLine("#{0} - {1}", basket.PumpID, basket.Grade);

            Label(receipt, "Volume");
            receipt.WriteLine("{0:0.000}", basket.Volume);

            Label(receipt, "Price");
            FuelUnitPrice(receipt, basket.UnitPrice);

            Label(receipt, "Total");
            receipt.WriteLine("{0:0.00}", basket.Amount);
        }

        public string PostPayCash(SaleStatus sale)
        {
            return Generate(true, receipt =>
            {
                var basket = sale.SoldBasket;

                PostPay(receipt, basket);

                FormatInvoiceNo(receipt, sale.InvoiceNo);
                FormatSaleDateTime(receipt);

                receipt.WriteLine();

                Label(receipt, "PaymentMethod");
                receipt.WriteLine("{0}", sale.SaleTender.Description);

                Label(receipt, "Paid");
                receipt.WriteLine("{0:0.00} ({1:0.00})", sale.TotalPaid / sale.SaleTender.ExchangeRate, sale.TotalPaid);

                FormatExchangeRate(receipt, sale.SaleTender.ExchangeRate);

                Label(receipt, "Change");
                receipt.WriteLine("{0:0.00}", sale.Change);
            });
        }

        public string PostPayCard(SaleStatus sale)
        {
            return Generate(true, receipt =>
            {
                var basket = sale.SoldBasket;

                PostPay(receipt, basket);

                FormatInvoiceNo(receipt, sale.InvoiceNo);
                FormatSaleDateTime(receipt);

                receipt.WriteLine();

                receipt.WriteLine(sale.SaleResponse.Receipt);
            });
        }

        private void Prepay(StringWriter receipt, SaleStatus sale)
        {
            Label(receipt, "Pump");
            receipt.WriteLine("#{0} - {1}", sale.PumpId, sale.SaleGrade.Type);

            Label(receipt, "Prepay");
            receipt.WriteLine("{0:0.00}", sale.Amount);

            Label(receipt, "Volume");
            receipt.WriteLine("{0:0.000}",
                    (sale.Amount / sale.SaleGrade.Price.UnitPrice));

            Label(receipt, "Price");
            FuelUnitPrice(receipt, sale.SaleGrade.Price.UnitPrice);
        }

        public string PrepayCash(SaleStatus sale)
        {
            return Generate(true, receipt =>
            {
                Prepay(receipt, sale);

                FormatInvoiceNo(receipt, sale.InvoiceNo);
                FormatSaleDateTime(receipt);

                receipt.WriteLine();

                Label(receipt, "PaymentMethod");
                receipt.WriteLine("{0}", sale.SaleTender.Description);

                Label(receipt, "Paid");
                receipt.WriteLine("{0:0.00} ({1:0.00})", sale.TotalPaid / sale.SaleTender.ExchangeRate, sale.TotalPaid);

                FormatExchangeRate(receipt, sale.SaleTender.ExchangeRate);

                Label(receipt, "Change");
                receipt.WriteLine("{0:0.00}", sale.Change);
            });
        }

        public string PrepayCard(SaleStatus sale)
        {
            return Generate(true, receipt =>
            {
                Prepay(receipt, sale);

                FormatInvoiceNo(receipt, sale.InvoiceNo);
                FormatSaleDateTime(receipt);

                receipt.WriteLine();

                receipt.WriteLine(sale.SaleResponse.Receipt);
            });
        }

        public string CardSaleFailure(SaleStatus sale)
        {
            return Generate(true, receipt =>
            {
                FormatInvoiceNo(receipt, sale.InvoiceNo);
                FormatSaleDateTime(receipt);
                receipt.WriteLine();
                receipt.WriteLine(sale.SaleResponse.Receipt);
            });
        }

        private void FullPrepayRefund(StringWriter receipt, SaleStatus sale)
        {
            Label(receipt, "Pump");
            receipt.WriteLine("#{0} - {1}", sale.PumpId, sale.SaleGrade.Type);

            Label(receipt, "Prepay");
            receipt.WriteLine("{0:0.00}", sale.Amount);

            receipt.WriteLine();

            Label(receipt, "Volume");
            receipt.WriteLine("0.000");

            Label(receipt, "Price");
            FuelUnitPrice(receipt, sale.SaleGrade.Price.UnitPrice);

            Label(receipt, "Total");
            receipt.WriteLine("0.00");

            FormatInvoiceNo(receipt, sale.InvoiceNo);
            FormatSaleDateTime(receipt);
        }

        public string FullPrepayRefundCard(SaleStatus sale)
        {
            return Generate(true, receipt =>
            {
                FullPrepayRefund(receipt, sale);

                receipt.WriteLine();

                receipt.WriteLine(sale.RefundResponse.Receipt);
            });
        }

        public string FullPrepayRefundCash(SaleStatus sale)
        {
            return Generate(true, receipt =>
            {
                FullPrepayRefund(receipt, sale);

                receipt.WriteLine();

                Label(receipt, "RefundMethod");
                receipt.WriteLine("{0}", sale.SaleTender.Description);

                Label(receipt, "Paid");
                receipt.WriteLine("{0:0.00} ({1:0.00})", sale.TotalPaid / sale.RefundTender.ExchangeRate, sale.TotalPaid);

                FormatExchangeRate(receipt, sale.RefundTender.ExchangeRate);

                Label(receipt, "Change");
                receipt.WriteLine("{0:0.00}", sale.Change);
            });
        }

        private void PartialPrepayRefund(StringWriter receipt, SaleStatus sale)
        {
            var basket = sale.SoldBasket;

            Label(receipt, "Pump");
            receipt.WriteLine("#{0} - {1}", basket.PumpID, basket.Grade);

            Label(receipt, "Prepay");
            receipt.WriteLine("{0:0.00}", basket.Prepay.PrepayAmount);

            receipt.WriteLine();

            Label(receipt, "Volume");
            receipt.WriteLine("{0:0.000}", basket.Volume);

            Label(receipt, "Price");
            FuelUnitPrice(receipt, basket.UnitPrice);

            Label(receipt, "Total");
            receipt.WriteLine("{0:0.00}", basket.Amount);

            FormatInvoiceNo(receipt, basket.Prepay.PrepayInvoice);
            FormatSaleDateTime(receipt);
        }

        public string PartialPrepayRefundCard(SaleStatus sale)
        {
            return Generate(true, receipt =>
            {
                PartialPrepayRefund(receipt, sale);

                receipt.WriteLine();

                receipt.WriteLine(sale.RefundResponse.Receipt);
            });
        }

        public string PartialPrepayRefundCash(SaleStatus sale)
        {
            return Generate(true, receipt =>
            {
                PartialPrepayRefund(receipt, sale);

                receipt.WriteLine();

                Label(receipt, "RefundMethod");
                receipt.WriteLine("{0}", sale.SaleTender.Description);

                Label(receipt, "Paid");
                receipt.WriteLine("{0:0.00} ({1:0.00})", sale.TotalPaid / sale.RefundTender.ExchangeRate, sale.TotalPaid);

                FormatExchangeRate(receipt, sale.RefundTender.ExchangeRate);

                Label(receipt, "Change");
                receipt.WriteLine("{0:0.00}", sale.Change);
            });
        }

        public string DriveOffOrPumpTest(SaleStatus sale)
        {
            return Generate(true, receipt =>
            {
                var basket = sale.SoldBasket;

                Label(receipt, "ReceiptType");
                receipt.WriteLine("#{0}", sale.SoldBasket.InvoiceType);
                receipt.WriteLine();
                PostPay(receipt, basket);
                FormatInvoiceNo(receipt, sale.InvoiceNo);
                FormatSaleDateTime(receipt);
            });
        }
    }
}
