using InfonetPos.FcsIntegration.Entities.Receipt;
using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPOS.Core.Helpers;
using InfonetPOS.Core.Interfaces;
using MvvmCross.Commands;
using MvvmCross.Logging;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class ReprintReceiptViewModel : MvxViewModel
    {
        private readonly IMvxNavigationService navigationService;
        private readonly IMvxLog log;
        private readonly IFcsService fcsService;
        private readonly IAppSettings appSettings;

        private DateTime selectedDate;
        private string minimumInvoiceNo;
        private string maximumInvoiceNo;

        #region UI Properties
        public string Language => App.Language.ToString();
        public string DateToShow { get; set; }
        public DateTime SelectedDate
        {
            get
            {
                return selectedDate;
            }
            set
            {
                minimumInvoiceNo = "0";
                maximumInvoiceNo = "0";
                selectedDate = value;
                new Task(async () =>
                {
                    await ShowReceipts(minimumInvoiceNo, ReceiptOrder.Descending);
                }).Start();
            }
        }
        public List<ReceiptInfo> ReceiptData { get; set; }
        public bool PreviousButtonVisibility { get; set; }
        public bool NextButtonVisibility { get; set; }
        #endregion

        #region Commands

        public IMvxCommand PreviousCommand => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("ReprintReceiptViewModel: Show previous {0} Receipts.", appSettings.NoOfReceiptsToShow);
                await ShowReceipts(minimumInvoiceNo, ReceiptOrder.Descending);
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("ReprintReceiptViewModel: Exception:{0}.", ex.Message), ex);
            }

        });

        public IMvxCommand NextCommand => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("ReprintReceiptViewModel: Show next {0} Receipts.", appSettings.NoOfReceiptsToShow);
                await ShowReceipts(maximumInvoiceNo, ReceiptOrder.Ascending);
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("ReprintReceiptViewModel: Exception:{0}.", ex.Message), ex);
            }

        });

        public IMvxCommand CancelCommand => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("ReprintReceiptViewModel: Cancel reprint receipt.Go to home view model.");
                await this.navigationService.Close(this);
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("ReprintReceiptViewModel: Exception:{0}.", ex.Message), ex);
            }

        });

        public IMvxCommand PreviousDayCommand => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("ReprintReceiptViewModel: Trying to process the previous day's receipt list.");
                SelectedDate = SelectedDate.AddDays(-1);
                log.Debug("ReprintReceiptViewModel: Selected date:{0}.", SelectedDate.ToString());
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("ReprintReceiptViewModel: Exception:{0}.", ex.Message), ex);
            }

        });

        public IMvxCommand NextDayCommand => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("ReprintReceiptViewModel: Trying to process the next day's receipt list.");
                SelectedDate = SelectedDate.AddDays(1);
                log.Debug("ReprintReceiptViewModel: Selected date:{0}.", SelectedDate.ToString());
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("ReprintReceiptViewModel: Exception:{0}.", ex.Message), ex);
            }

        });

        #endregion

        public ReprintReceiptViewModel(IMvxLog log,
                                       IMvxNavigationService navigationService,
                                       IFcsService fcsService,
                                       IAppSettings appSettings)
        {
            this.log = log;
            this.navigationService = navigationService;
            this.fcsService = fcsService;
            this.appSettings = appSettings;
        }

        public override async Task Initialize()
        {
            log.Debug("ReprintReceiptViewModel: View Model is initialized.");
            await base.Initialize();
            SelectedDate = DateTime.Now;
        }

        public override void ViewAppearing()
        {
            log.Debug("ReprintReceiptViewModel: View is appearing.");
            base.ViewAppearing();
            App.CultureChange += OnCultureChangeAsync;
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo cultureInfo)
        {
            await RaiseAllPropertiesChanged();
        }

        public override void ViewDisappearing()
        {
            log.Debug("ReprintReceiptViewModel: View is disappearing.");
            base.ViewDisappearing();
            App.CultureChange -= OnCultureChangeAsync;
        }

        public async void ReprintSelectedReceipt(object selectedReceipt)
        {
            ReceiptInfo receiptToProceed = selectedReceipt as ReceiptInfo;
            log.Debug("ReprintReceiptViewModel: Go to ReprintReceiptDataViewModel and reprint receipt for invoiceNo:{0}.", receiptToProceed.InvoiceNumber);
            await this.navigationService.Navigate<ReprintReceiptDataViewModel, string>(receiptToProceed.InvoiceNumber);
        }

        private async Task<List<ReceiptInfo>> GetReceipts(string invoiceNo, ReceiptOrder receiptOrder)
        {
            log.Debug("ReprintReceiptViewModel: Trying to get receipts.");
            List<ReceiptInfo> receiptData = null;
            GetReceiptCriteria criteria = new GetReceiptCriteria()
            {
                Date = SelectedDate,
                StartInvoiceNumber = new ReceiptStartInvoiceNumber()
                {
                    InvoiceNumber = invoiceNo,
                    ReceiptOrder = receiptOrder
                },
                Count = appSettings.NoOfReceiptsToShow
            };

            log.Debug("ReprintReceiptViewModel: Trying to fetch all receipts");
            var response = await this.fcsService?.GetReceipt(ReceiptType.PayInStore, criteria);
            if (response.ResultOK)
            {
                log.Debug("ReprintReceiptViewModel: Fetched all receipts successfully");
                receiptData = response.Data;
                if (receiptOrder == ReceiptOrder.Ascending)
                {
                    receiptData.Reverse();
                }
                log.Debug("ReprintReceiptViewModel: Receipt Data:{0}.", ReceiptData);
            }
            else
            {
                log.Error("ReprintReceiptViewModel: Can't fetch receipts data properly.");
            }

            return receiptData;
        }

        private async Task ShowReceipts(string invoiceNo, ReceiptOrder receiptOrder)
        {
            try
            {
                DateToShow = SelectedDate.ToString("dddd MMM dd, yyyy");
                log.Debug("ReprintReceiptViewModel: Trying to fetch receipt data.");
                ReceiptData = await GetReceipts(invoiceNo, receiptOrder);
                SetMaximumAndMinimumInvoiceNo();

            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("ReprintReceiptViewModel: Exception:{0}", ex.Message), ex);
            }
        }

        private async void SetMaximumAndMinimumInvoiceNo()
        {
            log.Debug("ReprintReceiptViewModel: Setting Maximum and minimum Invoice no.");
            if (ReceiptData != null && ReceiptData.Count > 0)
            {
                maximumInvoiceNo = ReceiptData[0].InvoiceNumber;
                minimumInvoiceNo = ReceiptData[ReceiptData.Count - 1].InvoiceNumber;
                var response = await GetReceipts(minimumInvoiceNo, ReceiptOrder.Descending);
                PreviousButtonVisibility = response.Count > 1;
                response = await GetReceipts(maximumInvoiceNo, ReceiptOrder.Ascending);
                NextButtonVisibility = response.Count > 1;
            }
            else
            {
                maximumInvoiceNo = "0";
                minimumInvoiceNo = "0";
            }

            log.Debug("ReprintReceiptViewModel: MaximumInvoiceNo: {0}, MinimumInvoiceNo: {1}.", maximumInvoiceNo, minimumInvoiceNo);
        }
    }
}
