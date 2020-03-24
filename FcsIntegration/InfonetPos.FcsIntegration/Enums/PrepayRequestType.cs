using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPos.FcsIntegration.Enums
{
    public enum PrepayRequestType
    {
        Switch,
        Hold,
        Cancel,
        Set,
        HoldRemove,
        Remove,
        CancelHoldRemove
    }
}
