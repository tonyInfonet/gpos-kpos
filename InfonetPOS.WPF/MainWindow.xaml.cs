using InfonetPOS.Core.Helpers;
using InfonetPOS.Core.Resources;
using MvvmCross;
using MvvmCross.Logging;
using MvvmCross.Platforms.Wpf.Views;
using System;
using System.Windows;

namespace InfonetPOS.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MvxWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            var log = Mvx.IoCProvider.Resolve<IMvxLog>();
            var posManager = Mvx.IoCProvider.Resolve<PosManager>();

            Application.Current.DispatcherUnhandledException += async (sender, exception) =>
            {
                log.ErrorException($"MainWindow.xmal.cs:Unhandled exception: {exception.Exception.Message}", exception.Exception);
                MessageBox.Show("An unhandled error occured.");
                await posManager.StopPos();
                exception.Handled = true;
                Application.Current.Shutdown();
            };

            this.Closed += (async (s, e) =>
            {
                try
                {
                    log.Debug("MainWindow.xaml.cs: Closing the application");

                    await posManager.StopPos();
                }
                catch (Exception ex)
                {
                    log.DebugException("MainWindow.xaml.cs: SignOff:{0}.", ex);
                }
            });

            this.Closing += ((s, e) =>
            {
                log.Debug("MainWindow.xaml.cs: User is trying to close the application.");
                if (posManager.CanCloseApp == false)
                {
                    log.Debug("MainWindow.xaml.cs: User is not allowed to close the application.");
                    // cancel the close operation.
                    string msg = AppResources.ResourceManager.GetString("CantCloseApp", AppResources.Culture);
                    MessageBox.Show(msg, "Infonet POS");

                    e.Cancel = true;
                }
            });
        }
    }
}
