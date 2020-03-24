using InfonetPos.FcsIntegration.Entities.SiteConfiguration;
using InfonetPOS.Core.Enums;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.Entities
{
    [AddINotifyPropertyChangedInterface]
    public class GradeDetails
    {
        public DecimalPlace Decimal { get; set; }
        public Grade CurrentData { get; set; }

        public double NewPrice { get; set; }
        public string NewPriceStr
        {
            get
            {
                if (NewPrice > 0)
                    return DecimalFormatter.FormatStr(NewPrice, Decimal);
                return null;
            }
        }

        public string CurrentPriceStr
        {
            get
            {
                if (CurrentData != null && CurrentData.Price != null)
                    return DecimalFormatter.FormatStr(CurrentData.Price.UnitPrice, Decimal);
                return null;
            }
        }
    }
}
