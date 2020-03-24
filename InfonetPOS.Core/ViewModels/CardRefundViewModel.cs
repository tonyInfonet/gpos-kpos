using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPOS.Core.Helpers;
using InfonetPOS.Core.Helpers.ReceiptGenerator;
using InfonetPOS.Core.Interfaces;
using InfonetPOS.Core.TPS.Services.Interfaces;
using MvvmCross.Logging;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InfonetPOS.Core.ViewModels
{
    public class CardRefundViewModel : MvxViewModel<int>
    {
        private readonly IMvxNavigationService navigationService;
        private readonly ITpsService tpsService;
        private readonly IFcsService fcsService;
        private readonly IMvxLog log;
        private readonly IAppSettings appSettings;
        private readonly PosManager posManager;
        private readonly IReceiptGenerator receiptGenerator;

        private int pumpId;

        #region UI Properties
        public string Language => App.Language.ToString();
        public bool IsTpsConnected { get; set; }
        public bool IsFcsConnected { get; set; }
        #endregion

        public override void Prepare(int parameter)
        {
            log.Debug("CardRefundViewModel: Receiving pumpId {0} as parameter.", parameter);
            this.pumpId = parameter;
        }

        public CardRefundViewModel(IMvxNavigationService navigationService,
                               ITpsService tpsService,
                               IFcsService fcsService,
                               IMvxLog log,
                               IAppSettings appSettings,
                               PosManager posManager,
                               IReceiptGenerator receiptGenerator)
        {
            this.navigationService = navigationService;
            this.tpsService = tpsService;
            this.fcsService = fcsService;
            this.log = log;
            this.posManager = posManager;
            this.appSettings = appSettings;
            this.receiptGenerator = receiptGenerator;
        }

        public override async void ViewAppeared()
        {
            log.Debug("CardRefundViewModel: View is appeared.");
            base.ViewAppeared();

            try
            {
                SubscribeAllEvents();
                IsTpsConnected = App.IsTpsConnected;
                IsFcsConnected = App.IsFcsConnected;

                if (!IsTpsConnected)
                {
                    log.Debug("CardRefundViewModel: TPS is not connected.Go to tender selection page.");
                    await NavigateToTenderSelection();
                }
                else
                {
                    log.Debug("CardRefundViewModel: TPS is connected.Process sale for refund.");
                    double refundAmount = posManager.ActiveSale.GetAvailableRefund();
                    posManager.ActiveSale.TotalPaid = posManager.ActiveSale.Amount;
                    posManager.ActiveSale.Change = refundAmount;
                    log.Debug("CardRefundViewModel: RefundAmount: {0}, TotalPaid: {1}, Change: {2}", refundAmount, posManager.ActiveSale.TotalPaid, posManager.ActiveSale.Change);

                    CancellationTokenSource tokenSource = new CancellationTokenSource(appSettings.DelayForTPSResponse);
                    CancellationToken token = tokenSource.Token;
                    log.Debug("CardRefundViewModel: Request for refund.");
                    var response = await tpsService.RefundRequestAsync("Credit",
                                                                       this.pumpId,
                                                                       posManager.ActiveSale.InvoiceNo,
                                                                       refundAmount,
                                                                       token);

                    if (response != null && response.RequestApproved)
                    {
                        log.Debug("CardRefundViewModel: RefundRequest response is not null and refund is approved.");
                        posManager.ActiveSale.RefundStatus = Enums.RefundApprovalStatus.RefundApproved;
                    }
                    else if (response == null)
                    {
                        log.Debug("CardRefundViewModel: Task is canceled.");
                        await this.navigationService.Close(this);
                    }
                    else
                    {
                        log.Debug("CardRefundViewModel: Refund is not approved.");
                        posManager.ActiveSale.RefundStatus = Enums.RefundApprovalStatus.RefundNotApproved;
                    }

                    if (response != null)
                    {
                        log.Debug("CardRefundViewModel: RefundRequest response is not null.Generate and print receipt.");
                        posManager.ActiveSale.RefundResponse = response;
                        posManager.ActiveSale.Receipt = response.Receipt;
                        GenerateReceipt();
                        await NavigateToPrintReceipt();
                    }
                }

            }
            catch (Exception ex)
            {
                log.DebugException(string.Format("CardRefundViewModel: Exception:{0} ", ex.Message), ex);
            }
        }

        private void GenerateReceipt()
        {
            log.Debug("CardRefundViewModel: Generating receipt.");

            if (App.CurrentFeature == Enums.FeatureType.PrepayRefund
                || App.CurrentFeature == Enums.FeatureType.DeletePrepay)
            {
                log.Debug("CardRefundViewModel: Generate receipt for full prepay refund.");
                posManager.ActiveSale.Receipt = receiptGenerator.FullPrepayRefundCard(posManager.ActiveSale);
            }
            else if (App.CurrentFeature == Enums.FeatureType.PrepayBasketRefund)
            {
                log.Debug("CardRefundViewModel: Generate receipt for partial prepay refund.");
                posManager.ActiveSale.Receipt = receiptGenerator.PartialPrepayRefundCard(posManager.ActiveSale);
            }
  
        }

        public override void ViewDisappearing()
        {
            log.Debug("CardRefundViewModel: View is disappearing.");
            base.ViewDisappearing();
            UnSubscribeAllEvents();
        }

        private void UnSubscribeAllEvents()
        {
            log.Debug("CardRefundViewModel: UnSubscribing All events.");
            App.CultureChange -= OnCultureChangeAsync;
        }

        private void SubscribeAllEvents()
        {
            log.Debug("CardRefundViewModel: Subscribing All events.");
            App.CultureChange += OnCultureChangeAsync;
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo cultureInfo)
        {
            await RaiseAllPropertiesChanged();
        }

        private async Task NavigateToPrintReceipt()
        {
            try
            {
                if(App.IsFcsConnected && App.IsTpsConnected)
                {
                    log.Debug("CardRefundViewModel: Navigate to print recipt page.");
                    await this.navigationService.Navigate<PrintReceiptViewModel, int>(pumpId);
                }
            }
            catch(Exception ex)
            {
                log.ErrorException(string.Format("CardTenderViewModel: Exception: {0} ", ex.Message), ex);
            }
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
                        log.Debug("CardRefundViewModel: Tps is not connected. Go to Tender Selection page again.");
                        await this.navigationService.Close(this);
                    };
                    goToTenderSelectionTimer.Enabled = true;

                }
                else
                {
                    log.Error("CardRefundViewModel: Fcs is not connected. Can't navigate to tender selection page.");
                }
            }
            catch (Exception ex)
            {
                log.DebugException(string.Format("CardRefundViewModel: Exception:{0}.", ex.Message), ex);
            }
        }
    }
}
