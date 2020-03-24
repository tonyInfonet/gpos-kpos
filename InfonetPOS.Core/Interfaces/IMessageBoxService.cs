using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.Interfaces
{
    public enum MessageBoxResultType
    {
        None,
        Ok,
        Yes,
        No,
        Cancel
    } 

    public enum MessageBoxButtonType
    {
        OK,
        OKCancel,
        YesNo,
        YesNoCancel
    }

    public interface IMessageBoxService
    {
        MessageBoxResultType ShowMessageBox(string message, MessageBoxButtonType buttonType);
    }
}
