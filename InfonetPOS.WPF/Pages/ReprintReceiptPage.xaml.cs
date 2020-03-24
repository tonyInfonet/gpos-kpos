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
    [MvxContentPresentation]
    /// <summary>
    /// Interaction logic for ReprintReceiptPage.xaml
    /// </summary>
    public partial class ReprintReceiptPage : MvxWpfView<ReprintReceiptViewModel>
    {
        public ReprintReceiptPage()
        {
            InitializeComponent();
        }

        private void ReprintReceiptDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
        {
            if(eventArgs?.AddedItems!=null && eventArgs?.AddedItems.Count > 0)
            {
                this.ViewModel.ReprintSelectedReceipt(eventArgs.AddedItems[0]);
            }
        }
    }
}
