using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPOS.Core.Entities;
using InfonetPOS.Core.Enums;
using InfonetPOS.Core.Helpers;
using InfonetPOS.Core.Interfaces;
using MvvmCross.Logging;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.ViewModels
{
    public class PaymentStatusViewModel : MvxViewModel<int>
    {
        public int PumpId { get; set; }
        System.Timers.Timer goToHomeTimer;
        public string paymentApprovalStatus { get; set; }
        public string Language => App.Language.ToString();

        public bool PromptToPump
        {
            get => paymentApprovalStatus == PaymentApprovalStatus.PaymentApproved.ToString()
                   && App.CurrentFeature != FeatureType.Postpay;
        }

        public override void Prepare(int parameter)
        {
            log.Debug("PaymentStatusViewModel: Receiving pumpid:{0} as parameter.", parameter);
            PumpId = parameter;
        }

        public override async Task Initialize()
        {
            log.Debug("PaymentStatusViewModel: ViewModel is initializing.");
            await base.Initialize();
            SetPaymentApprovalStatus();
        }

        private void SetPaymentApprovalStatus()
        {
            log.Debug("PaymentStatusViewModel: Setting payment approval status.");
            if (posManager.ActiveSale.RefundReason != RefundReason.None)
            {
                paymentApprovalStatus = posManager.ActiveSale.RefundReason.ToString();
            }
            else
            {
                paymentApprovalStatus = posManager.ActiveSale.PaymentStatus.ToString();
            }
            log.Debug("PaymentStatusViewModel: PaymentStatus is:{0}", paymentApprovalStatus);
        }

        private readonly PosManager posManager;
        private readonly IMvxLog log;
        private readonly IAppSettings appSettings;
        private readonly IMvxNavigationService navigationService;
        private readonly IFcsService fcsService;
        private readonly IMessageBoxService messageBoxService;

        public PaymentStatusViewModel(PosManager posManager,
            IMvxLog log,
            IAppSettings appSettings,
            IMvxNavigationService navigationService,
            IFcsService fcsService,
            IMessageBoxService messageBoxService)
        {
            this.posManager = posManager;
            this.log = log;
            this.appSettings = appSettings;
            this.navigationService = navigationService;
            this.fcsService = fcsService;
            this.messageBoxService = messageBoxService;
        }

        public override async void ViewAppearing()
        {
            log.Debug("PaymentStatusViewModel: View is appearing.");
            base.ViewAppearing();
            App.CultureChange += OnCultureChangeAsync;

            if (App.CurrentFeature == FeatureType.Prepay)
            {
                log.Debug("PaymentStatusViewModel: Feature type is prepay.Process prepay payment.");
                await OnPrepayStatus();
            }
            else if (App.CurrentFeature == FeatureType.Postpay)
            {
                log.Debug("PaymentStatusViewModel: Feature type is postpay.Process postpay.");
                await OnPostPayStatus();
            }
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo cultureInfo)
        {
            await RaiseAllPropertiesChanged();
        }

        public override void ViewDisappearing()
        {
            log.Debug("PaymentStatusViewModel: View is disappearing.");
            base.ViewDisappearing();
            App.CultureChange -= OnCultureChangeAsync;
        }

        private async Task OnPostPayStatus()
        {
            if (posManager.ActiveSale.PaymentStatus == PaymentApprovalStatus.PaymentApproved)
            {
                log.Debug("PaymentStatusViewModel: Payment is approved for postpay.CleanUp sale.");
                await posManager.CleanUpSale(posManager.ActiveSale, PumpId, true);
            }
            else
            {
                log.Debug("PaymentStatusViewModel: Payment is not approved for postpay.Cancel hold basket.");
                await CancelHoldPostpayBasket();
            }
            log.Debug("PaymentStatusViewModel: If postpay completed, then navigate to welcome page.");
            NavigateToHome();
        }

        private async Task CancelHoldPostpayBasket()
        {
            try
            {
                log.Debug("PaymentStatusViewModel: Canceling hold postpay Basket : {0}.", posManager?.ActiveSale?.SoldBasket);
                var response = await fcsService?.CancelHoldBasket(posManager?.ActiveSale?.SoldBasket?.BasketID);
                if (response != null)
                {
                    if (response.ResultOK)
                    {
                        log.Debug("PaymentStatusViewModel: Cancel hold postpay basket successfully.");
                    }
                    else
                    {
                        log.Error("PaymentStatusViewModel: Cancel hold basket failed for postpay.");
                        messageBoxService.ShowMessageBox("CancelHoldBasketError", MessageBoxButtonType.OK);
                    }
                }
                else
                {
                    log.Error("PaymentStatusViewModel: Cancel hold basket response is null for postpay.");
                }
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("PaymentStatusViewModel: exception: {0}", ex.Message), ex);
            }
        }

        private async Task OnPrepayStatus()
        {
            if (posManager.ActiveSale.PaymentStatus == PaymentApprovalStatus.PaymentApproved)
            {
                log.Debug("PaymentStatusViewModel: If payment approved,navigate to welcome receipt.");
                NavigateToHome();
            }
            else if (posManager.ActiveSale.PaymentStatus == PaymentApprovalStatus.PaymentNotApproved)
            {
                log.Debug("PaymentStatusViewModel: If payment is not approved and pump is idle, then navigate to welcome page.");
                await posManager.CleanUpSale(posManager.ActiveSale, PumpId, false);
                NavigateToHome();
            }
        }

        private void NavigateToHome()
        {
            log.Debug("PaymentStatusViewModel: After 5 sec. go to welcome view model");
            goToHomeTimer = new System.Timers.Timer(appSettings.TimerInterval)
            {
                AutoReset = false
            };

            goToHomeTimer.Elapsed += async (sender, eventArgs) =>
            {
                await this.navigationService.Navigate<HomeViewModel>();
            };
            goToHomeTimer.Enabled = true;
        }
    }
}
