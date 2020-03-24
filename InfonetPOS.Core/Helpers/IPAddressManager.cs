using MvvmCross.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace InfonetPOS.Core.Helpers
{
    public class IPAddressManager
    {
        private readonly IMvxLog log;
        public IPAddressManager(IMvxLog log)
        {
            this.log = log;
        }

        public List<string> GetV4Ips()
        {
            try
            {
                log.Debug("IPAddressManager: Trying to fetch local ip addresses");
                return Dns.GetHostAddresses(Dns.GetHostName())
                    .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .ToList()
                    .Select(ip => ip.ToString())
                    .ToList();
            }
            catch(Exception ex)
            {
                log.ErrorException(string.Format("IPAddressManager: Exception: {0}.", ex.Message), ex);
                return null;
            }
        }
    }
}
