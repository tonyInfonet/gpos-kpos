using InfonetPOS.Core.Interfaces;
using InfonetPOS.Core.Resources;
using MvvmCross;
using MvvmCross.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InfonetPOS.WPF.Controls
{
    public class MessageBoxService : IMessageBoxService
    {
        private Dictionary<MessageBoxResult, MessageBoxResultType> messageBoxResultDictionary = new Dictionary<MessageBoxResult, MessageBoxResultType>()
        {
            {MessageBoxResult.None, MessageBoxResultType.None },
            {MessageBoxResult.OK, MessageBoxResultType.Ok },
            {MessageBoxResult.Yes, MessageBoxResultType.Yes },
            {MessageBoxResult.No, MessageBoxResultType.No },
            {MessageBoxResult.Cancel, MessageBoxResultType.Cancel }
        };

        public MessageBoxResultType ShowMessageBox(string message, MessageBoxButtonType buttonType)
        {
            var log = Mvx.IoCProvider.Resolve<IMvxLog>();
            log.Debug("MessageBoxService: Show Message:{0}", message);

            MessageBoxResultType messageBoxResult = MessageBoxResultType.None;
            bool success = true;
            message = AppResources.ResourceManager.GetString(message, AppResources.Culture);

            if (buttonType == MessageBoxButtonType.OK)
            {
                success = messageBoxResultDictionary.TryGetValue(MessageBox.Show(message, "InfonetPOS", MessageBoxButton.OK), out messageBoxResult);

            }
            else if (buttonType == MessageBoxButtonType.OKCancel)
            {
                success = messageBoxResultDictionary.TryGetValue(MessageBox.Show(message, "InfonetPOS", MessageBoxButton.OKCancel), out messageBoxResult);
            }
            else if (buttonType == MessageBoxButtonType.YesNo)
            {
                success = messageBoxResultDictionary.TryGetValue(MessageBox.Show(message, "InfonetPOS", MessageBoxButton.YesNo), out messageBoxResult);
            }
            else if (buttonType == MessageBoxButtonType.YesNoCancel)
            {
                success = messageBoxResultDictionary.TryGetValue(MessageBox.Show(message, "InfonetPOS", MessageBoxButton.YesNoCancel), out messageBoxResult);
            }

            if (success)
            {
                log.Debug("MessageBoxService: Succeesfully showed MessageBox to user and return MessageBoxResult.");
                return messageBoxResult;
            }
            else
            {
                log.Error("MessageBoxService: Error while showing MessageBox.");
                return MessageBoxResultType.None;
            }
        }
    }
}
