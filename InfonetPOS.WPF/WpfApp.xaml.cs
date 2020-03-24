using MvvmCross.Core;
using MvvmCross.Platforms.Wpf.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace InfonetPOS.WPF
{
    /// <summary>
    /// Interaction logic for WpfApp.xaml
    /// </summary>
    public partial class WpfApp : MvxApplication
    {
        protected override void RegisterSetup()
        {
            this.RegisterSetupType<MvxSetup>();
        }
    }
}
