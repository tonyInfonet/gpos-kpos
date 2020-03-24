using InfonetPOS.Core.Entities;
using InfonetPOS.Core.Interfaces;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;

namespace InfonetPOS.Core.Helpers
{
    public class Printer : IPrinter
    {
        private Font printFont;
        private StringReader streamToPrint;

        public Printer(IAppSettings appSettings)
        {
            printFont = new Font(appSettings.PrinterFont, appSettings.PrinterFontSize);
        }

        public void PrintReceipt(SaleStatus currentSale)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (var streamWriter = new StringWriter(stringBuilder))
            {
                if (currentSale.SaleResponse != null)
                {
                    streamWriter.Write(currentSale.SaleResponse.Receipt);
                }

                if (currentSale.SoldBasket != null)
                {
                    streamWriter.WriteLine("============================");
                    streamWriter.WriteLine("PUMP # : {0}-{1}", currentSale.SoldBasket.PumpID, currentSale.SoldBasket.Grade);
                    streamWriter.WriteLine("VOL : {0} L", currentSale.SoldBasket.Volume);
                    streamWriter.WriteLine("PRICE / L: SAR {0}", currentSale.SoldBasket.UnitPrice);
                    streamWriter.WriteLine("TOTAL: SAR {0}", currentSale.SoldBasket.Amount);
                }
                else if (currentSale.SaleType == SaleType.Prepay)
                {
                    streamWriter.WriteLine("============================");
                    streamWriter.WriteLine("PUMP # : {0}-{1}", currentSale.PumpId, currentSale.SaleGrade.Type);
                    streamWriter.WriteLine("VOL : {0:0.###} L",
                        (currentSale.Amount / currentSale.SaleGrade.Price.UnitPrice));
                    streamWriter.WriteLine("PRICE / L: SAR {0:0.###}", currentSale.SaleGrade.Price.UnitPrice);
                    streamWriter.WriteLine("TOTAL: SAR {0:0.###}", currentSale.Amount);

                }

                if (currentSale.RefundResponse != null)
                {
                    streamWriter.WriteLine("============================");
                    streamWriter.WriteLine("         REFUND");
                    streamWriter.WriteLine("============================");
                    streamWriter.Write(currentSale.RefundResponse.Receipt);
                }
            }

            PrintDocument(stringBuilder);

        }

        public void PrintReportReceipt(string data)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (var streamWriter = new StringWriter(stringBuilder))
            {
                if (data != null)
                {
                    streamWriter.Write(data);
                }
            }

            PrintDocument(stringBuilder);

        }

        private void PrintDocument(StringBuilder stringBuilder)
        {
            string printString = stringBuilder.ToString();
            PrintDocument(printString);
        }

        public void PrintDocument(string printString)
        {
            streamToPrint = new StringReader(printString);
            PrintDocument pd = new PrintDocument();
            pd.PrintPage += new PrintPageEventHandler(PrintPage);
            // Print the document.
            pd.Print();
        }

        private void PrintPage(object sender, PrintPageEventArgs ev)
        {
            float linesPerPage = 0;
            float yPos = 0;
            int count = 0;
            float leftMargin = 0; // ev.MarginBounds.Left;
            float topMargin = 0;  //ev.MarginBounds.Top;
            String line = null;

            // Calculate the number of lines per page.
            linesPerPage = ev.MarginBounds.Height /
               printFont.GetHeight(ev.Graphics);

            // Iterate over the file, printing each line.
            while (count < linesPerPage &&
               ((line = streamToPrint.ReadLine()) != null))
            {

                yPos = topMargin + (count * printFont.GetHeight(ev.Graphics));
                ev.Graphics.DrawString(line, printFont, Brushes.Black,
                   leftMargin, yPos, new StringFormat());
                count++;
            }

            // If more lines exist, print another page.
            if (line != null)
            {
                ev.HasMorePages = true;
            }
            else
            {
                ev.HasMorePages = false;

                streamToPrint.Close();
                streamToPrint.Dispose();
            }
        }
    }
}
