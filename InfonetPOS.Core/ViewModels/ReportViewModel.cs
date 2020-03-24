using InfonetPos.FcsIntegration.Entities.Report;
using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPOS.Core.Helpers;
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
    public class ReportViewModel : MvxViewModel<ReportType>
    {
        private readonly IMvxNavigationService navigationService;
        private readonly ITpsService tpsService;
        private readonly IFcsService fcsService;
        private readonly IMvxLog log;
        private readonly Printer printer;

        #region Private Properties
        private ReportType reportType;
        #endregion

        #region UI Properties
        public string Language => App.Language.ToString();
        public string ReportType { get => reportType.ToString(); }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Report { get; set; }
        public bool ReportScrollViewerVisibility { get; set; }
        #endregion

        #region Commands
        public IMvxCommand GetReportCommand => new MvxCommand(async () =>
        {
            log.Debug("ReportViewModel: Processing report..");
            try
            {
                ReportScrollViewerVisibility = true;
                await GetReport();
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("ReportViewModel: Exception: {0}.", ex.Message), ex);
            }
        });

        public IMvxCommand CancelReportCommand => new MvxCommand(async () =>
        {
            log.Debug("ReportViewModel: Canceling report..");
            try
            {
                log.Debug("ReportViewModel: Cancel report.Go to home view model.");
                await this.navigationService.Close(this);
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("ReportViewModel: Exception: {0}.", ex.Message), ex);
            }
        });

        public IMvxCommand PrintReportCommand => new MvxCommand(async () =>
        {
            log.Debug("ReportViewModel: Processing Print report..");
            try
            {
                if (Report.Length == 0 || Report == null)
                {
                    await GetReport();
                }
                log.Debug("ReportViewModel: Go to print report view model.");
                await this.navigationService.Navigate<PrintReportViewModel, string>(Report);
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("ReportViewModel: Exception: {0}.", ex.Message), ex);
            }
        });
        #endregion

        public ReportViewModel(IMvxNavigationService navigationService,
                               ITpsService tpsService,
                               IFcsService fcsService,
                               IMvxLog log,
                               Printer printer)
        {
            this.navigationService = navigationService;
            this.tpsService = tpsService;
            this.fcsService = fcsService;
            this.log = log;
            this.printer = printer;
        }

        public override async void ViewAppearing()
        {
            log.Debug("ReportViewModel: View is appearing.");
            App.CultureChange += OnCultureChangeAsync;
            Report = "";
            StartDate = DateTime.Now;
            EndDate = DateTime.Now;
            ReportScrollViewerVisibility = false;
            if (this.reportType == InfonetPos.FcsIntegration.Entities.Report.ReportType.CurrentSales)
            {
                log.Debug("ReportViewModel: Report type is CurrentSales.Set ReportScrollViewerVisibility true.");
                ReportScrollViewerVisibility = true;
                await GetReport();
            }
            base.ViewAppearing();
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo cultureInfo)
        {
            await RaiseAllPropertiesChanged();
        }

        public override void ViewDisappearing()
        {
            log.Debug("ReportViewModel: View is disappearing.");
            base.ViewDisappearing();
            App.CultureChange -= OnCultureChangeAsync;
        }

        public override void Prepare(ReportType reportType)
        {
            log.Debug("ReportViewModel: Receiving report type:{0} as parameter.", reportType.ToString());
            this.reportType = reportType;
        }

        #region All Private Methods
        private async Task GetReport()
        {
            log.Debug("ReportViewModel: Requesting for report type:{0}.", reportType.ToString());

            if (this.reportType == InfonetPos.FcsIntegration.Entities.Report.ReportType.EOD
              || this.reportType == InfonetPos.FcsIntegration.Entities.Report.ReportType.EODDetail)
            {
                EndDate = StartDate;
            }

            GetReportCriteria criteria = new GetReportCriteria()
            {
                Period = new Period() { StartDay = StartDate, EndDay = EndDate },
                SaleType = new ReportRequestSaleType() { Type = ReportCriteriaSaleType.All }
            };

            var response = await fcsService.GetReport(reportType, criteria);
            if (response.ResultOK)
            {
                Report = response.Report;
                log.Debug("ReportViewModel: Get Report: {0}", Report);
            }
            else
            {
                log.Error("ReportViewModel: Report response error.");
            }
        }
        #endregion
    }
}
