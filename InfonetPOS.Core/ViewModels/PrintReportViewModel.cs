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
    public class PrintReportViewModel : MvxViewModel<string>
    {
        private readonly IMvxNavigationService navigationService;
        private readonly IMvxLog log;
        private readonly Printer printer;

        private string report;

        #region UI Properties
        public string Language => App.Language.ToString();
        #endregion

        public PrintReportViewModel(IMvxLog log,
            Printer printer,
            IMvxNavigationService navigationService)
        {
            this.log = log;
            this.printer = printer;
            this.navigationService = navigationService;
        }

        public override async void ViewAppeared()
        {
            log.Debug("PrintReportViewModel: View is appeared.");
            base.ViewAppeared();
            App.CultureChange += OnCultureChangeAsync;

            try
            {
                log.Debug("PrintReportViewModel: Printing report.");
                printer.PrintReportReceipt(report);
                await this.navigationService.Navigate<HomeViewModel>();
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("PrintReportViewModel: Exception:{0}. ", ex.Message), ex);
            }
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo e)
        {
            await RaiseAllPropertiesChanged();
        }

        public override void ViewDisappeared()
        {
            log.Debug("PrintReportViewModel: View is disappeared.");
            base.ViewDisappeared();
            App.CultureChange -= OnCultureChangeAsync;
        }

        public override void Prepare(string report)
        {
            log.Debug("PrintReportViewModel: Receiving parameter report: {0}", report);
            this.report = report;
        }
    }
}
