using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPOS.Core.DB.Entities;
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
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace InfonetPOS.Core.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class LoginViewModel : MvxViewModel
    {
        private readonly IMvxNavigationService navigationService;
        private readonly IMvxLog log;
        private readonly IFcsService fcsService;
        private readonly IDBAccess dbAccess;
        private readonly PosManager posManager;
        private readonly IAppSettings appSettings;
        private readonly IPAddressManager iPAddressManager;
        private readonly IMessageBoxService messageBoxService;
        private readonly IApplicationService applicationService;

        #region UI Properties
        public bool CanLogin { get; set; }
        public List<int> SelectableShifts { get; private set; }
        public List<string> SelectableUsers { get; private set; }
        public List<Till> SelectableTills { get; private set; }
        private string selectedUser;
        public string SelectedUser
        {
            get => selectedUser;
            set
            {
                selectedUser = value;
                new Task(async () =>
                {
                    await PrepareShiftAndTillInputs(value);
                }).Start();
            }
        }
       
        public bool ShouldOpenNewTill { get; private set; }
        public int SelectedShift { get; set; }
        public string SelectedShiftStr
        {
            get
            {
                if (SelectedShift > 0)
                    return SelectedShift.ToString();
                else
                    return null;
            }
        }
        public Till SelectedTill { get; set; }
        public string Language => App.Language.ToString();
        private string ConvertToUnsecureString(SecureString securePassword)
        {
            if (securePassword == null)
            {
                return string.Empty;
            }

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        #endregion
        public IMvxCommand LoginCommand => new MvxCommand<object>(async param =>
        {

            if (string.IsNullOrEmpty(SelectedUser))
                return;
            string pwd = dbAccess.GetPassword(SelectedUser);
            var passwordContainer = param as IHavePassword;
            if (passwordContainer == null)
            {
                messageBoxService.ShowMessageBox("EnterPassword", MessageBoxButtonType.OK);
                return;
            }
            var secureString = passwordContainer.Password;
            if (secureString == null)
            {
                messageBoxService.ShowMessageBox("EnterPassword", MessageBoxButtonType.OK);
                return;
            }
            string PasswordInVM = ConvertToUnsecureString(secureString);
            if (string.IsNullOrEmpty(PasswordInVM))
            {
                messageBoxService.ShowMessageBox("EnterPassword", MessageBoxButtonType.OK);
                return;
            }

            if (pwd != PasswordInVM)
            {
                messageBoxService.ShowMessageBox("WrongPassword", MessageBoxButtonType.OK);
                return;
            }
            if (IsInputValid())
            {
                log.Debug("LoginViewModel: Trying to login.Update database and go to home view model");
                var result = Login();
                while (!result)
                {
                    log.Error("LoginViewModel: Login failed.");
                    log.Info("LoginViewModel: Show MessageBox to user to know if he/she wants to retry or not.");
                    var response = messageBoxService.ShowMessageBox("ConnectionLoseWhileLogin", MessageBoxButtonType.YesNo);

                    if (response == MessageBoxResultType.Yes)
                    {
                        log.Debug("LoginViewModel: User Wants to retry for login.");
                        result = Login();
                    }
                    else
                    {
                        log.Debug("LoginViewModel: User refuses to retry for login.Close the application.");
                        await applicationService.CloseApplication();
                    }
                }
                
                log.Debug("LoginViewModel:Login successfull.");

                
                await this.navigationService.Navigate<HomeViewModel>();
            }
            else
            {
                log.Debug("LoginViewModel: Incorrect login credentials.");
            }
        });
        


        public LoginViewModel(IMvxNavigationService navigationService,
                              IMvxLog log,
                              IFcsService fcsService,
                              IDBAccess dbAccess,
                              PosManager posManager,
                              IAppSettings appSettings,
                              IPAddressManager iPAddressManager,
                              IMessageBoxService messageBoxService,
                              IApplicationService applicationService)
        {
            this.navigationService = navigationService;
            this.log = log;
            this.fcsService = fcsService;
            this.dbAccess = dbAccess;
            this.posManager = posManager;
            this.appSettings = appSettings;
            this.iPAddressManager = iPAddressManager;
            this.messageBoxService = messageBoxService;
            this.applicationService = applicationService;
        }

        public override async Task Initialize()
        {
            log.Debug("LoginViewModel: Initialiizing ViewModel.");
            App.CultureChange += OnCultureChangeAsync;
            App.SetHomePageVisiblity(true);
            if (await IsIpListed())
            {
                log.Debug("LoginViewModel: IdAddress Listed.So,enable login.");
                await EnableLogin();
            }
            else
            {
                log.Error("LoginViewModel: IdAddress is not Listed.So,disable login.");
                DisableLogin();
            }

            await base.Initialize();
            log.Debug("LoginViewModel: Finished initializing ViewModel.");
        }

        public override void ViewAppearing()
        {
            log.Debug("LoginViewModel: View is appearing...");
            base.ViewAppearing();
            posManager.CanCloseApp = true;
        }

        public override void ViewDisappearing()
        {
            log.Debug("LoginViewModel: View is disappearing.");
            App.CultureChange -= OnCultureChangeAsync;
            App.SetHomePageVisiblity(false);
            base.ViewDisappearing();
        }

        private async void OnCultureChangeAsync(object sender, CultureInfo e)
        {
            await RaiseAllPropertiesChanged();
        }

        private async Task<bool> IsIpListed()
        {
            bool matchFound = false;
            log.Debug("LoginViewModel: Started fetching LocalIps and dbIPs.");
            var localIPs = iPAddressManager.GetV4Ips();
            var dbIPs = dbAccess.GetPosIpAddresses();

            while (dbIPs == null)
            {
                log.Error("LoginViewModel: DB connection lose while fetching PosIpAddresses.");
                log.Info("LoginViewModel: Show MessageBox to user to know if he/she wants to retry or not.");
                var response = messageBoxService.ShowMessageBox("ConnectionLoseWhileLogin", MessageBoxButtonType.YesNo);

                if (response == MessageBoxResultType.Yes)
                {
                    log.Debug("LoginViewModel: User wants to retry for dbIPs.");
                    dbIPs = dbAccess.GetPosIpAddresses();
                }
                else
                {
                    log.Debug("LoginViewModel: User refuses to retry for dbIps.Close the application.");
                    await applicationService.CloseApplication();
                }
            }

            if (dbIPs != null)
            {
                log.Debug("LogInViewModel: dbIPs is not null.");
                localIPs.ForEach(localIP =>
                {
                    dbIPs.ForEach(dbIP =>
                    {
                        if (dbIP == localIP)
                        {
                            matchFound = true;
                            log.Debug("LoginViewModel: Pos Ip address is matched.");
                        }
                    });
                });
            }

            return matchFound;
        }

        private void DisableLogin()
        {
            log.Debug("LoginViewModel: CanLogin property is set as false.");
            CanLogin = false;
        }

        private async Task EnableLogin()
        {
            log.Debug("LoginViewModel: CanLogin property is set as true.So,Fetch all shifts from DB.");
            CanLogin = true;
            this.SelectableShifts = dbAccess.GetAllShifts();
            while (this.SelectableShifts == null)
            {
                log.Error("LoginViewModel: Failed to get all shifts from DB.");
                log.Info("LoginViewModel: Show MessageBox to user to know if he/she wants to retry or not.");
                var response = messageBoxService.ShowMessageBox("ConnectionLoseWhileLogin", MessageBoxButtonType.YesNo);

                if (response == MessageBoxResultType.Yes)
                {
                    log.Debug("LoginViewModel: User wants to retry.So,try to fetch all shifts from DB again.");
                    this.SelectableShifts = dbAccess.GetAllShifts();
                }
                else
                {
                    log.Debug("LoginViewModel: User refuses to retry for fetching selectable shifts again.Close the application.");
                    await applicationService.CloseApplication();
                }
            }

            log.Debug("LoginViewModel: Started fetching all users from DB.");
            this.SelectableUsers = dbAccess.GetAllUsers();
            while (this.SelectableUsers == null)
            {
                log.Error("LoginViewModel: Failed to get all users from DB.");
                log.Info("LoginViewModel: Show MessageBox to user to know if he/she wants to retry or not.");
                var response = messageBoxService.ShowMessageBox("ConnectionLoseWhileLogin", MessageBoxButtonType.YesNo);

                if (response == MessageBoxResultType.Yes)
                {
                    log.Debug("LoginViewModel: User wants to retry.So,try to fetch all users from DB again.");
                    this.SelectableUsers = dbAccess.GetAllUsers();
                }
                else
                {
                    log.Debug("LoginViewModel: User refuses to retry for fetching selectable users again.Close the application.");
                    await applicationService.CloseApplication();
                }
            }
            log.Debug("LoginViewModel: Finished EnableLogin process.");
        }

        private bool IsInputValid()
        {
            log.Info("LoginViewModel: Check if UserInput is valid or not.");
            return SelectedTill != null
                && SelectedShift != 0
                && SelectedUser != null;
        }

        private async Task PrepareShiftAndTillInputs(string userName)
        {
            log.Debug("LoginViewModel: Preparing shift and till inputs.");
            log.Debug("LoginViewModel: Fetch active tills for username {0} from DB.", userName);
          
            var activeTills = dbAccess.GetActiveTills(userName);
            while (activeTills == null)
            {
                log.Error("LoginViewModel: Failed to get active tills from DB.");
                log.Info("LoginViewModel: Show MessageBox to user to know if he/she wants to retry or not.");
                var response = messageBoxService.ShowMessageBox("ConnectionLoseWhileLogin", MessageBoxButtonType.YesNo);

                if (response == MessageBoxResultType.Yes)
                {
                    log.Debug("LoginViewModel: User wants to retry.So,try to fetch active tills from DB again.");
                    activeTills = dbAccess.GetActiveTills(userName);
                }
                else
                {
                    log.Debug("LoginViewModel: User refuses to retry for fetching active tills again.Close the application.");
                    await applicationService.CloseApplication();
                }
            }

            log.Debug("LoginViewModel: Find the active till.");
            var activeTill = activeTills?.FirstOrDefault();
            if (activeTill != null)
            {
                log.Error("LoginViewModel: ActiveTill is null.");
                SelectedTill = activeTill;
                SelectedShift = activeTill.ShiftNo;
                ShouldOpenNewTill = false;
            }
            else
            {
                log.Info("LoginViewModel: ActiveTill is not null.Fetch available tills from DB.");
                SelectedTill = null;
                SelectableTills = dbAccess.GetAvailableTills();
                while (SelectableTills == null)
                {
                    log.Error("LoginViewModel: Failed to get available tills from DB.");
                    log.Info("LoginViewModel: Show MessageBox to user to know if he/she wants to retry or not.");
                    var response = messageBoxService.ShowMessageBox("ConnectionLoseWhileLogin", MessageBoxButtonType.YesNo);

                    if (response == MessageBoxResultType.Yes)
                    {
                        log.Debug("LoginViewModel: User wants to retry.So,try to fetch available tills from DB again.");
                        SelectableTills = dbAccess.GetAvailableTills();
                    }
                    else
                    {
                        log.Debug("LoginViewModel: User refuses to retry for fetching available tills again.Close the application.");
                        await applicationService.CloseApplication();
                    }
                }
                SelectedShift = SelectableShifts[0];
                ShouldOpenNewTill = true;
                log.Debug("LoginViewModel: Finished preparing shift and till inputs.User can open new till now.");
            }
        }

        private bool Login()
        {
            log.Debug("LoginViewModel: Trying to login pos.");
            bool result = false;
            if (ShouldOpenNewTill)
            {
                log.Debug("LoginViewModel: User can open new till for pos login.SelectedUser:{0},SelectedShift:{1},SelectedTil:{2}", SelectedUser, SelectedShift, SelectedTill.TillNo);
                result = posManager.PosLogin(SelectedUser, SelectedShift, SelectedTill.TillNo);
            }
            else
            {
                log.Debug("LoginViewModel: User can't open new till for pos login.SelectedTill:{0}", SelectedTill.TillNo);
                result = posManager.PosLogin(SelectedTill);
            }
 
            return result;
        }
    }
}
