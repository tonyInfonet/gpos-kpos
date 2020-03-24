using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.TPS.Entities
{
    public class TpsResponse
    {
        [Index(0)]
        public string TransactionType { get; set; }
        [Index(1)]
        public string PaymentType { get; set; }
        [Index(2)]
        public string CardType { get; set; }

        /// <summary>
        /// Invoice Number of a transaction
        /// </summary>
        [Index(3)]
        public string InvNo { get; set; }
        [Index(4)]
        public string SequenceNo { get; set; }
        [Index(5)]
        public string TransactionNo { get; set; }
        //[Index(6)]
        //public string UnknownIndex6 { get; set; }

        [Index(7)]
        public string TerminalID { get; set; }
        [Index(8)]
        public string Amount { get; set; }
        [Index(9)]
        public string LanguageCode { get; set; }
        [Index(10)]
        public string AccountTypeCode { get; set; }
        [Index(11)]
        public string FleetTrack2 { get; set; }
        //[Index(12)]
        //public string UnknownIndex12 { get; set; }
        //[Index(13)]
        //public string UnknownIndex13 { get; set; }

        [Index(14)]
        public string Result { get; set; }
        //[Index(15)]
        //public string UnknownIndex15 { get; set; }

        [Index(16)]
        public string BankApprovalCode { get; set; }
        [Index(17)]
        public string BankResponseCode { get; set; }

        //[Index(18)]
        //public string UnknownIndex18 { get; set; }
        //[Index(19)]
        //public string UnknownIndex19 { get; set; }
        [Index(20)]
        public string Date { get; set; }
        [Index(21)]
        public string Time { get; set; }
        [Index(22)]
        public string Response { get; set; }

        /// <summary>
        /// Masked Card No
        /// </summary>
        [Index(23)]
        public string CardNo { get; set; }

        //#region Unknown Index 24-27
        //[Index(24)]
        //public string UnknownIndex24 { get; set; }
        //[Index(25)]
        //public string UnknownIndex25 { get; set; }
        //[Index(26)]
        //public string UnknownIndex26 { get; set; }
        //[Index(27)]
        //public string UnknownIndex27 { get; set; }
        //#endregion

        [Index(28)]
        public string ISOResponse { get; set; }
        //[Index(29)]
        //public string UnknownIndex29 { get; set; }
        [Index(30)]
        public string Receipt { get; set; }

        //[Index(31)]
        //public string UnknownIndex31 { get; set; }
        [Index(32)]
        public string CardName { get; set; }

        [Index(33)]
        public string EndData { get; set; }

        [Ignore]
        public bool RequestApproved => string.Equals(Result, "Approved", StringComparison.OrdinalIgnoreCase);
        [Ignore]
        public bool HasEndDataTag => string.Equals(EndData, "END-DATA", StringComparison.OrdinalIgnoreCase);
    }
}
