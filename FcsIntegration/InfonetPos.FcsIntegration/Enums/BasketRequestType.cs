using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPos.FcsIntegration.Enums
{
    public enum BasketRequestType
    {
        None,
        Create,
        Cancel,
        Remove,
        Clear,
        Hold
    }
}
