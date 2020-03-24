using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPOS.Core.DB.Entities;
using InfonetPOS.Core.DB.Interface;
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
    public class TenderSelectionViewModel : MvxViewModel<int>
    {
        private readonly IMvxNavigationService navigationService;
        private readonly IMvxLog log;
        private readonly IDBAccess dbAccess;
        private readonly PosManager posManager;
        private readonly IFcsService fcsService;
        private readonly IReceiptGenerator receiptGenerator;
        private readonly IAppSettings appSettings;
        private readonly IMessageBoxService messageBoxService;

        #region UI Properties
        private Tender selectedTender;
        public Tender SelectedTender
        {
            get => selectedTender;
            set
            {
                selectedTender = value;
                Convert();
            }
        }
        public string Language => App.Language.ToString();
        public MvxObservableCollection<Tender> SupportedTenders { get; set; }
        public double ExchangeRate { get; set; }
        public double ConvertedAmount { get; set; }
        public int PumpId { get; set; }
        public string MessageKey { get; set; }
        public bool CanDriveOffOrPumpTest
        {
            get => (App.CurrentFeature == Enums.FeatureType.Postpay);
            //get => (App.CurrentFeature == Enums.FeatureType.Postpay && appSettings.PosType == InfonetPos.FcsIntegration.Enums.POSType.KPOS);
        }
        public bool CanCancel
        {
            get => (App.CurrentFeature == Enums.FeatureType.DeletePrepay
                  || App.CurrentFeature == Enums.FeatureType.Postpay
                  || App.CurrentFeature == Enums.FeatureType.Prepay
                  || App.CurrentFeature == Enums.FeatureType.PrepayBasketRefund);
        }
        #endregion

        #region Commands

        public IMvxCommand DriveOffCommand => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("TenderSelectionViewModel: Take confirmation from user whether he wants to drive off or not.");
                var response = await this.navigationService.Navigate<ConfirmationViewModel, string, bool>("DriveOffMessage");
                if (response)
                {
                    log.Debug("TenderSelectionViewModel: User wants to drive off.Trying to drive off.");
                    App.CurrentFeature = Enums.FeatureType.DriveOff;
                    await ProcessDriveOffOrPumpTest();

                }
                else
                {
                    log.Debug("TenderSelectionViewModel: User doesn't wants to drive off.");
                }
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("TenderSelectionViewModel: Exception:{0}.", ex.Message), ex);
            }
        });

        public IMvxCommand PumpTestCommand => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("TenderSelectionViewModel: Take confirmation from user whether he wants to PumpTest or not.");
                var response = await this.navigationService.Navigate<ConfirmationViewModel, string, bool>("PumpTestMessage");
                if (response)
                {
                    log.Debug("TenderSelectionViewModel: User wants to PumpTest.Trying to PumpTest.");
                    App.CurrentFeature = Enums.FeatureType.PumpTest;
                    await ProcessDriveOffOrPumpTest();

                }
                else
                {
                    log.Debug("TenderSelectionViewModel: User doesn't want to Pump Test.");
                }
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("TenderSelectionViewModel: Exception:{0}.", ex.Message), ex);
            }
        });

        public IMvxCommand CancelCmd => new MvxCommand(async () =>
        {
            log.Debug("TenderSelectionViewModel: User Pressed cancel.Feature type is: {0}", App.CurrentFeature.ToString());
            if (App.CurrentFeature == Enums.FeatureType.Prepay)
            {
                await CancelPrepay();
            }
            else if (App.CurrentFeature == Enums.FeatureType.DeletePrepay)
            {
                await CancelPrepayDelete();
            }
            else if (App.CurrentFeature == Enums.FeatureType.Postpay
                    || App.CurrentFeature == Enums.FeatureType.PrepayBasketRefund)
            {
                log.Debug("TenderSelectionViewModel: Current feature is {0}.Cancel tender for basket", App.CurrentFeature.ToString());
                await CancelTenderForBasket();
            }
        });

        public IMvxCommand ConfirmCmd => new MvxCommand(async () =>
        {
            log.Debug("TenderSelectionViewModel: User pressed confirm button.");
            UseConvertedAmount();

            if (App.CurrentFeature == Enums.FeatureType.Prepay
              || App.CurrentFeature == Enums.FeatureType.Postpay)
            {
                posManager.ActiveSale.InvoiceNo = GetInvoiceNo();
            }

            if (posManager.ActiveSale.InvoiceNo == null)
            {
                await CancelPrepay();
            }
            else if (SelectedTender.Class == DB.Enums.TenderClass.CASH)
            {
                await OnCashSelection();
            }
            else
            {
                await OnCardSelection();
            }
        });
        #endregion

        private async Task OnCashSelection()
        {
            if (App.CurrentFeature == Enums.FeatureType.Prepay
                || App.CurrentFeature == Enums.FeatureType.Postpay)
            {

                await navigationService.Navigate<CashTenderViewModel, int>(PumpId);
            }
            else if (App.CurrentFeature == Enums.FeatureType.PrepayRefund
                     || App.CurrentFeature == Enums.FeatureType.DeletePrepay
                     || App.CurrentFeature == Enums.FeatureType.PrepayBasketRefund)
            {
                await navigationService.Navigate<CashRefundViewModel>();
            }
        }

        private async Task OnCardSelection()
        {
            if (App.CurrentFeature == Enums.FeatureType.Prepay
                || App.CurrentFeature == Enums.FeatureType.Postpay)
            {

                await navigationService.Navigate<CardTenderViewModel, int>(PumpId);
            }
            else if (App.CurrentFeature == Enums.FeatureType.PrepayRefund
                     || App.CurrentFeature == Enums.FeatureType.DeletePrepay
                     || App.CurrentFeature == Enums.FeatureType.PrepayBasketRefund)
            {
                await navigationService.Navigate<CardRefundViewModel, int>(PumpId);
            }

        }

        public TenderSelectionViewModel(IMvxNavigationService navigationService,
                                        IMvxLog log,
                                        IDBAccess dbAccess,
                                        PosManager posManager,
                                        IFcsService fcsService,
                                        IReceiptGenerator receiptGenerator,
                                        IAppSettings appSettings,
                                        IMessageBoxService messageBoxService)
        {
            this.navigationService = navigationService;
            this.log = log;
            this.dbAccess = dbAccess;
            this.posManager = posManager;
            this.fcsService = fcsService;
            this.receiptGenerator = receiptGenerator;
            this.appSettings = appSettings;
            this.messageBoxService = messageBoxService;
        }

        public async override Task Initialize()
        {
            log.Debug("TenderSelectionViewModel: ViewModel is initializing.");
            SetMessageKey();

            if (!await LoadTenders())
            {
                if (App.CurrentFeature == Enums.FeatureType.Prepay)
                {
                    await CancelPrepay();
                }
            }
            log.Debug("TenderSelectionViewModel: ViewModel initialization finished.");
            await base.Initialize();
        }

        public override void ViewAppearing()
        {
            log.Debug("TenderSelectionViewModel: View is appearing.");
            base.ViewAppearing();
            App.CultureChange += OnCultureChangeAsync;
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo cultureInfo)
        {
            await RaiseAllPropertiesChanged();
        }

        public override void ViewDisappearing()
        {
            log.Debug("TenderSelectionViewModel: View is disappearing.");
            base.ViewDisappearing();
            App.CultureChange -= OnCultureChangeAsync;
        }

        private void SetMessageKey()
        {
            log.Debug("TenderSelectionViewModel: Setting Message key.");
            MessageKey = "PaymentMethod";

            if (App.CurrentFeature == Enums.FeatureType.Prepay
                || App.CurrentFeature == Enums.FeatureType.Postpay)
            {
                MessageKey = "SelectPaymentMethod";
            }
            else if (App.CurrentFeature == Enums.FeatureType.PrepayRefund
                || App.CurrentFeature == Enums.FeatureType.DeletePrepay
                || App.CurrentFeature == Enums.FeatureType.PrepayBasketRefund)
            {
                MessageKey = "SelectRefundMethod";
            }
        }

        private async Task<bool> LoadTenders()
        {
            try
            {
                log.Debug("TenderSelectionViewModel: Loading all tenders from database");
                var supportedTenders = posManager.GetTenders();
                while (supportedTenders == null)
                {
                    log.Debug("TenderSelectionViewModel: Failed to get tenders from DB.Ask your to try again.");
                    var response = messageBoxService.ShowMessageBox("ConnectionLoseWhileLogin", MessageBoxButtonType.YesNo);
                    if (response == MessageBoxResultType.Yes)
                    {
                        log.Debug("TenderSelectionViewModel: User wants to fetch tender again from DB.");
                        supportedTenders = posManager.GetTenders();
                    }
                    else
                    {
                        log.Debug("TenderSelectionViewModel: User refused to try again.");
                        break;
                    }
                }

                if (supportedTenders == null)
                {
                    log.Error("TenderSelectionViewModel: Supported tenders null.");
                    return false;
                }

                if (App.CurrentFeature == Enums.FeatureType.PrepayBasketRefund
                   || App.CurrentFeature == Enums.FeatureType.PrepayRefund
                   || App.CurrentFeature == Enums.FeatureType.DeletePrepay)
                {
                    log.Debug("TenderSelectionViewModel: Filter all tenders which supports refund.");
                    supportedTenders = supportedTenders.Where(tender => tender?.GiveAsRef == true && tender?.GiveAsRef != null).ToList();
                }

                SupportedTenders = new MvxObservableCollection<Tender>(supportedTenders);

                if (SupportedTenders.Count <= 0)
                {
                    log.Error("TenderSelectionViewModel: Supported tenders list in empty.");
                    return false;
                }
                else
                {
                    log.Debug("TenderSelectionViewModel: By deafult select the first tender.");
                    SelectedTender = SupportedTenders[0];
                    return true;
                }
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("TenderSelectionViewModel: Exception Message: {0}", ex.Message), ex);
                return false;
            }
        }

        private void UseConvertedAmount()
        {
            log.Debug("TenderSelectionViewModel: Use converted amount.");
            if (App.CurrentFeature == Enums.FeatureType.Prepay
                || App.CurrentFeature == Enums.FeatureType.Postpay)
            {
                posManager.ActiveSale.SaleTender = SelectedTender;
                log.Debug("TenderSelectionViewModel: Feature type is prepay or postpay.SaleTender is {0}", posManager.ActiveSale.SaleTender.Description);
            }
            else if (App.CurrentFeature == Enums.FeatureType.PrepayRefund
                || App.CurrentFeature == Enums.FeatureType.DeletePrepay
                || App.CurrentFeature == Enums.FeatureType.PrepayBasketRefund)
            {
                posManager.ActiveSale.RefundTender = SelectedTender;
                log.Debug("TenderSelectionViewModel: Feature type is refund.Refund tender is {0}", posManager.ActiveSale.RefundTender.Description);
            }
        }

        private async Task GoToHome()
        {
            log.Debug("TenderSelectionViewModel: Go to home page.");
            if (App.CurrentFeature == Enums.FeatureType.Prepay)
            {
                await this.navigationService.Navigate<HomeViewModel>();
            }
            else
            {
                await this.navigationService.Close(this);
            }
        }

        private void Convert()
        {
            ExchangeRate = SelectedTender.ExchangeRate;
            log.Debug("TenderSelectionViewModel: Converting amount using exchange rate");
            if (App.CurrentFeature == Enums.FeatureType.Prepay
                || App.CurrentFeature == Enums.FeatureType.Postpay)
            {
                posManager.ActiveSale.SaleTender = selectedTender;
                ConvertedAmount = posManager.ActiveSale.Amount / ExchangeRate;
            }
            else if (App.CurrentFeature == Enums.FeatureType.PrepayBasketRefund)
            {
                posManager.ActiveSale.RefundTender = selectedTender;
                ConvertedAmount = posManager.ActiveSale.GetAvailableRefund() / ExchangeRate;
            }
            else if (App.CurrentFeature == Enums.FeatureType.DeletePrepay ||
                App.CurrentFeature == Enums.FeatureType.PrepayRefund)
            {
                posManager.ActiveSale.RefundTender = selectedTender;
                ConvertedAmount = posManager.ActiveSale.Amount / ExchangeRate;
            }
            log.Debug("TenderSelectionViewModel: Selected Tender:{0}, Exchange Rate:{1}, Converted Amount:{2}",
                    SelectedTender.Description, ExchangeRate, ConvertedAmount);
        }

        public override void Prepare(int parameter)
        {
            log.Debug("TenderSelectionViewModel: Receiving pumpId:{0} as parameter.", parameter);
            PumpId = parameter;
        }

        private async Task CancelPrepay()
        {
            log.Debug("TenderSelectionViewModel: Canceling prepay.");
            await posManager.CleanUpSale(posManager.ActiveSale, PumpId, false);
            await GoToHome();
        }


        private async Task CancelPrepayDelete()
        {
            log.Debug("TenderSelectionViewModel: Canceling prepay delete.");
            if ((await fcsService.PrepayCancelHoldRemoveAsync(PumpId)).ResultOk)
            {
                log.Debug("TenderSelectionViewModel: PrepayCancelHoldRemove successfully.");
                posManager.ResetPrepayDelete(posManager.ActiveSale);
            }
            else
            {
                log.Error("TenderSelectionViewModel: PrepayCancelHoldRemoveError occurred.");
                messageBoxService.ShowMessageBox("PrepayCancelHoldRemoveError", MessageBoxButtonType.OK);
            }
            await GoToHome();
        }

        private async Task CancelTenderForBasket()
        {
            try
            {
                log.Debug("TenderSelectionViewModel: Canceling tender for basket.");
                log.Debug("TenderSelectionViewModel: Sending cancelhold to basket with basket id:{0}.", posManager?.ActiveSale?.SoldBasket?.BasketID);
                var response = await fcsService?.CancelHoldBasket(posManager?.ActiveSale?.SoldBasket?.BasketID);
                if (response.ResultOK)
                {
                    log.Debug("TenderSelectionViewModel: Cancel hold successfully.");
                }
                else
                {
                    log.Error("TenderSelectionViewModel: Cancel hold error.");
                    messageBoxService.ShowMessageBox("CancelHoldBasketError", MessageBoxButtonType.OK);
                }
                log.Debug("TenderSelectionViewModel: Navigate to home page.");
                await GoToHome();
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("TenderSelectionViewModel: Exception: {0}", ex.Message), ex);
            }
        }

        private async Task ProcessDriveOffOrPumpTest()
        {
            log.Debug("TenderSelectionViewModel: Process Drive Off or Pump Test.");
            posManager.ActiveSale.InvoiceNo = GetInvoiceNo();
            if (posManager.ActiveSale.InvoiceNo == null)
            {
                var cancelHoldBasketResponse = await fcsService?.CancelHoldBasket(posManager.ActiveSale.SoldBasket.BasketID);
                if (cancelHoldBasketResponse.ResultOK)
                {
                    log.Debug("TenderSelectionViewModel: CancelHoldBasket successfully.");
                }
                else
                {
                    log.Error("TenderSelectionViewModel: CancelHoldBasket failure");
                    messageBoxService.ShowMessageBox("CancelHoldBasketError", MessageBoxButtonType.OK);
                }
                log.Debug("TenderSelectionViewModel: Go to Home.");
                await GoToHome();
            }
            else
            {
                log.Debug("TenderSelectionViewModel: InvoiceNo: {0}", posManager.ActiveSale.InvoiceNo);
                posManager.ActiveSale.TotalPaid = 0;
                log.Debug("TenderSelectionViewModel: TotalPaid: {0}", posManager.ActiveSale.TotalPaid);
                posManager.ActiveSale.Change = 0;
                log.Debug("TenderSelectionViewModel: Change: {0}", posManager.ActiveSale.Change);
                posManager.ActiveSale.SoldBasket.InvoiceType = App.CurrentFeature.ToString();
                log.Debug("TenderSelectionViewModel: InvoiceType: {0}", posManager.ActiveSale.SoldBasket.InvoiceType);
                posManager.ActiveSale.Receipt = receiptGenerator.DriveOffOrPumpTest(posManager.ActiveSale);
                log.Debug("TenderSelectionViewModel: Receipt: {0}", posManager.ActiveSale.Receipt);
                log.Debug("TenderSelectionViewModel: Go to Print receipt view model to print receipt for pumpId:{0}", this.PumpId);
                await this.navigationService.Navigate<PrintReceiptViewModel, int>(this.PumpId);
            }
            
        }

        private string GetInvoiceNo()
        {
            log.Debug("TenderSelectionViewModel: Get Invoice no.");
            var invoiceNo = dbAccess.GetInvoiceNo();
            while (invoiceNo == null)
            {
                var response = messageBoxService.ShowMessageBox("ConnectionLoseWhileLogin", MessageBoxButtonType.YesNo);
                if (response == MessageBoxResultType.Yes)
                {
                    invoiceNo = dbAccess.GetInvoiceNo();
                }
                else
                {
                    break;
                }
            }

            if (invoiceNo != null)
            {
                log.Debug("TenderSelectionViewModel: Invoice no {0} returned.", invoiceNo);
            }
            return invoiceNo;
        }
    }
}
