using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using InfonetPOS.Core.Entities;
using MvvmCross.Logging;
using InfonetPOS.Core.Interfaces;
using InfonetPOS.Core.Enums;
using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPos.FcsIntegration.Entities;
using InfonetPos.FcsIntegration.Entities.SiteConfiguration;
using System.Threading;
using MvvmCross.Commands;
using InfonetPOS.Core.Helpers;
using InfonetPOS.Core.TPS.Services.Interfaces;
using InfonetPos.FcsIntegration.Entities.Report;
using System.Globalization;

namespace InfonetPOS.Core.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class HomeViewModel : MvxViewModel
    {
        private readonly IMvxNavigationService navigationService;
        private readonly IMvxLog log;
        private readonly IAppSettings appSettings;
        private readonly IFcsService fcsService;
        private readonly ITpsService tpsService;
        private readonly PosManager posManager;
        private readonly IMessageBoxService messageBoxService;

        #region UI properties
        public MvxObservableCollection<PumpDetails> Pumps { get; set; }
        public bool IsSignOnDone { get; set; }
        public bool IsFcsConnected { get; set; }
        public string Language => App.Language.ToString();

        public bool IsKPOS
        {
            get => App.PosType == InfonetPos.FcsIntegration.Enums.POSType.KPOS;
        }
        #endregion

        public HomeViewModel(
            IMvxNavigationService navigationService,
            IMvxLog log,
            IAppSettings appSettings,
            IFcsService fcsService,
            PosManager posManager,
            ITpsService tpsService,
            IMessageBoxService messageBoxService)
        {
            this.navigationService = navigationService;
            this.log = log;
            this.appSettings = appSettings;
            this.fcsService = fcsService;
            this.posManager = posManager;
            this.tpsService = tpsService;
            this.messageBoxService = messageBoxService;
        }

        public override async Task Initialize()
        {
            log.Debug("HomeViewModel: ViewModel is initializing.");
            await base.Initialize();
            App.CurrentFeature = FeatureType.None;
            ClearSwitchingAttempt();
            this.IsFcsConnected = App.IsFcsConnected;
            IsSignOnDone = App.IsSignOnDone;
            await SignOnFcs();
            GetPumpsDetails();
            log.Debug("HomeViewModel: Finished initializing view model.");
        }

        public override void ViewAppearing()
        {
            log.Debug("HomeViewModel: View is appearing.");
            App.CurrentFeature = FeatureType.None;
            SubscribeAllEvents();
            base.ViewAppearing();
            posManager.ClearActiveSale();
            posManager.CanCloseApp = true;
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo cultureInfo)
        {
            await RaiseAllPropertiesChanged();
        }

        public override void ViewDisappearing()
        {
            log.Debug("HomeViewModel: View is disappearing.");
            base.ViewDisappearing();
            UnSubscribeAllEvents();
            posManager.CanCloseApp = false;
        }

        #region subscription
        private void SubscribeAllEvents()
        {
            log.Debug("HomeViewModel: Subscribing all events.");
            App.CultureChange += OnCultureChangeAsync;
            fcsService.ConnectionStatusChanged += OnFcsConnectionStatusChanged;
            fcsService.FcsStatusChanged += OnFcsStatusChanged;
            fcsService.FcsReceivedConfigurationEvent += OnFcsConfigChange;
            posManager.BasketChangedEvent += OnBasketChange;
            posManager.PrepaySalePumpSwitchEvent += OnPrepaySalePumpSwitch;
        }
        private void UnSubscribeAllEvents()
        {
            log.Debug("HomeViewModel: Unsubscribing all events.");
            App.CultureChange -= OnCultureChangeAsync;
            fcsService.ConnectionStatusChanged -= OnFcsConnectionStatusChanged;
            fcsService.FcsStatusChanged -= OnFcsStatusChanged;
            fcsService.FcsReceivedConfigurationEvent -= OnFcsConfigChange;
            posManager.BasketChangedEvent -= OnBasketChange;
            posManager.PrepaySalePumpSwitchEvent -= OnPrepaySalePumpSwitch;
        }
        #endregion

        private void OnPrepaySalePumpSwitch(object sender, KeyValuePair<int, int> e)
        {
            int pumpId = e.Key;
            int oldPumpId = e.Value;

            log.Debug("HomeViewModel: Prepay sale pump switch from {0} to {0}", pumpId, oldPumpId);

            UpdatePumpPrepayAmount(pumpId);
            UpdatePumpPrepayAmount(oldPumpId);
        }

        private void UpdatePumpPrepayAmount(int pumpId)
        {
            log.Debug("HomeViewModel: Update pump {0} prepay amount.", pumpId);
            var pumpDetails = Pumps?.FirstOrDefault(pump => pump?.Id == pumpId);
            if (pumpDetails != null)
            {
                log.Debug("HomeViewModel: Pump details is not null.Can update prepay amount.");
                pumpDetails.PrepayAmount = posManager.GetPrepayAmount(pumpId);
            }
            else
            {
                log.Error("HomeViewModel: Pump details is null.Can't update prepay amount.");
            }
        }

        private void OnBasketChange(object sender, int pumpId)
        {
            log.Debug("HomeViewModel: Basket Changed for pumpId:{0}", pumpId);
            var pumpDetails = Pumps?.FirstOrDefault(pump => pump?.Id == pumpId);
            if (pumpDetails != null)
            {
                log.Debug("HomeViewModel: Pump details is not null.Can update the basket list.");
                pumpDetails.Baskets = posManager.GetBasketsForPump(pumpId);
            }
            else
            {
                log.Error("HomeViewModel: Pump details is null.Can't update the basket list.");
            }
        }

        private async void OnFcsConnectionStatusChanged(object sender, bool fcsConnectionStatus)
        {
            log.Debug("HomeViewModel: Fcs Connection status changed.");
            IsFcsConnected = fcsConnectionStatus;
            await SignOnFcs();
        }

        private async Task SignOnFcs()
        {
            log.Debug("HomeViewModel: Trying to SignOn fcs.");
            if (IsFcsConnected)
            {
                log.Debug("HomeViewModel: Fcs is connected.");
                IsSignOnDone = App.IsSignOnDone;
                if (!IsSignOnDone)
                {
                    log.Debug("HomeViewModel: Sign on done is false.Trying to sign on with username:{0},TillNo:{1},shift:{2}", posManager.UserName, posManager.TillNo, posManager.ShiftNo); ;
                    await App.SignOnAsync(posManager.UserName, posManager.TillNo, posManager.ShiftNo);
                    IsSignOnDone = App.IsSignOnDone;
                    log.Debug("HomeViewModel: Sign on Done is:{0}", IsSignOnDone.ToString());
                }
            }
            else
            {
                log.Debug("HomeViewModel: Fcs is not connected.Sign on done false");
                IsSignOnDone = false;
            }
        }

        private async void OnFcsStatusChanged(object sender, FcsStatus e)
        {
            log.Debug("HomeViewModel: Fcs status has changed.");
            GetPumpsDetails();
        }

        private void OnFcsConfigChange(object sender, ConfigurationRequest e)
        {
            log.Debug("HomeViewModel: Fcs configuration is changed.");
            GetPumpsDetails();
            posManager.ProcessQueuedBaskets();
        }

        private void GetPumpsDetails()
        {
            log.Debug("HomeViewModel: try to get pumps details.");
            if (fcsService.CurrentFcsStatus == null
                || fcsService.FCSConfig == null
                || fcsService.FCSConfig.Pumps == null
                || fcsService.FCSConfig.Pumps.Count <= 0)
            {
                log.Debug("HomeViewModel: GetPumpDetails - Pumps not found in config.");
                return;
            }

            log.Debug("HomeViewModel: Get all pumps details.");

            var newPumpList = new MvxObservableCollection<PumpDetails>();

            var supportedPumpIds = posManager.GetSupportedPumpIds();
            if (supportedPumpIds == null)
            {
                log.Debug("HomeViewModel: GetPumpDetails - Pumps not found ... .");
                return;
            }

            foreach (var pumpConfig in fcsService.FCSConfig.Pumps)
            {
                if (!int.TryParse(pumpConfig.PumpID, out int pumpId)
                    || !supportedPumpIds.Contains(pumpId))
                {
                    log.Debug("HomeViewModel: Invalid pump id.");
                    continue;
                }

                var pumpStatus = this.fcsService.CurrentFcsStatus?.GetPumpStatus(pumpId).ToString();
                pumpStatus = (pumpStatus == "NoConnection" || pumpStatus == null)
                    ? "No Connection" : pumpStatus;


                var prepayStatus = this.fcsService.CurrentFcsStatus.GetPrepayStatus(pumpId);
                log.Debug("HomeViewModel: PumpStatus: {0}, PrepayStatus: [{1}].", pumpStatus, prepayStatus.ToString());

                var cashierAuthorize = pumpConfig.CashierAuthorize == "Yes"
                                        || pumpConfig.CashierAuthorize == "True";

                var allowPostpay = pumpConfig.AllowPostpay == "Yes"
                                        || pumpConfig.AllowPostpay == "True";

                var prepayAmount = posManager.GetPrepayAmount(pumpId);
                log.Debug("HomeViewModel: PrepayAmount:{0}", prepayAmount);

                newPumpList.Add(new PumpDetails()
                {
                    Id = pumpId,
                    Status = pumpStatus,
                    PumpPrepayStatus = prepayStatus,
                    CashierAuthorize = cashierAuthorize,
                    AllowPostpay = allowPostpay,
                    PrepayAmount = prepayAmount,
                    Baskets = posManager.GetBasketsForPump(pumpId)
                });
            }

            UpdatePumpsForSwitchingAttempt(newPumpList);

            if (newPumpList.Count > 0)
            {
                Pumps = newPumpList;
                log.Debug("HomeViewModel: Pumps are initialized and total pumps:{0}.", Pumps.Count);
            }
            else
            {
                log.Error("HomeViewModel: NewPumpList is empty.");
            }
        }

        #region pump menu
        public IMvxCommand PrepayCmd => new MvxCommand<PumpDetails>(
        async (pump) =>
        {
            log.Info("HomeViewModel: Holding pump: {0}", pump.Id);
            var response = await fcsService.PrepayHoldAsync(pump.Id);
            if (response.ResultOk)
            {
                log.Info("HomeViewModel: Holding pump: {0} successful", pump.Id);
                posManager.StartNewSale(pump.Id);
                posManager.ActiveSale.SaleType = SaleType.Prepay;
                posManager.ActiveSale.IsPrepayHold = true;
                App.CurrentFeature = FeatureType.Prepay;
                log.Debug("HomeViewModel: Navigate to PrepayViewModel");
                await navigationService.Navigate<PrepayViewModel, int>(pump.Id);
            }
            else
            {
                log.Error("HomeViewModel: PrepayHold error occurred.");
                messageBoxService.ShowMessageBox("PrepayHoldError", MessageBoxButtonType.OK);
            }
        });

        public IMvxCommand SwitchPrepayCmd => new MvxCommand<PumpDetails>(
        async (pump) =>
        {
            SetSwitchingAttempt(pump.Id);
        });

        public IMvxCommand CancelPrepaySwitchCmd => new MvxCommand<PumpDetails>(
        async (pump) =>
        {
            ClearSwitchingAttempt();
        });

        public IMvxCommand ConfirmPrepaySwitchCmd => new MvxCommand<PumpDetails>(
        async (pump) =>
        {
            try
            {
                await SwitchPrepay(pump.Id);
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("HomeViewModel: Exception: {0}.", ex.Message), ex);
            }
        });

        public IMvxCommand DeletePrepayCmd => new MvxCommand<PumpDetails>(
        async (pump) =>
        {
            try
            {
                log.Debug("HomeViewModel: Delete Prepay - Trying to set prepay sale status for delete");
                if (posManager.SetPrepaySaleAsActive(pump.Id))
                {
                    log.Debug("HomeViewModel: Delete Prepay - Sale status found and set active. Trying to hold");
                    var holdResponse = await fcsService.PrepayHoldRemoveAsync(pump.Id);
                    if (holdResponse.ResultOk)
                    {
                        log.Debug("HomeViewModel: Delete Prepay - Hold successful. Going to tender selection");
                        posManager.PreparePrepayDelete(posManager.ActiveSale);
                        App.CurrentFeature = FeatureType.DeletePrepay;
                        await navigationService.Navigate<TenderSelectionViewModel, int>(pump.Id);
                    }
                    else
                    {
                        log.Debug("HomeViewModel: Delete Prepay - Hold not successful");
                        messageBoxService.ShowMessageBox("PrepayHoldRemoveError", MessageBoxButtonType.OK);
                    }
                }
                else
                {
                    log.Error("HomeViewModel: Sale status not found");
                }
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("HomeViewModel: Exception: {0}.", ex.Message), ex);
            }
        });

        public IMvxCommand AuthorizeCmd => new MvxCommand<PumpDetails>(async (pump) =>
        {
            try
            {
                log.Debug("HomeViewModel: Trying to authorize pump:{0}", pump.Id);
                var response = await fcsService.AuthorizePump(pump.Id);
                if (response.ResultOK)
                {
                    log.Debug("HomeViewModel: Authorized successfully.");
                }
                else
                {
                    log.Error("HomeViewModel: set pump authorization error occured");
                    messageBoxService.ShowMessageBox("CantAuthorizePostPay", MessageBoxButtonType.OK);
                }
            }
            catch (Exception ex)
            {
                log.DebugException(string.Format("HomeViewModel: Exception: {0}.", ex.Message), ex);
            }
        });

        public IMvxCommand StartCmd => new MvxCommand<PumpDetails>(
        async (pump) =>
        {
            log.Debug("HomeViewModel: Trying to start pump {0}", pump.Id);
            var response = await fcsService.StartPump(pump.Id);
            if (response.ResultOK)
            {
                log.Debug("HomeViewModel: Start pump {0}.", pump.Id);
            }
            else
            {
                log.Error("HomeViewModel: Start pump {0} error occured", pump.Id);
                messageBoxService.ShowMessageBox("CantStartPump", MessageBoxButtonType.OK);
            }
        });

        public IMvxCommand StopCmd => new MvxCommand<PumpDetails>(
        async (pump) =>
        {
            log.Debug("HomeViewModel: Trying to stop pump {0}", pump.Id);
            var response = await fcsService.StopPump(pump.Id);
            if (response.ResultOK)
            {
                log.Debug("HomeViewModel: Stop pump {0}.", pump.Id);
            }
            else
            {
                log.Error("HomeViewModel: Stop pump {0} error occured", pump.Id);
                messageBoxService.ShowMessageBox("CantStopPump", MessageBoxButtonType.OK);
            }
        });

        public IMvxCommand LogoutCmd => new MvxCommand(
        async () =>
        {
            log.Debug("HomeViewModel: User pressed logout button.Go to LogoutViewModel.");
            await navigationService.Navigate<LogoutViewModel>();
        });

        public IMvxCommand BasketCmd => new MvxCommand<BasketDetail>(async (basketDetail) =>
        {
            log.Debug("HomeViewModel: Started processing Basket Command with basketID: {0}.", basketDetail.BasketID);
            try
            {
                int pumpId = int.Parse(basketDetail.PumpID);
                posManager.SetActiveSale(pumpId, basketDetail.BasketID);

                if (basketDetail.Prepay != null)
                {
                    log.Debug("HomeViewModel: Processing refund for prepay basket");
                    App.CurrentFeature = FeatureType.PrepayBasketRefund;
                }
                else
                {
                    log.Debug("HomeViewModel: Processing postpasy basket.");
                    App.CurrentFeature = FeatureType.Postpay;
                    posManager.ActiveSale.Amount = basketDetail.Amount;
                }

                log.Debug("HomeViewModel: Holding Basket");
                var response = await fcsService.HoldBasket(basketDetail.BasketID);
                if (response.ResultOK)
                {
                    log.Debug("HomeViewModel: Hold basket successfully. Navigate to tender selection page.");
                    await this.navigationService.Navigate<TenderSelectionViewModel, int>(pumpId);
                }
                else
                {
                    log.Error("HomeViewModel: Hold basket failure");
                    messageBoxService.ShowMessageBox("BasketHoldError", MessageBoxButtonType.OK);
                }

            }
            catch (Exception ex)
            {
                log.DebugException(string.Format("HomeViewModel: Exception: {0}.", ex.Message), ex);
            }
        });
        #endregion

        #region Switching attempt
        private int CurrentSwitchSourcePumpId = 0;

        private void SetSwitchingAttempt(int pumpId)
        {
            log.Debug("HomeViewModel: Set switching attempt.CurrentSwitchSourcePumpId: {0}", pumpId);
            CurrentSwitchSourcePumpId = pumpId;
            UpdatePumpsForSwitchingAttempt(Pumps);
        }

        private void ClearSwitchingAttempt()
        {
            log.Debug("HomeViewModel: Clear switching attempt.");
            CurrentSwitchSourcePumpId = 0;
            UpdatePumpsForSwitchingAttempt(Pumps);
        }

        private void UpdatePumpsForSwitchingAttempt(MvxObservableCollection<PumpDetails> pumpList)
        {
            log.Debug("HomeViewModel: Updating pumps for switching attempt.");
            if (pumpList == null)
            {
                log.Debug("HomeViewModel: PumpList is empty.");
                return;
            }

            foreach (var pump in pumpList)
            {
                if (CurrentSwitchSourcePumpId > 0)
                {
                    log.Debug("HomeViewModel: Current switch source pumpId exists.Set switch status.");
                    pump.SetSwitchStatus(CurrentSwitchSourcePumpId);
                }
                else
                {
                    log.Debug("HomeViewModel: Current switch source pumpId doesn't exist.Clear switch status.");
                    pump.ClearSwitchStatus();
                }
            }
        }

        private async Task SwitchPrepay(int newPumpId)
        {
            log.Debug("HomeViewModel: Started switching prepay.New PumpId:{0}", newPumpId);
            if (CurrentSwitchSourcePumpId <= 0)
            {
                log.Error("HomeViewModel: Current switching source pumpId is 0.");
                return;
            }

            log.Debug("HomeViewModel: Prepay Switch - Sending prepay switch cmd to fcs");
            var response = await fcsService.PrepaySwitchAsync(newPumpId, CurrentSwitchSourcePumpId);
            if (response.ResultOk)
            {
                log.Debug("HomeViewModel: Prepay Switch - FCS prepay switch successful. Trying to move sale status for prepay.");
                var saleStatusMoved = posManager.SwitchPrepaySale(newPumpId, CurrentSwitchSourcePumpId);
                if (saleStatusMoved)
                {
                    log.Debug("HomeViewModel: Prepay Switch - Sale status moved.");
                }
                else
                {
                    log.Debug("HomeViewModel: Prepay Switch - Sale status not found.");
                }
                ClearSwitchingAttempt();
            }
            else
            {
                log.Debug("HomeViewModel: Prepay Switch - FCS prepay switch failed");
                messageBoxService.ShowMessageBox("CantSwitchPrepay", MessageBoxButtonType.OK);
            }

        }
        #endregion


        public IMvxCommand ShowReportCommand => new MvxCommand<string>(async (reportType) =>
        {
            log.Debug("HomeViewModel: Started Processing show report command.");
            try
            {
                log.Debug("HomeViewModel: Report type is: {0}. Navigate to Report view model.", reportType);
                var report = (ReportType)Enum.Parse(typeof(ReportType), reportType);
                await this.navigationService.Navigate<ReportViewModel, ReportType>(report);
            }
            catch (Exception ex)
            {
                log.DebugException(string.Format("HomeViewModel: Exception: {0}.", ex.Message), ex);
            }

        });

        public IMvxCommand ReprintReceiptCommand => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("HomeViewModel: Go to Reprint receipt page");
                await this.navigationService.Navigate<ReprintReceiptViewModel>();
            }
            catch (Exception ex)
            {
                log.DebugException(string.Format("HomeViewModel: Exception:{0}", ex.Message), ex);
            }

        });

        public IMvxCommand StartAllPumpsCommand => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("HomeViewModel: Want start all pumps. Go to ConfirmationViewModel for confirmation.");
                var response = await this.navigationService.Navigate<ConfirmationViewModel, string, bool>("WantToStartPumps");
                if (response == true)
                {
                    log.Debug("HomeViewModel: Response is true.Trying to start all pumps.");
                    var startPumpResponse = await this.fcsService?.StartPump(0);
                    if (startPumpResponse.ResultOK)
                    {
                        log.Debug("HomeViewModel: Started all pumps successfully.");
                    }
                    else
                    {
                        log.Error("HomeViewModel: Start all pumps failure.");
                        messageBoxService.ShowMessageBox("CantStartAllPump", MessageBoxButtonType.OK);
                    }
                }
                else
                {
                    log.Debug("HomeViewModel: User does't want to start all pumps");
                }
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("HomeViewModel: Exception:{0}", ex.Message), ex);
            }

        });

        public IMvxCommand StopAllPumpsCommand => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("HomeViewModel: Want stop all pumps. Go to ConfirmationViewModel for confirmation.");
                var response = await this.navigationService.Navigate<ConfirmationViewModel, string, bool>("WantToStopPumps");
                if (response == true)
                {
                    log.Debug("HomeViewModel: Response is true.Trying to stop all pumps.");
                    var stopPumpResponse = await this.fcsService?.StopPump(0);
                    if (stopPumpResponse.ResultOK)
                    {
                        log.Debug("HomeViewModel: Stopped all pumps successfully.");
                    }
                    else
                    {
                        log.Error("HomeViewModel: Stop all pumps failure.");
                        messageBoxService.ShowMessageBox("CantStopAllPump", MessageBoxButtonType.OK);
                    }
                }
                else
                {
                    log.Debug("HomeViewModel: User does't want to stop all pumps");
                }
            }
            catch (Exception ex)
            {
                log.DebugException(string.Format("HomeViewModel: Exception:{0}", ex.Message), ex);
            }

        });

        public IMvxCommand FuelPriceChangeCmd => new MvxCommand(async () =>
        {
            try
            {
                log.Debug("HomeViewModel: Go to fuel price change page");
                await this.navigationService.Navigate<GradesViewModel>();
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("HomeViewModel: Exception:{0}", ex.Message), ex);
            }

        });
    }
}
