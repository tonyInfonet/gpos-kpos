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
using System.Threading;
using System.Threading.Tasks;

namespace InfonetPOS.Core.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class ConfirmationViewModel : MvxViewModel<string, bool>
    {
        private readonly IMvxNavigationService navigationService;
        private readonly IMvxLog log;

        public string Language => App.Language.ToString();
        public string Message { get; set; }

        public IMvxCommand ConfirmationCommand => new MvxCommand<bool>(async (result) =>
        {
            try
            {
                log.Debug("ConfirmationViewModel: User pressed confirm button.Close navigation.");
                await navigationService.Close(this, result);
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("ConfirmationViewModel: Exception: {0}.", ex.Message), ex);
            }
        });


        public ConfirmationViewModel(IMvxNavigationService navigationService,
                                    IMvxLog log)
        {
            this.navigationService = navigationService;
            this.log = log;
        }

        public override void Prepare(string message)
        {
            log.Debug("ConfirmationViewModel: Receiving message:{0} as parameter.", message);
            Message = message;
        }

        public override void ViewAppearing()
        {
            log.Debug("ConfirmationViewModel: View is appearing.");
            base.ViewAppearing();
            App.CultureChange += OnCultureChangeAsync;
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo cultureInfo)
        {
            await RaiseAllPropertiesChanged();
        }

        public override void ViewDisappearing()
        {
            log.Debug("ConfirmationViewModel:View is disappearing.");
            base.ViewDisappearing();
            App.CultureChange -= OnCultureChangeAsync;
        }
    }
}
