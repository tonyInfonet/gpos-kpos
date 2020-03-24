using InfonetPos.FcsIntegration.Entities.Receipt;
using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPOS.Core.Helpers;
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
    public class ReprintReceiptDataViewModel : MvxViewModel<string>
    {
        private readonly IMvxNavigationService navigationService;
        private readonly IMvxLog log;
        private readonly Printer printer;
        private readonly IFcsService fcsService;

        private string invoiceId;

        #region UI Properties
        public string Language => App.Language.ToString();
        public string Receipt { get; set; }
        #endregion

        #region Commands
        public IMvxCommand CancelCommand => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("ReprintReceiptDataViewModel: Cancel reprint receipt.Go to reprint receipt view model.");
                await this.navigationService.Close(this);
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("ReprintReceiptDataViewModel: Exception:{0}.", ex.Message), ex);
            }

        });

        public IMvxCommand PrintCommand => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("ReprintReceiptDataViewModel: Reprint receipt data and go to reprint receipt view model.");
                printer.PrintReportReceipt(Receipt);
                await this.navigationService.Navigate<ReprintReceiptViewModel>();
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("ReprintReceiptDataViewModel: Exception:{0}.", ex.Message), ex);
            }

        });
        #endregion

        public ReprintReceiptDataViewModel(IMvxNavigationService navigationService,
                                     IMvxLog log,
                                     Printer printer,
                                     IFcsService fcsService)
        {
            this.navigationService = navigationService;
            this.log = log;
            this.printer = printer;
            this.fcsService = fcsService;
        }

        public override async Task Initialize()
        {
            log.Debug("ReprintReceiptDataViewModel: View Model is initializing.");
            await base.Initialize();
            Receipt = "";
            await GetReceiptData();
            log.Debug("ReprintReceiptDataViewModel: View Model initialization is finished.");
        }

        public override void Prepare(string invoiceNo)
        {
            invoiceId = invoiceNo;
            log.Debug("ReprintReceiptDataViewModel: Receiving invoiceNo: {0} as parameter.", invoiceId);
        }

        public override void ViewAppearing()
        {
            log.Debug("ReprintReceiptDataViewModel: View is appearing.");
            base.ViewAppearing();
            App.CultureChange += OnCultureChangeAsync;
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo cultureInfo)
        {
            await RaiseAllPropertiesChanged();
        }

        public override void ViewDisappearing()
        {
            log.Debug("ReprintReceiptDataViewModel: View is disappearing.");
            base.ViewDisappearing();
            App.CultureChange -= OnCultureChangeAsync;
        }

        private async Task GetReceiptData()
        {
            try
            {
                log.Debug("ReprintReceiptDataViewModel: Trying to get receipt data for invoice id:{0}.", invoiceId);
                var response = await this.fcsService?.GetReceiptData(ReceiptType.PayInStore, invoiceId);
                if (response.ResultOK)
                {
                    log.Debug("ReprintReceiptDataViewModel: Successfully fetched receipt data.");
                    Receipt = response.Receipt;
                    log.Debug("ReprintReceiptDataViewModel: Receipt : {0}.", Receipt);
                }
                else
                {
                    log.Error("ReprintReceiptDataViewModel: Error while fetching receipt data.");
                }
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("ReprintReceiptDataViewModel: Exception:{0}", ex.Message), ex);
            }
        }
    }

}
