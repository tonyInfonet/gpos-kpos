using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPOS.Core.Enums;
using InfonetPOS.Core.Helpers;
using InfonetPOS.Core.Interfaces;
using InfonetPOS.Core.TPS.Services.Interfaces;
using InfonetPOS.Core.TPS.Entities;
using MvvmCross.Logging;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfonetPos.FcsIntegration.Entities;
using InfonetPOS.Core.DB.Enums;
using InfonetPOS.Core.Helpers.ReceiptGenerator;
using System.Globalization;
using System.Threading;

namespace InfonetPOS.Core.ViewModels
{
    public class CardTenderViewModel : MvxViewModel<int>
    {
        private readonly IMvxNavigationService navigationService;
        private readonly ITpsService tpsService;
        private readonly IFcsService fcsService;
        private readonly IMvxLog log;
        private readonly IAppSettings appSettings;
        private readonly PosManager posManager;
        private readonly IReceiptGenerator receiptGenerator;

        private double amount { get; set; }
        private int pumpId { get; set; }

        #region UI Properties
        public string Language => App.Language.ToString();
        public bool IsTpsConnected { get; set; }
        public bool IsFcsConnected { get; set; }
        #endregion

        public CardTenderViewModel(IMvxNavigationService navigationService,
                                    IMvxLog log,
                                    ITpsService tpsService,
                                    IFcsService fcsService,
                                    IAppSettings appSettings,
                                    PosManager posManager,
                                    IReceiptGenerator receiptGenerator)
        {
            this.navigationService = navigationService;
            this.log = log;
            this.tpsService = tpsService;
            this.fcsService = fcsService;
            this.posManager = posManager;
            this.appSettings = appSettings;
            this.receiptGenerator = receiptGenerator;
        }

        public override void Prepare(int parameter)
        {
            log.Debug("CardTenderViewModel: Receiving pumpId {0} as parameter.", parameter);
            pumpId = parameter;
        }

        public override async void ViewAppeared()
        {
            log.Debug("CardTenderViewModel: View is appeared.");
            base.ViewAppeared();

            try
            {
                SubscribeAllEvents();
                amount = posManager.ActiveSale.Amount;
                IsTpsConnected = App.IsTpsConnected;
                IsFcsConnected = App.IsFcsConnected;

                if (!IsTpsConnected)
                {
                    log.Error("CardTenderViewModel: TPS is not connected.Go to tender selection page.");
                    await NavigateToTenderSelection();
                }
                else
                {
                    log.Debug("CardTenderViewModel: TPS is connected.Process sale.");
                    await ProcessSaleResponse();
                }
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("CardTenderViewModel: Exception:{0}", ex.Message), ex);
            }
        }

        public override void ViewDisappearing()
        {
            log.Debug("CardTenderViewModel: View is disappearing.");
            base.ViewDisappearing();
            UnSubscribeAllEvents();
        }

        private async Task ProcessSaleResponse()
        {
            log.Debug("CardTenderViewModel: Processing sale response.");
            var saleResponse = await RequestPrepaySale();
            if (saleResponse != null && saleResponse.RequestApproved)
            {
                log.Debug("CardTenderViewModel: SaleResponse is not null and sale request is approved.");
                if (App.CurrentFeature == FeatureType.Prepay)
                {
                    await OnPrepaySaleSuccess();
                }
                else if (App.CurrentFeature == FeatureType.Postpay)
                {
                    await OnPostpaySaleSuccess();
                }
            }
            else if (saleResponse == null)
            {
                log.Error("CardTenderViewModel: Cancellation token is requested or tps is not connected.Go to TenderSelection page.");
                await this.navigationService.Close(this);
            }
            else
            {
                log.Debug("CardTenderViewModel: Sale response is not null.But Request is not approved.");
                await OnSaleFailure();
            }

        }

        private async Task OnPostpaySaleSuccess()
        {
            log.Info("CardTenderViewModel: Payment Approved.Generate and print receipt for postpay.");
            posManager.ActiveSale.Receipt = receiptGenerator.PostPayCard(posManager.ActiveSale);
            posManager.ActiveSale.PaymentStatus = PaymentApprovalStatus.PaymentApproved;
            await GoToPrintReceipt();
        }

        private async Task GoToPrintReceipt()
        {
            try
            {
                if (App.IsFcsConnected && App.IsTpsConnected)
                {
                    log.Debug("CardTenderViewModel: Payment Status is {0} and navigate to PrintReceipt page.", posManager.ActiveSale.PaymentStatus.ToString());
                    await navigationService.Navigate<PrintReceiptViewModel, int>(pumpId);
                }
                else
                {
                    log.Error("CardTenderViewModel: Fcs or Tps is not connected.");
                }
            }
            catch (Exception ex)
            {
                log.ErrorException("CardTenderViewModel: ", ex);
            }
        }

