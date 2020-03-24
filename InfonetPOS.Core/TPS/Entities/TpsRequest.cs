using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.TPS.Entities
{
    public class TpsRequest
    {
        /// <summary>
        /// Type of a transaction, i.e. SaleInside, RefundInside, EODTerminal
        /// </summary>
        [Index(0)]
        public string TransactionType { get; set; }

        /// <summary>
        /// Type of Card, i.e. Credit, Debit, Fleet
        /// </summary>
        [Index(1)]
        public string PaymentType { get; set; }

        /// <summary>
        /// Pump #
        /// </summary>
        [Index(2)]
        public int LaneNo { get; set; }

        /// <summary>
        /// Invoice Number of a transaction
        /// </summary>
        [Index(3)]
        public string InvoiceNo { get; set; }

        #region unknown indexes 4-7
        [Index(4)]
        public string UnknownIndex4 { get; set; }
        [Index(5)]
        public string UnknownIndex5 { get; set; }
        [Index(6)]
        public string UnknownIndex6 { get; set; }
        [Index(7)]
        public string UnknownIndex7 { get; set; }
        #endregion

        [Index(8)]
        public string Amount { get; set; }

        /// <summary>
        /// E or F (it is not important for TPS)
        /// </summary>
        [Index(9)]
        public char? LanguageCode { get; set; }

        [Index(10)]
        public string UnknownIndex10 { get; set; }

        /// <summary>
        /// Only for fleet
        /// </summary>
        [Index(11)]
        public string FleetTrack2 { get; set; }

        #region unknown indexes 12-36
        [Index(12)]
        public string UnknownIndex12 { get; set; }
        [Index(13)]
        public string UnknownIndex13 { get; set; }
        [Index(14)]
        public string UnknownIndex14 { get; set; }
        [Index(15)]
        public string UnknownIndex15 { get; set; }
        [Index(16)]
        public string UnknownIndex16 { get; set; }
        [Index(17)]
        public string UnknownIndex17 { get; set; }
        [Index(18)]
        public string UnknownIndex18 { get; set; }
        [Index(19)]
        public string UnknownIndex19 { get; set; }
        [Index(20)]
        public string UnknownIndex20 { get; set; }
        [Index(21)]
        public string UnknownIndex21 { get; set; }
        [Index(22)]
        public string UnknownIndex22 { get; set; }
        [Index(23)]
        public string UnknownIndex23 { get; set; }
        [Index(24)]
        public string UnknownIndex24 { get; set; }
        [Index(25)]
        public string UnknownIndex25 { get; set; }
        [Index(26)]
        public string UnknownIndex26 { get; set; }
        [Index(27)]
        public string UnknownIndex27 { get; set; }
        [Index(28)]
        public string UnknownIndex28 { get; set; }
        [Index(29)]
        public string UnknownIndex29 { get; set; }
        [Index(30)]
        public string UnknownIndex30 { get; set; }
        [Index(31)]
        public string UnknownIndex31 { get; set; }
        [Index(32)]
        public string UnknownIndex32 { get; set; }
        [Index(33)]
        public string UnknownIndex33 { get; set; }
        [Index(34)]
        public string UnknownIndex34 { get; set; }
        [Index(35)]
        public string UnknownIndex35 { get; set; }
        [Index(36)]
        public string UnknownIndex36 { get; set; }
        #endregion
        /// <summary>
        /// Only for fleet
        /// </summary>
        [Index(37)]
        public string FleetOptionalData { get; set; }
    }
}
