using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;
using System;
using System.Timers;
using System.Windows.Threading;
using InfonetPOS.Core.ViewModels;

namespace InfonetPOS.WPF.Pages
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    [MvxContentPresentation]
    public partial class MainPage : MvxWpfView<MainViewModel>
    {
        public MainPage()
        {
            InitializeComponent();
            var date = DateTime.Now.ToString("MMM d, yyyy hh:mm tt");

            this.DateTimeText.Text = date;
            System.Timers.Timer DateTimeUpdateTimer = new System.Timers.Timer(1000)
            {
                AutoReset = true
            };
            DateTimeUpdateTimer.Elapsed += SetCurrentDateTime;
            DateTimeUpdateTimer.Enabled = true;
        }

        private void SetCurrentDateTime(object sender, ElapsedEventArgs e)
        {
            var date = DateTime.Now.ToString("MMM d, yyyy hh:mm tt");
            Dispatcher.BeginInvoke(new Action(() =>
            {
                this.DateTimeText.Text = date;
            }), DispatcherPriority.Background);


        }
    }
}
