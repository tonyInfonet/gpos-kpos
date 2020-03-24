using InfonetPos.FcsIntegration.Entities;
using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPOS.Core.Enums;
using InfonetPOS.Core.Helpers;
using InfonetPOS.Core.Helpers.ReceiptGenerator;
using InfonetPOS.Core.Interfaces;
using InfonetPOS.Core.TPS.Services.Interfaces;
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
    public class CashTenderViewModel : MvxViewModel<int>
    {
        private readonly IMvxNavigationService navigationService;
        private readonly IFcsService fcsService;
        private readonly IMvxLog log;
        private readonly PosManager posManager;
        private readonly IAppSettings appSettings;
        private readonly IReceiptGenerator receiptGenerator;
        private readonly IMessageBoxService messageBoxService;

        #region UI Properties
        public string Language => App.Language.ToString();
        public double ConvertedAmountPayable { get; set; }
        private double AmountPayable;
        public double ConvertedAmountPaid { get; set; }
        private double AmountPaid;
        public double AmountToReturn { get; set; }
        public string CurrentAmountInString { get; set; }
        public int PumpId { get; set; }
        public bool IsDecimalButtonEnabled { get; set; }
        public bool CanCancel
        {
            get => (App.CurrentFeature == Enums.FeatureType.Postpay
                  || App.CurrentFeature == Enums.FeatureType.Prepay);
        }
        public bool IsConfirmButtonEnabled { get; set; }
        #endregion


        public IMvxCommand CancelCmd => new MvxCommand(async () =>
        {
            log.Debug("CashTenderViewModel: User pressed cancel button.");
            if (App.CurrentFeature == FeatureType.Prepay)
            {
                log.Debug("CashTenderViewModel: Current feature is prepay.cancel prepay.");
                await CancelPrepay();
            }
            else if (App.CurrentFeature == FeatureType.Postpay)
            {
                log.Debug("CashTenderViewModel: Current feature is postpay.Cancel postpay.");
                await CancelPostPay();
            }
        });

        public IMvxCommand DigitEntryCommand => new MvxCommand<string>(CurrentDigits =>
        {
            if (CurrentDigits == ".")
                this.IsDecimalButtonEnabled = false;
            this.CurrentAmountInString += CurrentDigits;
        });

        public IMvxCommand DigitDeleteCommmand => new MvxCommand(() =>
        {
            if (this.CurrentAmountInString.Length > 0)
            {
                if (this.CurrentAmountInString[this.CurrentAmountInString.Length - 1] == '.')
                    this.IsDecimalButtonEnabled = true;
                this.CurrentAmountInString = this.CurrentAmountInString?.Remove(this.CurrentAmountInString.Length - 1);
            }
            else
            {
                log.Error("CashTender: There is no digit to delete.");
            }
        });

        public IMvxCommand AmountEntryCommand => new MvxCommand(() =>
        {
            try
            {
                log.Debug("CashTenderViewModel: User pressed amount entry button.");
                if (this.CurrentAmountInString?.Length > 0)
                {
                    if (this.CurrentAmountInString[this.CurrentAmountInString.Length - 1] == '.')
                        this.CurrentAmountInString += "0";
                    ConvertedAmountPaid = Convert.ToDouble(this.CurrentAmountInString);
                    AmountPaid = ConvertedAmountPaid * posManager.ActiveSale.SaleTender.ExchangeRate;
                    AmountToReturn = AmountPaid - AmountPayable;
                    IsConfirmButtonEnabled = (AmountToReturn >= 0);
                    log.Info("CashTenderViewModel: Customer Paid amount {0} for fueling and Amount to return {1}.", AmountPaid, AmountToReturn);
                }
                else
                {
                    log.Error("CashTenderViewModel: Current amount is null or empty.");
                }
            }
            catch (Exception ex)
            {
                log.ErrorException("CashTenderViewModel: ", ex);
            }
        });

        public IMvxCommand ConfirmCmd => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("CashTenderViewModel: User pressed confirm button.");
                if (AmountToReturn >= 0)
                {
                    posManager.ActiveSale.TotalPaid = AmountPaid;
                    posManager.ActiveSale.Change = AmountToReturn;
                    posManager.ActiveSale.Receipt = "";
                    log.Debug("CashTenderViewModel: TotalPaid:{0}, Change:{1}", AmountPaid, AmountToReturn);

                    if (App.CurrentFeature == Enums.FeatureType.Prepay)
                    {
                        log.Debug("CashTenderViewModel: Current feature is prepay.Confirm prepay set.");
                        await OnPrepayConfirm();
                    }
                    else if (App.CurrentFeature == FeatureType.Postpay)
                    {
                        log.Debug("CashTenderViewModel: Current feature is post pay.Process postpay.");
                        await OnPostPayConfirm();
                    }
                }

            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("CashTenderViewModel: ", ex.Message), ex);
            }
        });

        private async Task OnPostPayConfirm()
        {
            log.Debug("CashTenderViewModel:Generate and print receipt for postpay.");
            posManager.ActiveSale.Receipt = receiptGenerator.PostPayCash(posManager.ActiveSale);
            posManager.ActiveSale.PaymentStatus = PaymentApprovalStatus.PaymentApproved;
            await this.navigationService.Navigate<PrintReceiptViewModel, int>(PumpId);
        }

        public CashTenderViewModel(IMvxNavigationService navigationService,
                                   IFcsService fcsService,
                                   IMvxLog log,
                                   PosManager posManager,
                                   IAppSettings appSettings,
                                   IReceiptGenerator receiptGenerator,
                                   IMessageBoxService messageBoxService)
        {
            this.navigationService = navigationService;
            this.fcsService = fcsService;
            this.log = log;
            this.posManager = posManager;
            this.appSettings = appSettings;
            this.receiptGenerator = receiptGenerator;
            this.messageBoxService = messageBoxService;
        }

        public override void ViewAppearing()
        {
            log.Debug("CashTenderViewModel: View is appearing.");
            base.ViewAppearing();
            App.CultureChange += OnCultureChangeAsync;
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo cultureInfo)
        {
            await RaiseAllPropertiesChanged();
        }

        public override void ViewDisappearing()
        {
            log.Debug("CashTenderViewModel: View is disappearing.");
            base.ViewDisappearing();
            App.CultureChange -= OnCultureChangeAsync;
        }

        public override Task Initialize()
        {
            log.Debug("CashTenderViewModel: ViewModel is initializing.");
            this.CurrentAmountInString = "";
            this.IsDecimalButtonEnabled = true;
            this.IsConfirmButtonEnabled = false;
            SetAmounts();
            log.Debug("CashTenderViewModel: ViewModel initialization finished.");
            return base.Initialize();
        }

        public override void Prepare(int parameter)
        {
            log.Debug("CashTenderViewModel: Received pumpid:{0} as parameter.", parameter);
            PumpId = parameter;
        }

        private async Task OnPrepayConfirm()
        {
            log.Debug("CashTenderViewModel: Payment is approved for prepay.Generate receipt for prepay and set prepay.");
            posManager.ActiveSale.PaymentStatus = PaymentApprovalStatus.PaymentApproved;
            posManager.ActiveSale.Receipt = receiptGenerator.PrepayCash(posManager.ActiveSale);

            var response = await SetPrepay();
            if (response.ResultOk)
            {
                log.Debug("CashTenderViewModel: Successfully set prepay.");
                await OnSetPrepaySuccess();
            }
            else
            {
                log.Error("CashTenderViewModel: Prepay set failed.");
                await OnSetPrepayFailure();
            }
        }

        private async Task<PrepayResponse> SetPrepay()
        {
            log.Info("CashTenderViewModel: If sale type is prepay and payment is approved ,then request to fcs to set prepay.");
            return await fcsService.PrepaySetAsync(this.PumpId,
                                                        posManager.ActiveSale.Amount,
                                                        posManager.ActiveSale.InvoiceNo,
                                                        appSettings.PosId,
                                                        posManager.ActiveSale.TotalPaid,
                                                        posManager.ActiveSale.Change,
                                                        posManager.ActiveSale.Receipt);
        }

        private async Task OnSetPrepaySuccess()
        {
            log.Debug("CashTenderViewModel: Make IsPrepaySet in currentSale true as prepay is set.");
            posManager.ActiveSale.IsPrepaySet = true;
            await GoToPrintReceipt();
        }

        private async Task OnSetPrepayFailure()
        {
            log.Debug("CashTenderViewModel: Fcs can't authorize pump.Navigate to Tender selection page.");
            posManager.ActiveSale.RefundReason = RefundReason.CantAuthorizePump;

            App.CurrentFeature = FeatureType.PrepayRefund;
            await navigationService.Navigate<TenderSelectionViewModel, int>(this.PumpId);
        }

        private async Task GoToPrintReceipt()
        {
            try
            {
                if (App.IsFcsConnected)
                {
                    log.Debug("CashTenderViewModel: Payment Status is {0} and navigate to paymentStatus page.", posManager.ActiveSale.PaymentStatus.ToString());
                    await navigationService.Navigate<PrintReceiptViewModel, int>(PumpId);
                }
                else
                {
                    log.Error("CashTenderViewModel: Fcs is not connected.Can't go for print receipt.");
                }
            }
            catch (Exception ex)
            {
                log.ErrorException("CashTenderViewModel: ", ex);
            }
        }

        private void SetAmounts()
        {
            AmountPayable = posManager.ActiveSale.Amount;
            ConvertedAmountPayable = AmountPayable / posManager.ActiveSale.SaleTender.ExchangeRate;
            AmountPaid = 0;
            log.Debug("CashTenderViewModel: Setting amounts.AmountPayable:{0},ConvertedAmountPayable:{1},AmountPaid:{2}", AmountPayable, ConvertedAmountPayable, AmountPaid);
        }

        private async Task CancelPrepay()
        {
            log.Debug("CashTenderViewModel: Canceling prepay.Clean up sale and go to home page.");
            await posManager.CleanUpSale(posManager.ActiveSale, PumpId, false);
            await navigationService.Navigate<HomeViewModel>();
        }

        private async Task CancelPostPay()
        {
            try
            {
                log.Debug("CashTenderViewModel: Canceling tender for postpay.");
                log.Debug("CashTenderViewModel: Sending cancelhold to basket with basket id:{0}.", posManager?.ActiveSale?.SoldBasket?.BasketID);
                var response = await fcsService?.CancelHoldBasket(posManager?.ActiveSale?.SoldBasket?.BasketID);
                if (response.ResultOK)
                {
                    log.Debug("CashTenderViewModel: Cancel hold successfully.Navigate to home view model.");
                }
                else
                {
                    log.Error("CashTenderViewModel: Cancel hold error.");
                    messageBoxService.ShowMessageBox("CancelHoldBasketError", MessageBoxButtonType.OK);
                }
                await this.navigationService.Navigate<HomeViewModel>();
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("CashTenderViewModel: Exception: {0}", ex.Message), ex);
            }
        }
    }
}
