using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPOS.Core.Helpers;
using InfonetPOS.Core.Interfaces;
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
    public class RefundStatusViewModel : MvxViewModel
    {
        private readonly IMvxNavigationService navigationService;
        private readonly IMvxLog log;
        private readonly PosManager posManager;
        private readonly IFcsService fcsService;
        private readonly IAppSettings appSettings;
        private readonly IMessageBoxService messageBoxService;


        #region UI Properties
        public bool IsRefundApproved { get; set; }
        public string MessageKey
        {
            get
            {
                if (App.CurrentFeature == Enums.FeatureType.DeletePrepay && IsRefundApproved)
                    return "PrepayRemoved";
                return null;
            }
        }
        public string Language => App.Language.ToString();
        #endregion

        #region ViewModel private properties
        System.Timers.Timer goToHomeTimer;
        #endregion

        public RefundStatusViewModel(IMvxNavigationService navigationService,
                                      IMvxLog log,
                                      PosManager posManager,
                                      IFcsService fcsService,
                                      IAppSettings appSettings,
                                      IMessageBoxService messageBoxService)
        {
            this.navigationService = navigationService;
            this.log = log;
            this.posManager = posManager;
            this.fcsService = fcsService;
            this.appSettings = appSettings;
            this.messageBoxService = messageBoxService;
        }

        public override async void ViewAppeared()
        {
            log.Debug("RefundStatusViewModel: View is appeared.");
            App.CultureChange += OnCultureChangeAsync;

            if (posManager.ActiveSale.RefundStatus == Enums.RefundApprovalStatus.RefundApproved)
            {
                this.IsRefundApproved = true;
            }
            else
            {
                this.IsRefundApproved = false;
            }

            if (IsRefundApproved)
            {
                log.Debug("RefundStatusViewModel: Refund is approved.");
                await OnRefundApproved();
            }
            else
            {
                log.Debug("RefundStatusViewModel: Refund is not approved.");
                await OnRefundNotApproved();
            }

            await GoToHome();
            base.ViewAppeared();
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo cultureInfo)
        {
            await RaiseAllPropertiesChanged();
        }

        public override void ViewDisappeared()
        {
            log.Debug("RefundStatusViewModel: View is disappeared.");
            base.ViewDisappeared();
            App.CultureChange -= OnCultureChangeAsync;
        }

        #region All private Methods
        private async Task OnRefundApproved()
        {
            if (App.CurrentFeature == Enums.FeatureType.PrepayRefund
                || App.CurrentFeature == Enums.FeatureType.DeletePrepay)
            {
                log.Debug("RefundStatusViewModel: Refund approved for feature type PrepayRefund or DeletePrepay.");
                await OnPrepayRefundApproved();
            }
            else
            {
                log.Debug("RefundStatusViewModel: Refund approved for PrepayBasketRefund.Clean pump Active sale.");
                await posManager.CleanUpSale(posManager.ActiveSale, posManager.ActiveSale.PumpId, true);
            }
        }

        private async Task OnRefundNotApproved()
        {
            if (App.CurrentFeature == Enums.FeatureType.PrepayBasketRefund)
            {
                log.Debug("RefundStatusViewModel: Feature type is prepay basket refund.Cancel Basket hold.");
                await CancelBasketHold();
            }
            else if (App.CurrentFeature == Enums.FeatureType.DeletePrepay)
            {
                log.Debug("RefundStatusViewModel: Feature type is delete prepay.Cancel prepay delete.");
                await CancelPrepayDelete();
            }
            else if (App.CurrentFeature == Enums.FeatureType.PrepayRefund)
            {
                log.Debug("RefundStatusViewModel: Feature type is PrepayRefund.Cancel PrepayRefund.");
                await CancelPrepayRefund();
            }
        }

        private async Task CancelBasketHold()
        {
            log.Debug("RefundStatusViewModel: Refund not approved.Cancel hold basket.");
            var response = await fcsService.CancelHoldBasket(posManager.ActiveSale.SoldBasket.BasketID);
            if (response.ResultOK)
            {
                log.Debug("RefundStatusViewModel: Successfully cancel hold basket with basketID: {0}.", posManager.ActiveSale.SoldBasket.BasketID);
            }
            else
            {
                log.Error("RefundStatusViewModel: Cancel hold basket failure with basketID: {0}.", posManager.ActiveSale.SoldBasket.BasketID);
            }
        }

        private async Task GoToHome()
        {
            log.Debug("RefundStatusViewModel: After 5 sec. go to home view model");
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

        private async Task OnPrepayRefundApproved()
        {
            log.Debug("RefundStatusViewModel: Refund approved for prepay remove. Clean up sale.");
            await posManager.CleanUpSale(posManager.ActiveSale,
                posManager.ActiveSale.PumpId, false);
        }

        private async Task CancelPrepayDelete()
        {
            if ((await fcsService.PrepayCancelHoldRemoveAsync(posManager.ActiveSale.PumpId)).ResultOk)
            {
                posManager.ResetPrepayDelete(posManager.ActiveSale);
            }
        }

        private async Task CancelPrepayRefund()
        {
            log.Debug("RefundStatusViewModel: Canceling prepau refund as refund is not approved.");
            messageBoxService.ShowMessageBox("RefundNotApprovedForPrepayRefund", MessageBoxButtonType.OK);
            await posManager.CleanUpSale(posManager.ActiveSale,
               posManager.ActiveSale.PumpId, false);
            await GoToHome();
        }

        #endregion
    }
}
