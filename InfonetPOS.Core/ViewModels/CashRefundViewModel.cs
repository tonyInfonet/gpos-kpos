using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPOS.Core.Enums;
using InfonetPOS.Core.Helpers;
using InfonetPOS.Core.Helpers.ReceiptGenerator;
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
    public class CashRefundViewModel : MvxViewModel
    {
        private readonly IMvxNavigationService navigationService;
        private readonly IMvxLog log;
        private readonly PosManager posManager;
        private readonly IFcsService fcsService;
        private readonly IReceiptGenerator receiptGenerator;
        private readonly IMessageBoxService messageBoxService;

        #region UI Properties
        public string Language => App.Language.ToString();
        public double AmountPayable { get; set; }
        public double AmountPaid { get; set; }
        public double AmountToReturn { get; set; }
        public bool CanCancel
        {
            get => App.CurrentFeature == FeatureType.DeletePrepay
                  || App.CurrentFeature == FeatureType.PrepayBasketRefund;
        }
        #endregion

        #region Commands
        public IMvxCommand CancelCommand => new MvxCommand(async () =>
        {
            log.Debug("CashRefundViewModel: User pressed cancel button. Cancel refund.");
            if (App.CurrentFeature == FeatureType.DeletePrepay)
            {
                log.Debug("CashRefundViewModel: Current feature is delete prepay.Cancel prepay delete.");
                await CancelPrepayDelete();
            }
            else if (App.CurrentFeature == FeatureType.PrepayBasketRefund)
            {
                log.Debug("CashRefundViewModel: Current feature is prepay basket refund.Cancel cash refund.");
                await CancelPrepayBasketRefund();
            }

        });
        public IMvxCommand ConfirmCommand => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("CashRefundViewModel: Cash refund finished.Generate and Print receipt");
                posManager.ActiveSale.RefundStatus = RefundApprovalStatus.RefundApproved;
                GenerateReceipt();
                await this.navigationService.Navigate<PrintReceiptViewModel, int>(posManager.ActiveSale.PumpId);
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("CashRefundViewModel: Exception: {0}.", ex.Message), ex);
            }
        });

        private void GenerateReceipt()
        {
            log.Debug("CashRefundViewModel: Generating receipt.");
            if (App.CurrentFeature == FeatureType.PrepayRefund
                || App.CurrentFeature == FeatureType.DeletePrepay)
            {
                log.Debug("CashRefundViewModel: Generate receipt for full prepay refund.");
                posManager.ActiveSale.Receipt = receiptGenerator.FullPrepayRefundCash(posManager.ActiveSale);
            }
            else if (App.CurrentFeature == FeatureType.PrepayBasketRefund)
            {
                log.Debug("CashRefundViewModel: Generate receipt for partial prepay refund.");
                posManager.ActiveSale.Receipt = receiptGenerator.PartialPrepayRefundCash(posManager.ActiveSale);
            }
        }
        #endregion


        public CashRefundViewModel(IMvxNavigationService navigationService,
                                   IMvxLog log,
                                   PosManager posManager,
                                   IFcsService fcsService,
                                   IReceiptGenerator receiptGenerator,
                                   IMessageBoxService messageBoxService)
        {
            this.navigationService = navigationService;
            this.log = log;
            this.posManager = posManager;
            this.fcsService = fcsService;
            this.receiptGenerator = receiptGenerator;
            this.messageBoxService = messageBoxService;
        }

        public override void ViewAppearing()
        {
            log.Debug("CashRefundViewModel: View is appearing.");
            base.ViewAppearing();
            App.CultureChange += OnCultureChangeAsync;
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo cultureInfo)
        {
            await RaiseAllPropertiesChanged();
        }

        public override void ViewDisappearing()
        {
            log.Debug("CashRefundViewModel: View is disappearing.");
            base.ViewDisappearing();
            App.CultureChange -= OnCultureChangeAsync;
        }

        public override Task Initialize()
        {
            log.Debug("CashRefundViewModel: ViewModel is initialized.");
            SetAmounts();
            return base.Initialize();
        }

        #region All Private Methods
        private void SetAmounts()
        {
            try
            {
                if (App.CurrentFeature == FeatureType.PrepayRefund
                    || App.CurrentFeature == FeatureType.DeletePrepay)
                {
                    AmountPayable = 0;
                    AmountPaid = posManager.ActiveSale.Amount;
                    log.Debug("CashRefundViewModel: Refund reason is Can't Authorize pump.");
                }
                else if (App.CurrentFeature == FeatureType.PrepayBasketRefund)
                {
                    AmountPayable = posManager.ActiveSale.SoldBasket.Amount;
                    AmountPaid = posManager.ActiveSale.SoldBasket.Prepay.PrepayAmount;
                    log.Debug("CashRefundViewModel: Refund reason is partial prepay.");
                }

                AmountToReturn = AmountPaid - AmountPayable;
                posManager.ActiveSale.TotalPaid = AmountPaid;
                posManager.ActiveSale.Change = AmountToReturn;
                posManager.ActiveSale.Receipt = "";
                log.Debug("CashRefundViewModel: Amount payable:{0}, Amount Paid:{1}, Amount to return:{2}.", AmountPayable, AmountPaid, AmountToReturn);
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("CashRefundViewModel: Exception:{0}.", ex.Message), ex);
            }
        }

        private async Task CancelPrepayDelete()
        {
            log.Debug("CashRefundViewModel: Canceling prepay delete.");
            if ((await fcsService.PrepayCancelHoldRemoveAsync(posManager.ActiveSale.PumpId)).ResultOk)
            {
                log.Debug("CashRefundViewModel: PrepayCancelHoldRemove successfully.");
                posManager.ResetPrepayDelete(posManager.ActiveSale);
            }
            else
            {
                log.Error("CashRefundViewModel: PrepayCancelHoldRemove error occurred.");
                messageBoxService.ShowMessageBox("PrepayCancelHoldRemoveError", MessageBoxButtonType.OK);
            }
            await navigationService.Navigate<HomeViewModel>();
        }

        private async Task CancelPrepayBasketRefund()
        {
            try
            {
                log.Debug("CashRefundViewModel: Canceling prepay basket refund.");
                log.Debug("CashRefundViewModel: Sending cancelhold to basket with basket id:{0}.", posManager?.ActiveSale?.SoldBasket?.BasketID);
                var response = await fcsService?.CancelHoldBasket(posManager?.ActiveSale?.SoldBasket?.BasketID);
                if (response.ResultOK)
                {
                    log.Debug("CashRefundViewModel: Cancel hold successfully.");
                }
                else
                {
                    log.Error("CashRefundViewModel: Cancel hold error.");
                    messageBoxService.ShowMessageBox("CancelHoldBasketError", MessageBoxButtonType.OK);
                }
                log.Debug("CashRefundViewModel:Navigate to HomeViewModel.");
                await this.navigationService.Navigate<HomeViewModel>();
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("CashRefundViewModel: Exception: {0}", ex.Message), ex);
            }
        }
        #endregion
    }
}
