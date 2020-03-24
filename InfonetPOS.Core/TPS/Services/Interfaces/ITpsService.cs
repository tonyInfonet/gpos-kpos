using InfonetPOS.Core.TPS.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InfonetPOS.Core.TPS.Services.Interfaces
{
    public interface ITpsService
    {
        /// <summary>
        /// Raised when connection is established to TPS
        /// </summary>
        event Action TpsConnectionEstablished;

        /// <summary>
        /// Raised when connection is closed to TPS
        /// </summary>
        event Action TpsConnectionClosed;

        Task ConnectAsync();
        Task<TpsResponse> SaleRequestAsync(string paymentType, int pumpNo, string invoiceNo, double amount, CancellationToken token);
        Task<TpsResponse> RefundRequestAsync(string paymentType, int pumpNo, string invoiceNo, double amount, CancellationToken token);
        Task<TpsResponse> EODTerminalRequestAsync(string paymentType, int pumpNo, string unKnownIndex7, CancellationToken token);
    }
}
