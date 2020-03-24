using AsyncNet.Tcp.Client;
using AsyncNet.Tcp.Connection.Events;
using AsyncNet.Tcp.Remote;
using AsyncNet.Tcp.Remote.Events;
using InfonetPOS.Core.TPS.Entities;
using InfonetPOS.Core.TPS.Services.Interfaces;
using InfonetPOS.Core.TPS.Utilities;
using MvvmCross.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InfonetPOS.Core.TPS.Services
{
    public class TpsService : ITpsService
    {
        private readonly IMvxLog log;

        public string IpAddress { get; private set; }
        public int Port { get; private set; }
        public bool IsConnected => peer?.TcpClient.Connected ?? false;

        private AsyncNetTcpClient client;
        private IRemoteTcpPeer peer;
        private IAwaitaibleAsyncNetTcpClient awaitaibleClient;
        private IAwaitaibleRemoteTcpPeer awaitaiblePeer;
        private TpsSerializer tpsSerializer;

        public event Action TpsConnectionEstablished;
        public event Action TpsConnectionClosed;

        public TpsService(IMvxLog log, string ipAddress, int port, TpsSerializer tpsSerializer)
        {
            this.log = log;
            IpAddress = ipAddress;
            Port = port;
            this.tpsSerializer = tpsSerializer;

            log.Debug("TpsService: Tps IpAddress:{0}, port:{1}", ipAddress, port);
            client = new AsyncNetTcpClient(ipAddress, port);
            log.Debug("TpsService: Suscribing ConnectionEstablished, FrameArrived and Connection closed events.");
            client.ConnectionEstablished += ConnectionEstablished;
            client.FrameArrived += FrameArrived;
            client.ConnectionClosed += ConnectionClosed;

            log.Debug("TpsService: Creating new instance of awaitaible client");
            awaitaibleClient = new AwaitaibleAsyncNetTcpClient(client);
        }

        public async Task ConnectAsync()
        {
            var connected = false;
            do
            {
                try
                {
                    log.Debug("TpsService: Trying to re-connect until connection is established.");
                    awaitaiblePeer = await awaitaibleClient.ConnectAsync();
                    connected = true;
                }
                catch
                {
                    log.Debug("TpsService: Trying to connect but failed and wait for 2s.");
                    await Task.Delay(2000); // Todo: take retry time value from config or use Poly
                }
            } while (!connected);
        }

        public async Task<TpsResponse> SaleRequestAsync(string paymentType, int pumpNo, string invoiceNo, double amount, CancellationToken token)
        {
            log.Debug("TpsService: Started Sending sale request...");
            log.Debug("TpsService: paymentType{0},PumpNo:{1},invoiceNo:{2},amount:{3}.", paymentType, pumpNo, invoiceNo, amount);
            var tpsRequest = new TpsRequest()
            {
                TransactionType = "SaleInside",
                PaymentType = paymentType,
                LaneNo = pumpNo,
                InvoiceNo = invoiceNo,
                Amount = string.Format("{0:0.00}", amount)
            };

            log.Debug("TpsService: Started serializing data.");
            string requestCsv = tpsSerializer.SerializeTpsRequest(tpsRequest);
            log.Debug("TpsService: Request message to tps: {0}", requestCsv);
            log.Debug("TpsService: Waiting for Response.");
            var responseCsv = await SendAsync(requestCsv, token);
            if (responseCsv == "")
            {
                return null;
            }
            log.Debug("TpsService: Response message from tps: {0}", responseCsv);
            log.Debug("TpsService: Started Deserilizing data.");
            var tpsResponse = tpsSerializer.DeSerializeTpsResponse(responseCsv);
            log.Debug("TpsService: Return Tps response.");
            return tpsResponse;

        }

        public async Task<TpsResponse> RefundRequestAsync(string paymentType, int pumpNo, string invoiceNo, double amount, CancellationToken token)
        {
            log.Debug("TpsService: Started Sending Refund Request...");
            log.Debug("TpsService: paymentType{0},PumpNo:{1},invoiceNo:{2},amount:{3}.", paymentType, pumpNo, invoiceNo, amount);
            var tpsRequest = new TpsRequest()
            {
                TransactionType = "RefundInside",
                PaymentType = paymentType,
                LaneNo = pumpNo,
                InvoiceNo = invoiceNo,
                Amount = string.Format("{0:0.00}", amount)
            };

            log.Debug("TpsService: Started serializing data.");
            string requestCsv = tpsSerializer.SerializeTpsRequest(tpsRequest);
            log.Debug("TpsService: Waiting for Response.");
            var responseCsv = await SendAsync(requestCsv, token);
            if (responseCsv == "")
            {
                return null;
            }
            log.Debug("TpsService: Started Deserilizing data.");
            var tpsResponse = tpsSerializer.DeSerializeTpsResponse(responseCsv);
            log.Debug("TpsService: Return Tps response.");
            return tpsResponse;
        }

        public async Task<TpsResponse> EODTerminalRequestAsync(string paymentType, int pumpNo, string unKnownIndex7, CancellationToken token)
        {
            log.Debug("TpsService: Started Sending EODTerminalRequest...");
            log.Debug("TpsService: paymentType:{0},pumpNo:{1},UnknownIndex7:{2}", paymentType, pumpNo, unKnownIndex7);
            var tpsRequest = new TpsRequest()
            {
                TransactionType = "EODTerminal",
                PaymentType = paymentType,
                UnknownIndex7 = unKnownIndex7
            };

            log.Debug("TpsService: Started serializing data.");
            string requestCsv = tpsSerializer.SerializeTpsRequest(tpsRequest);
            log.Debug("TpsService: Waiting for Response.");
            var responseCsv = await SendAsync(requestCsv, token);
            if (responseCsv == "")
            {
                return null;
            }
            log.Debug("TpsService: Started Deserilizing data.");
            var tpsResponse = tpsSerializer.DeSerializeTpsResponse(responseCsv);
            log.Debug("TpsService: Return Tps response.");
            return tpsResponse;
        }

        private void ConnectionEstablished(object sender, ConnectionEstablishedEventArgs e)
        {
            log.Info("TpsService: Tps Connection is established.");
            peer = e.RemoteTcpPeer;
            log.Info($"TpsService: Connected to [{peer.IPEndPoint}]");
            TpsConnectionEstablished?.Invoke();
        }

        private void FrameArrived(object sender, TcpFrameArrivedEventArgs e)
        {
            log.Info($"Client received: " + $"{System.Text.Encoding.UTF8.GetString(e.FrameData)}");
        }

        private void ConnectionClosed(object sender, ConnectionClosedEventArgs e)
        {
            log.Info("TpsService: Connection is Closed.");
            TpsConnectionClosed?.Invoke();
            peer?.Dispose();
            peer = null;
            awaitaiblePeer?.Dispose();
            awaitaiblePeer = null;
        }

        private async Task<string> SendAsync(string data, CancellationToken token)
        {
            string result = "";
            bool pendingData = true;
            try
            {
                if (awaitaiblePeer?.RemoteTcpPeer != null)
                {
                    log.Debug("TpsService: RemoteTcpPeer is not null.Encode data as bytes.");
                    var bytes = Encoding.UTF8.GetBytes(data);
                    log.Debug("TpsService: Send the bytes to remoteTcpPeer.");
                    await awaitaiblePeer.RemoteTcpPeer.SendAsync(bytes, token);
                    log.Debug("TpsService: Get the response as frame by frame.");
  
                    do
                    {
                        var response = await awaitaiblePeer.ReadFrameAsync(token).ConfigureAwait(false);
                        log.Debug("TpsService: Encode the response as string.");
                        result += Encoding.UTF8.GetString(response);
                        var trimmedResult = result.TrimEnd('\r', '\n');
                        if (trimmedResult.EndsWith("END-DATA"))
                        {
                            pendingData = false;
                            result = trimmedResult;
                        }
                    } while (pendingData);

                }
                else
                {
                    throw new Exception("TpsService: Problem while sending data: peer is null");
                }
            }
            catch (Exception ex)
            {
                log.DebugException("TpsService: Exception: ", ex);
                return "";
            }

            log.Debug("TpsService: Encoded result: {0}.", result);
            return result;
        }
    }
}
