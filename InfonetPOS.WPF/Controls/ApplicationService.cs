using InfonetPos.FcsIntegration.Entities;
using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPOS.Core.Helpers;
using InfonetPOS.Core.Interfaces;
using MvvmCross;
using MvvmCross.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.WPF.Controls
{
    public class ApplicationService : IApplicationService
    {
        public async Task CloseApplication()
        {
            var log = Mvx.IoCProvider.Resolve<IMvxLog>();
            try
            {
                log.Debug("ClosableWindow: Signing off....");
                var posManager = Mvx.IoCProvider.Resolve<PosManager>();
                await posManager.StopPos();
            }
            catch (Exception ex)
            {
                log.DebugException("ClosableWindow: SignOff:{0}.", ex);
            }

            Environment.Exit(0);
        }
    }
}
