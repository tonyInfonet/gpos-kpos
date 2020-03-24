using InfonetPOS.Core.Helpers;
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
    public class PrintReceiptViewModel : MvxViewModel<int>
    {
        public string Language => App.Language.ToString();

        private int pumpId;
        public override void Prepare(int parameter)
        {
            log.Debug("PrintReceiptViewModel: Receiving pumpId:{0} as parameter.", parameter);
            pumpId = parameter;
        }

        private readonly IMvxNavigationService navigationService;
        private readonly IMvxLog log;
        private readonly Printer printer;
        private readonly PosManager posManager;

        public PrintReceiptViewModel(IMvxLog log,
            Printer printer,
            PosManager posManager,
            IMvxNavigationService navigationService)
        {
            this.log = log;
            this.printer = printer;
            this.posManager = posManager;
            this.navigationService = navigationService;
        }

        public override async void ViewAppeared()
        {
            log.Debug("PrintReceiptViewModel: View is appeared.");
            base.ViewAppeared();
            App.CultureChange += OnCultureChangeAsync;

            try
            {
                log.Debug("PrintReceiptViewModel: Printing receipt.");
                Print();
                await Navigate();
            }
            catch (Exception ex)
            {
                log.ErrorException("PrintReceiptViewModel: ", ex);
            }
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo cultureInfo)
        {
            await RaiseAllPropertiesChanged();
        }

        public override void ViewDisappeared()
        {
            log.Debug("PrintReceiptViewModel: View is disappeared.");
            base.ViewDisappeared();
            App.CultureChange -= OnCultureChangeAsync;
        }

        private void Print()
        {
            if (posManager.ActiveSale.Receipt?.Length > 0)
            {
                log.Debug("PrintReceiptViewModel: Print from receipt.");
                printer.PrintDocument(posManager.ActiveSale.Receipt);
            }
            else
            {
                log.Debug("PrintReceiptViewModel: Print from active sale.");
                printer.PrintReceipt(posManager.ActiveSale);
            }
        }

        private async Task Navigate()
        {
            log.Debug("PrintReceiptViewModel: Feature type:{0}.", App.CurrentFeature.ToString());

            if (App.CurrentFeature == Enums.FeatureType.Prepay
                    || App.CurrentFeature == Enums.FeatureType.Postpay)
            {
                log.Debug("PrintReceiptViewModel: Feature type is prepay or postpay.Go to payment status page.");
                await GoToPaymentStatus();
            }
            else if (App.CurrentFeature == Enums.FeatureType.PrepayBasketRefund
                || App.CurrentFeature == Enums.FeatureType.PrepayRefund
                || App.CurrentFeature == Enums.FeatureType.DeletePrepay)
            {
                log.Debug("PrintReceiptViewModel: Feature type is refund. Go to refund status page.");
                await GoToRefundStatus();
            }
            else if (App.CurrentFeature == Enums.FeatureType.DriveOff
                || App.CurrentFeature == Enums.FeatureType.PumpTest)
            {
                log.Debug("PrintReceiptViewModel: Feature type is driveoff or pumptest. Go to home page.");
                await GoToHome();
            }
        }

        private async Task GoToPaymentStatus()
        {
            log.Debug("PrintReceiptViewModel: Receipt printed.Go to payment status view model.");
            await this.navigationService.Navigate<PaymentStatusViewModel, int>(this.pumpId);
        }

        private async Task GoToHome()
        {
            log.Debug("PrintReceiptViewModel: Receipt printed.Clean sale and Go to Home view model.");
            await posManager.CleanUpSale(posManager.ActiveSale, this.pumpId, true);
            await this.navigationService.Navigate<HomeViewModel>();
        }

        private async Task GoToRefundStatus()
        {
            log.Debug("PrintReceiptViewModel: Receipt printed.Go to refund status view model.");
            await this.navigationService.Navigate<RefundStatusViewModel>();
        }
    }
}
