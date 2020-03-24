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
    [MvxContentPresentation]
    /// <summary>
    /// Interaction logic for ReprintReceiptDataPage.xaml
    /// </summary>
    public partial class ReprintReceiptDataPage : MvxWpfView<ReprintReceiptDataViewModel>
    {
        public ReprintReceiptDataPage()
        {
            InitializeComponent();
            var appSettings = Mvx.IoCProvider.Resolve<IAppSettings>();
            this.ReceiptText.FontFamily = new FontFamily(appSettings.ReportReceiptFont);
            this.ReceiptText.FontSize = appSettings.ReportReceiptFontSize;
        }
    }
}
