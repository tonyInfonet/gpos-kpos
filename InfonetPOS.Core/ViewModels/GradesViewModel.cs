using InfonetPos.FcsIntegration.Entities.SetPrice;
using InfonetPos.FcsIntegration.Entities.SiteConfiguration;
using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPOS.Core.Entities;
using InfonetPOS.Core.Enums;
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
    public class GradesViewModel : MvxViewModel
    {
        #region setup
        public string Language => App.Language.ToString();
        public DecimalPlace FuelUnitPriceDecimal { get; private set; }
        private readonly IFcsService fcsService;
        private readonly IMvxLog log;
        private readonly IMvxNavigationService navigationService;
        private readonly IAppSettings appSettings;
        private readonly IMessageBoxService messageBoxService;

        public GradesViewModel(IFcsService fcsService,
            IMvxLog log,
            IMvxNavigationService navigationService,
            IAppSettings appSettings,
            IMessageBoxService messageBoxService)
        {
            this.fcsService = fcsService;
            this.log = log;
            this.navigationService = navigationService;
            this.appSettings = appSettings;
            this.messageBoxService = messageBoxService;
        }

        public override async Task Initialize()
        {
            log.Debug("GradesViewModel: ViewModel is initializing.");
            await base.Initialize();
            FuelUnitPriceDecimal = appSettings.FuelUnitPriceDecimal;
            IsDecimalButtonEnabled = true;
            GetGrades();
            log.Debug("HomeViewModel: ViewModel initialization is finished.");
            return;
        }

        public override void ViewAppearing()
        {
            log.Debug("GradesViewModel: View is appearing.");
            base.ViewAppearing();
            App.CultureChange += OnCultureChangeAsync;
            fcsService.FcsReceivedConfigurationEvent += OnFcsConfigChange;
            fcsService.FuelPriceChanged += OnFuelPriceChange;
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo cultureInfo)
        {
            await RaiseAllPropertiesChanged();
        }

        public override void ViewDisappearing()
        {
            log.Debug("GradesViewModel: View is disappearing.");
            base.ViewDisappearing();
            App.CultureChange -= OnCultureChangeAsync;
            fcsService.FcsReceivedConfigurationEvent -= OnFcsConfigChange;
            fcsService.FuelPriceChanged -= OnFuelPriceChange;
        }
        #endregion

        #region grades list
        public MvxObservableCollection<GradeDetails> Grades { get; set; }
        public GradeDetails SelectedGrade { get; set; }

        private void OnFuelPriceChange(object sender, PriceChangedEvent e)
        {
            log.Debug("GradesViewModel: Fuel price is changed.Get grades.");
            GetGrades();
        }

        private void OnFcsConfigChange(object sender, ConfigurationRequest e)
        {
            log.Debug("GradesViewModel: Fcs configuration is changed.Get grades.");
            GetGrades();
        }

        private void GetGrades()
        {
            log.Debug("GradesViewModel: Trying to get grades.");
            var newGrades = new MvxObservableCollection<GradeDetails>();
            if (fcsService.FCSConfig == null)
            {
                log.Error("GradesViewModel: Fcs configuration is null.");
                return;
            }
            fcsService.FCSConfig.FuelPrice.Grades.ForEach(
                grade => newGrades.Add(new GradeDetails()
                {
                    Decimal = FuelUnitPriceDecimal,
                    CurrentData = grade
                }));
            SelectedGrade = null;
            Grades = newGrades;
            log.Debug("GradesViewModel: Finished fetching all grades.");
        }
        #endregion

        #region amount entry
        public string CurrentAmountInString { get; set; }
        public bool IsDecimalButtonEnabled { get; set; }

        public IMvxCommand DigitDeleteCommmand => new MvxCommand(() =>
        {
            log.Debug("GradesViewModel: Deleting last digit.");
            if (this.CurrentAmountInString.Length > 0)
            {
                log.Debug("GradesViewModel: There is at least one digit to delete.");
                if (this.CurrentAmountInString[this.CurrentAmountInString.Length - 1] == '.')
                    this.IsDecimalButtonEnabled = true;
                this.CurrentAmountInString = this.CurrentAmountInString?.Remove(this.CurrentAmountInString.Length - 1);
            }
            else
            {
                log.Error("GradesViewModel: There is no digit to delete.");
            }
        });

        public IMvxCommand DigitEntryCommand => new MvxCommand<string>(CurrentDigits =>
        {
            log.Debug("GradesViewModel: User entered  {0}", CurrentDigits);
            if (CurrentDigits == ".")
                this.IsDecimalButtonEnabled = false;
            this.CurrentAmountInString += CurrentDigits;
        });

        public IMvxCommand AmountEntryCommand => new MvxCommand(() =>
        {
            try
            {
                log.Debug("GradesViewModel: User is setting amount.");
                if (this.CurrentAmountInString != null && this.CurrentAmountInString.Length > 0)
                {
                    log.Debug("GradesViewModel: Propose new price: {0}", CurrentAmountInString);
                    if (this.CurrentAmountInString[this.CurrentAmountInString.Length - 1] == '.')
                        this.CurrentAmountInString += "0";
                    ProposeNewPrice(Convert.ToDouble(this.CurrentAmountInString));
                }
                else
                {
                    log.Error("GradesViewModel: CurrentAmount is null or empty.");
                }
            }
            catch (Exception ex)
            {
                log.ErrorException("GradesViewModel: ", ex);
            }
        });
        #endregion

        #region set price
        private void ProposeNewPrice(double amount)
        {
            if (SelectedGrade == null)
            {
                log.Error("GradesViewModel: Selected Grade is null.");
                return;
            }

            SelectedGrade.NewPrice = amount;
            log.Info("GradesViewModel: Entry amount {0} for new price of {1}.", amount, SelectedGrade.CurrentData.Type);
        }

        public IMvxCommand CancelCmd => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("GradesViewModel: User pressed cancel button.Go to home page");
                await this.navigationService.Close(this);
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("GradesViewModel: Exception:{0}", ex.Message), ex);
            }

        });

        private List<PriceChange> GetChanges()
        {
            log.Debug("GradesViewModel: Fetching all price changes.");
            var changes = new List<PriceChange>();
            int decimalPlaces = FuelUnitPriceDecimal == DecimalPlace.N2 ? 2 : 3;

            foreach (var grade in Grades)
            {
                var newPrice = grade.NewPrice > 0
                    ? grade.NewPrice : grade.CurrentData.Price.UnitPrice;

                changes.Add(new PriceChange(true)
                {
                    DecimalPlaces = decimalPlaces,
                    Grade = grade.CurrentData.Type,
                    TierLevel = "Day",
                    CashPrice = newPrice,
                    CreditPrice = newPrice
                });
            }
            log.Debug("GradesViewModel: Finished fetching all price changes.");
            return changes;
        }

        public IMvxCommand SetCmd => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("GradesViewModel: Sending set price request to FCS");
                var response = await fcsService.SetPrice(GetChanges());
                if (response.ResultOK)
                {
                    log.Debug("GradesViewModel: FCS set price request successful");
                }
                else
                {
                    log.Debug("GradesViewModel: FCS set price request failed");
                    messageBoxService.ShowMessageBox("CantSetPrice", MessageBoxButtonType.OK);
                }
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("GradesViewModel: Exception:{0}", ex.Message), ex);
            }

        });
        #endregion
    }
}
