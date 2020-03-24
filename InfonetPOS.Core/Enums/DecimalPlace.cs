using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.Enums
{
    public enum DecimalPlace
    {
        N2,
        N3
    }

    public static class DecimalFormatter
    {
        public static string FormatStr(double value, DecimalPlace decimalPlace)
        {
            string ret = null;
            switch (decimalPlace)
            {
                case DecimalPlace.N2:
                case DecimalPlace.N3:
                    ret = value.ToString(decimalPlace.ToString());
                    break;
                default:
                    ret = value.ToString();
                    break;
            }
            return ret;
        }
    }
}
