using InfonetPos.FcsIntegration.Entities;
using InfonetPos.FcsIntegration.Entities.SiteConfiguration;
using InfonetPos.FcsIntegration.Enums;
using InfonetPos.FcsIntegration.Services;
using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPos.FcsIntegration.Utilities;
using InfonetPOS.Core.DB;
using InfonetPOS.Core.DB.Interface;
using InfonetPOS.Core.Enums;
using InfonetPOS.Core.Helpers;
using InfonetPOS.Core.Helpers.ReceiptGenerator;
using InfonetPOS.Core.Interfaces;
using InfonetPOS.Core.TPS.Services;
using InfonetPOS.Core.TPS.Services.Interfaces;
using InfonetPOS.Core.TPS.Utilities;
using InfonetPOS.Core.ViewModels;
using MvvmCross;
using MvvmCross.IoC;
using MvvmCross.Logging;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PropertyChanged;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace InfonetPOS.Core
{
    [AddINotifyPropertyChangedInterface]
    public class App : MvxApplication
    {
        public static CultureInfo Culture { get; private set; }
        public static bool ShowEnglishHint { get; set; }
        public static AppLanguage Language { get; private set; }
        public static POSType PosType { get; private set; }
        public static bool IsTpsConnected { get; private set; }
        public static bool IsFcsConnected { get; private set; }
        public static bool IsSignOnDone { get; private set; }
        public static FeatureType CurrentFeature { get; set; }
        public static event EventHandler<bool> HomePageVisibilityChange;
        public static event EventHandler<CultureInfo> CultureChange;


        private IFcsService fcsService;
        private IMvxNavigationService navigationService;
        private IMvxLog log;
        private IAppSettings appSettings;
        private System.Timers.Timer fcstimer;
        private PosManager posManager;

        public override void Initialize()
        {
            CreatableTypes()
                   .EndingWith("Service")
                   .AsInterfaces()
                   .RegisterAsLazySingleton();

            log = Mvx.IoCProvider.Resolve<IMvxLog>();
            appSettings = Mvx.IoCProvider.Resolve<IAppSettings>();
            TpsSerializer tpsSerializer = new TpsSerializer(log);
            FcsMessageSerializer fcsMessageSerializer = new FcsMessageSerializer(log);

            log.Debug("App: Initializing FcsService,TpsService and saleStatus as singleton..");
            Mvx.IoCProvider.LazyConstructAndRegisterSingleton<ITpsService>(() => new TpsService(log, appSettings.TpsIpAddress, appSettings.TpsPort, tpsSerializer));
            Mvx.IoCProvider.LazyConstructAndRegisterSingleton<IFcsService>(() => new FcsService(log, fcsMessageSerializer));
            Mvx.IoCProvider.LazyConstructAndRegisterSingleton<Printer>(() => new Printer(appSettings));
            Mvx.IoCProvider.LazyConstructAndRegisterSingleton<IDBAccess>(() => new DBAccess());
            Mvx.IoCProvider.ConstructAndRegisterSingleton(typeof(PosManager));
            Mvx.IoCProvider.ConstructAndRegisterSingleton(typeof(IPAddressManager));

            Mvx.IoCProvider.LazyConstructAndRegisterSingleton<IReceiptGenerator>(() => new ReceiptGenerator(
                    Mvx.IoCProvider.Resolve<PosManager>(),
                    AppLanguage.multi,
                    appSettings.FuelUnitPriceDecimal
                ));

            log.Debug("App: Load and configure all the neccessary settings..");
            LoadAppSettings();

            ConfigurePaymentServer();
            ConfigureFcsServer();
            RegisterAppStart<MainViewModel>();
        }

        private void LoadAppSettings()
        {
            log.Debug("App: Loading app settings...");

            var appLangauge = appSettings.DefaultLanguage;
            Enum.TryParse(appLangauge.ToLower(), out AppLanguage lang);
            log.Debug("App: Set current culture " + appLangauge);
            SetLanguage(lang);

            var posType = appSettings.PosType;
            log.Debug("App: Set current culture " + posType);
            SetPosType(posType);
        }

        public static void SetPosType(POSType posType)
        {
            var log = Mvx.IoCProvider.Resolve<IMvxLog>();
            log.Debug("App: Setting Pos type : {0}", posType.ToString());
            PosType = posType;
        }

        public static void SetLanguage(AppLanguage language)
        {
            var log = Mvx.IoCProvider.Resolve<IMvxLog>();
            log.Debug("App: Setting language.");
            Language = language;
            if (language == AppLanguage.multi)
            {
                ShowEnglishHint = true;
                log.Debug("App: language is multi.Set culture as arabic.");
                // Set arabic culture when english hint is on
                SetCulture(new CultureInfo(AppLanguage.ar.ToString()));
            }
            else
            {
                ShowEnglishHint = false;
                SetCulture(new CultureInfo(language.ToString()));
            }
        }

        public static void SetCulture(CultureInfo culture)
        {
            var log = Mvx.IoCProvider.Resolve<IMvxLog>();
            log.Debug("App: Setting culture.");

            Culture = culture;
            TranslationSource.Instance.CurrentCulture = culture;
            Resources.AppResources.Culture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureChange?.Invoke(null, culture);
        }

        public static void SetHomePageVisiblity(bool visible)
        {
            var log = Mvx.IoCProvider.Resolve<IMvxLog>();
            log.Debug("App: Setting Home page visibility.");

            HomePageVisibilityChange?.Invoke(null, visible);
        }


        private async void ConfigurePaymentServer()
        {
            log.Debug("App: Configuring Tps..");
            try
            {
                var tpsService = Mvx.IoCProvider.Resolve<ITpsService>();
                tpsService.TpsConnectionClosed += async () =>
                {
                    IsTpsConnected = false;
                    log.Info("App: Attempting to re-establish connection to TPS...");
                    await tpsService.ConnectAsync();
                };
                tpsService.TpsConnectionEstablished += () => IsTpsConnected = true;

                log.Info("App: Establishing Connection to TPS...");
                await tpsService.ConnectAsync();
            }
            catch (Exception ex)
            {
                log.ErrorException("App: ", ex);
            }
        }

        private void ConfigureFcsServer()
        {
            log.Debug("App: Configuring fcs...");
            fcsService = Mvx.IoCProvider.Resolve<IFcsService>();
            fcsService.ConnectionStatusChanged += OnFcsConnectionStatusChanged;
            fcsService.FcsReceivedConfigurationEvent += OnFCSConfigReceived;
            fcsService.FcsReceivedBasketEvent += OnBasketReceived;

            fcstimer = new System.Timers.Timer(2000)
            {
                AutoReset = false
            };

            fcstimer.Elapsed += OnCheckFcsConnection;

            log.Info("App: Attempting to establish connection to Fcs");
            fcsService.ConnectAsync(appSettings.FcsIpAddress, appSettings.FcsPort);
            if (!fcsService.IsConnected)
            {
                fcstimer.Enabled = true;
            }
        }

        private void OnFCSConfigReceived(object sender, ConfigurationRequest config)
        {
            log.Debug("App: fcs configuration is received.");
            posManager = Mvx.IoCProvider.Resolve<PosManager>();
            posManager.StartPos();
        }

        private void OnCheckFcsConnection(object sender, System.Timers.ElapsedEventArgs args)
        {
            if (fcsService.IsConnected == false)
            {
                log.Info("App: Attempting to re-establish connection to Fcs");
                fcsService.ConnectAsync(appSettings.FcsIpAddress, appSettings.FcsPort);
                fcstimer.Enabled = true;
            }
        }

        private async void OnFcsConnectionStatusChanged(object sender, bool isConnected)
        {
            log.Info("App: Fcs Connection status is changed...");
            //Todo: need to be aware of connection status changes
            var fcsService = sender as IFcsService;
            IsFcsConnected = isConnected;
            if (!IsFcsConnected)
            {
                IsSignOnDone = false;
                log.Debug("App: Fcs is disconnected. Navigate and stop pos.");

                navigationService = Mvx.IoCProvider.Resolve<IMvxNavigationService>();
                posManager = Mvx.IoCProvider.Resolve<PosManager>();

                if (posManager.IsUserLoggedIn())
                {
                    log.Debug("App: User logged in. Navigate to Home");
                    await navigationService.Navigate<HomeViewModel>();
                }
                else
                {
                    log.Debug("App: User not logged in. Navigate to Login");
                    await navigationService.Navigate<LoginViewModel>();
                }

                await posManager.StopPos();
                // Try re-connect
                fcstimer.Enabled = true;
            }
            else
            {
                log.Error("App: Fcs is connected now.");
            }
        }

        public static async Task SignOnAsync(string userName, int tillId, int shiftId)
        {
            var appSettings = Mvx.IoCProvider.Resolve<IAppSettings>();
            var log = Mvx.IoCProvider.Resolve<IMvxLog>();
            var fcsService = Mvx.IoCProvider.Resolve<IFcsService>();
            var posId = appSettings.PosId;
            var version = appSettings.Version;

            try
            {
                log.Debug("App: Signing on....");
                SignOnTPosResponse response = await fcsService.SignOnTPosAsync(posId, version, appSettings.PosType, userName, tillId, shiftId);
                if (response.ResultOk)
                {
                    IsSignOnDone = true;
                    log.Debug("App: Sign on done succesfully.");
                }
                else
                {
                    log.Error("App: Sign on failure.");
                }

            }
            catch (Exception ex)
            {
                IsSignOnDone = false;
                log.ErrorException(string.Format("App: SignOn exception: {0}.", ex.Message), ex);
            }

        }

        public static async Task SignOffAsync()
        {
            var log = Mvx.IoCProvider.Resolve<IMvxLog>();

            try
            {
                var appSettings = Mvx.IoCProvider.Resolve<IAppSettings>();
                var fcsService = Mvx.IoCProvider.Resolve<IFcsService>();
                var posId = appSettings.PosId;

                log.Debug("App: Signing off for posId:{0}.", posId);
                var response = await fcsService?.SignOffTPosAsync(posId);
                if (response.ResultOk)
                {
                    log.Debug("App: Sign off done successfully.");
                }
                else
                {
                    log.Error("App: Sign off failed.");
                }
    
            }
            catch (Exception ex)
            {
                log.ErrorException(string.Format("App: Exception:{0}.", ex.Message), ex);
            }
        }

        private async void OnBasketReceived(object sender, BasketRequest basketCommand)
        {
            log.Debug("App: Received basket with basketId:{0}", basketCommand.BasketDetail.BasketID);
            await posManager.OnBasketReceived(basketCommand);
        }
    }
}
