using InfonetPos.FcsIntegration.Enums;
using InfonetPOS.Core.Enums;
using InfonetPOS.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace InfonetPOS.WPF
{
    public class AppSettings : IAppSettings
    {
        #region App Config settings
        public List<int> pumpIds { get; set; }

        public string FcsIpAddress
        {
            get
            {
                return Properties.Settings.Default.FcsIpAddress;
            }
        }

        public int FcsPort
        {
            get
            {
                return Properties.Settings.Default.FcsPort;
            }
        }

        public string TpsIpAddress
        {
            get
            {
                return Properties.Settings.Default.TpsIpAddress;
            }
        }

        public int TpsPort
        {
            get
            {
                return Properties.Settings.Default.TpsPort;
            }
        }

        public string PosId
        {
            get
            {
                return Properties.Settings.Default.PosId;
            }
        }

        public string Version
        {
            get
            {
                return Properties.Settings.Default.Version;
            }
        }

        public List<int> PumpIds
        {
            get
            {
                if (pumpIds == null)
                {
                    string pumpId = Properties.Settings.Default.PumpIds;
                    string[] commaSeparatedPumpIds = pumpId.Split(',');
                    List<int> ResultedPumpIds = new List<int>();
                    foreach (var commaSeparatedPumpId in commaSeparatedPumpIds)
                    {
                        if (commaSeparatedPumpId.Contains('-'))
                        {
                            string[] hyphenSeparatedPumpIds = commaSeparatedPumpId.Split('-');
                            for (int i = int.Parse(hyphenSeparatedPumpIds[0]); i <= int.Parse(hyphenSeparatedPumpIds[1]); i++)
                            {
                                ResultedPumpIds.Add(i);
                            }
                        }
                        else
                        {
                            ResultedPumpIds.Add(int.Parse(commaSeparatedPumpId));
                        }
                    }
                    pumpIds = ResultedPumpIds;
                }
                return pumpIds;
            }
        }

        public string DefaultLanguage
        {
            get
            {
                return Properties.Settings.Default.DefaultLanguage;
            }
        }

        public string StoreName
        {
            get
            {
                return Properties.Settings.Default.StoreName;
            }
        }

        public double Amount
        {
            get
            {
                return Properties.Settings.Default.DefaultAmount;
            }
        }

        public DecimalPlace FuelUnitPriceDecimal
        {
            get
            {
                Enum.TryParse(Properties.Settings.Default.FuelUnitPriceDecimal.ToUpper(), out DecimalPlace ret);
                return ret;
            }
        }

        public string PrinterFont
        {
            get
            {
                return Properties.Settings.Default.PrinterFont;
            }
        }

        public int PrinterFontSize
        {
            get
            {
                return Properties.Settings.Default.PrinterFontSize;
            }
        }

        public POSType PosType
        {
            get
            {
                return (POSType)Enum.Parse(typeof(POSType), Properties.Settings.Default.PosType.ToUpper());
            }
        }

        public string CSCPumpDBConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["CSCPumpDBConnectionString"].ConnectionString;
            }
        }

        public string CSCMasterDBConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["CSCMasterDBConnectionString"].ConnectionString;
            }
        }

        public int TimerInterval
        {
            get
            {
                return Properties.Settings.Default.TimerInterval;
            }
        }

        public string CSCAdminDBConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["CSCAdminDBConnectionString"].ConnectionString;
            }
        }

        public string CSCTillsDBConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["CSCTillsDBConnectionString"].ConnectionString;
            }
        }

        public string ReportReceiptFont
        {
            get
            {
                return Properties.Settings.Default.ReportReceiptFont;
            }
        }

        public float ReportReceiptFontSize
        {
            get
            {
                return Properties.Settings.Default.ReportReceiptFontSize;
            }
        }

        public bool CanChangeLanguageAlways
        {
            get
            {
                return Properties.Settings.Default.CanChangeLanguageAlways;
            }
        }

        public int NoOfReceiptsToShow
        {
            get
            {
                return Properties.Settings.Default.NoOfReceiptsToShow;
            }
        }

        public int DelayForTPSResponse
        {
            get
            {
                return Properties.Settings.Default.DelayForTPSResponse;
            }
        }

        #endregion
    }
}
