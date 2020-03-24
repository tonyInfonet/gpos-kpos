using InfonetPOS.Core.Enums;
using InfonetPOS.Core.Interfaces;
using MvvmCross.Commands;
using MvvmCross.Logging;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class MainViewModel : MvxViewModel
    {
        private readonly IMvxNavigationService navigationService;
        private readonly IMvxLog log;
        private readonly IAppSettings appSettings;

        private bool homePageVisible;

        public string CurrentCulture => App.Culture.DisplayName;
        public bool IsMulti => App.ShowEnglishHint;
        public bool CanChangeLanguage
        {
            get
            {
                return (!IsMulti && (appSettings.CanChangeLanguageAlways || homePageVisible));
            }
        }

        public IMvxCommand ArabicCommand => new MvxCommand(async () =>
        {
            log.Info("HomeViewModel: Customer Select Arabic Language");
            App.SetLanguage(AppLanguage.ar);
            await RaiseAllPropertiesChanged();
        });

        public IMvxCommand EnglishCommand => new MvxCommand(async () =>
        {
            log.Debug("HomeViewModel: Customer Select English language.");
            App.SetLanguage(AppLanguage.en);
            await RaiseAllPropertiesChanged();
        });

        public IMvxCommand MultiLanguageCommand => new MvxCommand(async () =>
        {
            App.SetLanguage(AppLanguage.multi);
            await RaiseAllPropertiesChanged();
        });

        public MainViewModel(IMvxNavigationService navigationService,
                             IMvxLog log,
                             IAppSettings appSettings)
        {
            this.navigationService = navigationService;
            this.log = log;
            this.appSettings = appSettings;
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();

            App.HomePageVisibilityChange += OnHomePageVisibilityChange;
            this.navigationService.Navigate<LoginViewModel>();
        }

        private async void OnHomePageVisibilityChange(object sender, bool homePageVisible)
        {
            this.homePageVisible = homePageVisible;
            await RaiseAllPropertiesChanged();
        }
    }
}
