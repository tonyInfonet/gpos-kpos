﻿using InfonetPOS.Core.Interfaces;
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
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    [MvxContentPresentation]
    public partial class LoginPage : MvxWpfView<LoginViewModel>, IHavePassword

    {
        public LoginPage()
        {
            InitializeComponent();
            
        }

        private void UserSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            UserPassword.Password = "";
        }

        public System.Security.SecureString Password
        {
            get
            {
                return UserPassword.SecurePassword;
            }
        }
    }
}
