using InfonetPOS.Core.Interfaces;
using InfonetPOS.WPF.Controls;
using MvvmCross;
using MvvmCross.Logging;
using MvvmCross.Platforms.Wpf.Core;
using MvvmCross.Platforms.Wpf.Presenters;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using TerminalPOS.WPF.ViewPresenters;

namespace InfonetPOS.WPF
{
    public class MvxSetup : MvxWpfSetup<Core.App>
    {
        protected override void InitializeFirstChance()
        {
            base.InitializeFirstChance();
            Mvx.IoCProvider.RegisterSingleton<IAppSettings>(new AppSettings());
            Mvx.IoCProvider.RegisterType<IMessageBoxService>(() => new MessageBoxService());
            Mvx.IoCProvider.RegisterType<IApplicationService>(() => new ApplicationService());
        }

        public override MvxLogProviderType GetDefaultLogProviderType() => MvxLogProviderType.Serilog;

        protected override IMvxLogProvider CreateLogProvider()
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();

            return base.CreateLogProvider();
        }

        protected override IMvxWpfViewPresenter CreateViewPresenter(ContentControl root)
        {
            return new TerminalPosMvxContentPresenter(root, "PosContent");
        }
    }
}