        private async Task OnPrepaySaleSuccess()
        {
            log.Info("CardTenderViewModel: Payment Approved.Generate receipt for prepay and set prepay.");
            posManager.ActiveSale.PaymentStatus = PaymentApprovalStatus.PaymentApproved;
            posManager.ActiveSale.Receipt = receiptGenerator.PrepayCard(posManager.ActiveSale);

            try
            {
                var response = await SetPrepay();
                if (response.ResultOk)
                {
                    log.Debug("CardTenderViewModel: Set prepay successfully.");
                    await OnSetPrepaySuccess();
                }
                else
                {
                    log.Error("CardRefundViewModel: Set prepay failed.");
                    await OnSetPrepayFailure();
                }
            }
            catch (Exception ex)
            {
                log.ErrorException("CardTenderViewModel: ", ex);
            }
        }

        private async Task OnSaleFailure()
        {
            log.Info("CardTenderViewModel: Payment not approved.Generate receipt for card sale failure and go to print receipt.");
            posManager.ActiveSale.PaymentStatus = PaymentApprovalStatus.PaymentNotApproved;
            posManager.ActiveSale.Receipt = receiptGenerator.CardSaleFailure(posManager.ActiveSale);
            await GoToPrintReceipt();
        }

        private string GetPaymentType()
        {
            if (posManager.ActiveSale.SaleTender.Class == TenderClass.CRCARD)
            {
                log.Debug("CardTenderViewModel: Payment type is credit.");
                return "Credit";
            }
            else
            {
                log.Debug("CardRefundViewModel: Payment type is debit.");
                return "Debit";
            }
        }

        private async Task<TpsResponse> RequestPrepaySale()
        {
            log.Debug("CardTenderViewModel: Request for prepay sale.");
            string paymentType = GetPaymentType();
            string invoiceNo = posManager.ActiveSale.InvoiceNo;
            CancellationTokenSource tokenSource = new CancellationTokenSource(appSettings.DelayForTPSResponse);
            CancellationToken token = tokenSource.Token;
            TpsResponse saleResponse = null;

            log.Debug("CardTenderViewModel: Request for sale and waiting for response from tps.");
            saleResponse = await tpsService.SaleRequestAsync(paymentType, pumpId, invoiceNo, amount, token);
            if (saleResponse != null)
            {
                posManager.ActiveSale.SaleResponse = saleResponse;
                posManager.ActiveSale.Receipt = saleResponse.Receipt;
                posManager.ActiveSale.TotalPaid = amount;
                posManager.ActiveSale.Change = 0.0;
                log.Debug("CardTenderViewModel: Sale response is not null.TotalPaid:{0},Change:{1}", amount, posManager.ActiveSale.Change);
            }
            else
            {
                log.Error("CardTenderViewModel: Sale response is null.");
            }

            return saleResponse;
        }

        private async Task<PrepayResponse> SetPrepay()
        {
            log.Info("CardTenderViewModel: If sale type is prepay and payment is approved ,then request to fcs to set prepay.");
            return await fcsService.PrepaySetAsync(this.pumpId,
                                                        posManager.ActiveSale.Amount,
                                                        posManager.ActiveSale.InvoiceNo,
                                                        appSettings.PosId,
                                                        posManager.ActiveSale.TotalPaid,
                                                        posManager.ActiveSale.Change,
                                                        posManager.ActiveSale.Receipt);
        }

        private async Task OnSetPrepaySuccess()
        {
            log.Debug("CardTenderViewModel: Make IsPrepaySet in currentSale true as prepay is set.");
            posManager.ActiveSale.IsPrepaySet = true;
            await GoToPrintReceipt();
        }

        private async Task OnSetPrepayFailure()
        {
            log.Debug("CardTenderViewModel: Fcs can't authorize pump.Navigate to TenderSelection page.");
            posManager.ActiveSale.RefundReason = RefundReason.CantAuthorizePump;

            App.CurrentFeature = FeatureType.PrepayRefund;
            await navigationService.Navigate<TenderSelectionViewModel, int>(this.pumpId);
        }

        private void SubscribeAllEvents()
        {
            log.Debug("CardTenderViewModel: Subscribing All events.");
            App.CultureChange += OnCultureChangeAsync;
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo cultureInfo)
        {
            await RaiseAllPropertiesChanged();
        }

        private void UnSubscribeAllEvents()
        {
            log.Debug("CardTenderViewModel: UnSubscribing All events.");
            App.CultureChange -= OnCultureChangeAsync;
        }

        private async Task NavigateToTenderSelection()
        {
            try
            {
                IsFcsConnected = App.IsFcsConnected;
                if (IsFcsConnected)
                {
                    System.Timers.Timer goToTenderSelectionTimer;
                    goToTenderSelectionTimer = new System.Timers.Timer(2000)
                    {
                        AutoReset = false
                    };

                    goToTenderSelectionTimer.Elapsed += async (sender, eventArgs) =>
                    {
                        log.Debug("CardTenderViewModel: Tps is not connected. Go to Tender Selection page again.");
                        await this.navigationService.Close(this);
                    };
                    goToTenderSelectionTimer.Enabled = true;

                }
                else
                {
                    log.Error("CardTenderViewModel: Fcs is not connected.Can't go to tender selection page.");
                }
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("CardTenderViewModel: Exception:{0}.", ex.Message), ex);
            }
        }
    }
}
