using InfonetPOS.Core.Interfaces;
using InfonetPOS.Core.ViewModels;
using MvvmCross;
using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InfonetPOS.WPF.Pages
{
    /// <summary>
    /// Interaction logic for ReportPage.xaml
    /// </summary>
    [MvxContentPresentation]
    public partial class ReportPage : MvxWpfView<ReportViewModel>
    {
        public ReportPage()
        {           
            InitializeComponent();
            var appSettings = Mvx.IoCProvider.Resolve<IAppSettings>();
            this.ReportText.FontFamily = new FontFamily(appSettings.ReportReceiptFont);
            this.ReportText.FontSize = appSettings.ReportReceiptFontSize;
        }
    }
}
