using InfonetPOS.Core.ViewModels;
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
    /// Interaction logic for CashRefundPage.xaml
    /// </summary>
    [MvxContentPresentation]
    public partial class CashRefundPage : MvxWpfView<CashRefundViewModel>
    {
        public CashRefundPage()
        {
            InitializeComponent();
        }
    }
}
