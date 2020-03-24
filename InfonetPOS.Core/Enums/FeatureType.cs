using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.Enums
{
    public enum FeatureType
    {
        None,
        Prepay,
        PrepayRefund,
        DeletePrepay,
        PrepayBasketRefund,
        Postpay,
        PumpTest,
        DriveOff
    }
}
