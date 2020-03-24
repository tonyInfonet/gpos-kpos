using InfonetPos.FcsIntegration.Entities.SiteConfiguration;
using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPOS.Core.DB.Entities;
using InfonetPOS.Core.DB.Enums;
using InfonetPOS.Core.DB.Interface;
using InfonetPOS.Core.Helpers;
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
    public class PrepayViewModel : MvxViewModel<int>
    {
        public string Language => App.Language.ToString();

        public int PumpId { get; set; }
        public override void Prepare(int pumpId)
        {
            PumpId = pumpId;
        }

        private readonly IFcsService fcsService;
        private readonly IMvxLog log;
        private readonly IMvxNavigationService navigationService;
        private readonly IDBAccess dbService;
        private readonly PosManager posManager;
        private readonly IMessageBoxService messageBoxService;

        public PrepayViewModel(
            IFcsService fcsService,
            IMvxLog log,
            IMvxNavigationService navigationService,
            IDBAccess dbService,
            PosManager posManager,
            IMessageBoxService messageBoxService)
        {
            this.fcsService = fcsService;
            this.log = log;
            this.navigationService = navigationService;
            this.dbService = dbService;
            this.posManager = posManager;
            this.messageBoxService = messageBoxService;
        }

        public override void ViewAppearing()
        {
            log.Debug("PrepayViewModel: View Appearing");
            base.ViewAppearing();
            App.CultureChange += OnCultureChangeAsync;
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo cultureInfo)
        {
            await RaiseAllPropertiesChanged();
        }

        public override void ViewDisappearing()
        {
            log.Debug("PrepayViewModel: View Disappearing");
            base.ViewDisappearing();
            App.CultureChange -= OnCultureChangeAsync;
        }

        public async override Task Initialize()
        {
            log.Debug("PrepayViewModel: ViewModel is initializing.");
            await base.Initialize();
            IsDecimalButtonEnabled = true;
            if (!(await LoadGrades()))
            {
                await CancelPrepaySale();
                await GoHome();
            }
            return;
        }

        private async Task<bool> LoadGrades()
        {
            log.Debug("PrepayViewModel: Loading grades.");
            var pump = fcsService.FCSConfig.Pumps
                .Where(_pump => _pump.PumpID == PumpId.ToString())
                .Single();

            SupportedGrades = new MvxObservableCollection<Grade>();

            foreach (var pumpGrade in pump.Grades)
            {
                try
                {
                    var grade = fcsService.FCSConfig.FuelPrice.Grades
                                .Where(_grade => _grade.Type == pumpGrade.Type)
                                .Single();
                    SupportedGrades.Add(grade);
                }
                catch { }
            }

            if (SupportedGrades.Count <= 0)
            {
                return false;
            }
            else
            {
                SelectedGrade = SupportedGrades[0];
                return true;
            }
        }

        public Grade SelectedGrade { get; set; }
        public MvxObservableCollection<Grade> SupportedGrades { get; set; }
        public double Amount { get; set; }
        public string CurrentAmountInString { get; set; }
        public bool IsDecimalButtonEnabled { get; set; }

        private bool IsInputValid()
        {
            log.Debug("PrepayViewModel: Checking if input is valid or not.");
            return Amount > 0
                && SelectedGrade != null;
        }

        private void UpdateActiveSale()
        {
            posManager.ActiveSale.Amount = Amount;
            posManager.ActiveSale.SaleGrade = SelectedGrade;
            log.Debug("PrepayViewModel: Updated active sale with amount:{0} , grade:{1}", posManager.ActiveSale.Amount, posManager.ActiveSale.SaleGrade);
        }

        private async Task CancelPrepaySale()
        {
            log.Debug("PrepayViewModel: Canceling prepay sale.");
            await posManager.CleanUpSale(posManager.ActiveSale, PumpId, false);
        }

        private async Task GoHome()
        {
            log.Debug("PrepayViewModel: Closing navigation.");
            await navigationService.Close(this);
        }

        public IMvxCommand CancelCommand => new MvxCommand(async () =>
        {
            log.Debug("PrepayViewModel: User pressed cancel command.Cacel prepay.");
            await CancelPrepaySale();
            await GoHome();
        });

        public IMvxCommand AuthorizeForPumpTestCmd => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("PrepayViewModel: Going to ConfirmationViewModel for pump test authorization");
                if ((await ConfirmPumpTestAuthorization()))
                {
                    log.Debug("PrepayViewModel: User confirmded pump test authorization. Cancelling the prepay sale.");
                    await CancelPrepaySale();

                    log.Debug("PrepayViewModel: Setting pump on now");
                    var response = await fcsService.SetPumpOnNow(PumpId);
                    if (response.ResultOK)
                    {
                        log.Debug("PrepayViewModel: Set pump on now - successful");
                    }
                    else
                    {
                        log.Debug("PrepayViewModel: Set pump on now - failed");
                        messageBoxService.ShowMessageBox("CantAuthorizeForPumpTest", MessageBoxButtonType.OK);
                    }

                    await GoHome();
                }
            }
            catch (Exception ex)
            {
                log.ErrorException("PrepayViewModel: ", ex);
            }
        });

        public IMvxCommand DigitDeleteCommmand => new MvxCommand(() =>
        {
            if (this.CurrentAmountInString.Length > 0)
            {
                if (this.CurrentAmountInString[this.CurrentAmountInString.Length - 1] == '.')
                    this.IsDecimalButtonEnabled = true;
                this.CurrentAmountInString = this.CurrentAmountInString?.Remove(this.CurrentAmountInString.Length - 1);
            }
        });

        public IMvxCommand DigitEntryCommand => new MvxCommand<string>(CurrentDigits =>
        {
            if (CurrentDigits == ".")
                this.IsDecimalButtonEnabled = false;
            this.CurrentAmountInString += CurrentDigits;
        });

        public IMvxCommand AmountEntryCommand => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("PrepayViewModel: User entered amount.Set amount and process inputs.");
                SetAmount();
                await ProcessInputs();
            }
            catch (Exception ex)
            {
                log.ErrorException("PrepayViewModel: ", ex);
            }
        });

        private async Task ProcessInputs()
        {
            if (IsInputValid())
            {
                log.Debug("PrepayViewModel: Input is valid.Update active sale.");
                UpdateActiveSale();
                await navigationService.Navigate<TenderSelectionViewModel, int>(PumpId);
            }
            else
            {
                log.Error("PrepayViewModel: Input is invalid.");
            }
        }

        private void SetAmount()
        {
            if (this.CurrentAmountInString != null && this.CurrentAmountInString.Length > 0)
            {
                log.Info("PrepayViewModel: Customer Entry amount {0} for fueling.", Amount);
                if (this.CurrentAmountInString[this.CurrentAmountInString.Length - 1] == '.')
                    this.CurrentAmountInString += "0";
                Amount = Convert.ToDouble(this.CurrentAmountInString);
                log.Debug("PrepayViewModel: Navigate to PaymentProcessing page.");
            }
            else
            {
                log.Error("PrepayViewModel: CurrentAmount is null or empty.");
            }
        }

        private async Task<bool> ConfirmPumpTestAuthorization()
        {
            log.Debug("PrepayViewModel: Confirming pump test authorization.");
            return await navigationService.Navigate<ConfirmationViewModel, string, bool>("WantToAuthorizeForPumpTest");
        }
    }
}
