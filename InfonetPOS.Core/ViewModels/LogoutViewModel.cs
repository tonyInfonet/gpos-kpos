using InfonetPOS.Core.Helpers;
using InfonetPOS.Core.Interfaces;
using MvvmCross.Commands;
using MvvmCross.Logging;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class LogoutViewModel : MvxViewModel
    {
        public string Language => App.Language.ToString();

        private readonly IMvxNavigationService navigationService;
        private readonly IMvxLog log;
        private readonly PosManager posManager;
        private readonly IMessageBoxService messageBoxService;

        public LogoutViewModel(IMvxNavigationService navigationService,
            IMvxLog log,
            PosManager posManager,
            IMessageBoxService messageBoxService)
        {
            this.navigationService = navigationService;
            this.log = log;
            this.posManager = posManager;
            this.messageBoxService = messageBoxService;
        }

        public override void ViewAppearing()
        {
            log.Debug("LogoutViewModel: View is appearing.");
            base.ViewAppearing();
            App.CultureChange += OnCultureChangeAsync;
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo cultureInfo)
        {
            await RaiseAllPropertiesChanged();
        }

        public override void ViewDisappearing()
        {
            log.Debug("LogoutViewModel: View is disappearing.");
            base.ViewDisappearing();
            App.CultureChange -= OnCultureChangeAsync;
        }

        private async Task LogOut(bool closeTill)
        {
            log.Debug("LogoutViewModel:Started Logout process.");
            var result = posManager.PosLogout(closeTill);
            if (!result)
            {
                log.Error("LogoutViewModel: Logout failed.");
                log.Info("LogoutViewModel: Show MessageBox to user and inform him to retry.");
                var response = messageBoxService.ShowMessageBox("ConnectionLoseWhileLogout", MessageBoxButtonType.OK);
                log.Debug("LogoutViewModel: Close the navigation.");
                await navigationService.Close(this);
            }
            else
            {
                log.Debug("LogoutViewModel: Successful logout.");
                if (App.IsFcsConnected)
                {
                    log.Debug("LogoutViewModel: Fcs is connected. try to sign off.");
                    await App.SignOffAsync();
                }
                else
                {
                    log.Error("LogoutViewModel: Fcs is not connected.Go to LoginViewModel.");
                    await navigationService.Navigate<LoginViewModel>();
                }
            }
        }

        public IMvxCommand ConfirmTillCloseCmd => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("LogoutViewModel: User pressed Logout button and wants to close till.");
                await LogOut(true);
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("LogoutViewModel: Exception: {0}.", ex.Message), ex);
            }
        });

        public IMvxCommand CancelTillCloseCmd => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("LogoutViewModel: User pressed Logout button, but don't want to close till.");
                await LogOut(false);
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("LogoutViewModel: Exception: {0}.", ex.Message), ex);
            }
        });
    }
}
