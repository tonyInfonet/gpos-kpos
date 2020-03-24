using AsyncNet.Tcp.Client;
using AsyncNet.Tcp.Connection.Events;
using AsyncNet.Tcp.Remote;
using AsyncNet.Tcp.Remote.Events;
using MvvmCross.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using InfonetPos.FcsIntegration.Entities;
using InfonetPos.FcsIntegration.Entities.SiteConfiguration;
using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPos.FcsIntegration.Utilities;
using InfonetPos.FcsIntegration.Entities.SetPump;
using InfonetPos.FcsIntegration.Enums;
using InfonetPos.FcsIntegration.Entities.Report;
using InfonetPos.FcsIntegration.Entities.Receipt;
using InfonetPos.FcsIntegration.Entities.SetPrice;
using System.Collections.Generic;

namespace InfonetPos.FcsIntegration.Services
{
    public class FcsService : IFcsService
    {
        private IAsyncNetTcpClient fcsClient;
        private IRemoteTcpPeer fcsTcpPeer;
        private AutoResetEvent signOnWaitEvent;
        private AutoResetEvent signOnTPosWaitEvent;
        private AutoResetEvent signOffTPosWaitEvent;
        private AutoResetEvent prePayWaitEvent;
        private AutoResetEvent basketRemovalWaitEvent;
        private AutoResetEvent setPumpWaitEvent;
        private AutoResetEvent setPriceWaitEvent;
        private AutoResetEvent getReportWaitEvent;
        private AutoResetEvent getReceiptWaitEvent;
        private AutoResetEvent getReceiptDataWaitEvent;

        private FcsResponse<SignOnResponse> receivedSignOnResponse;
        private FcsResponse<SignOnTPosResponse> receivedSignOnTPosResponse;
        private FcsResponse<PrepayResponse> receivedPrepayResponse;
        private FcsResponse<BasketResponse> receivedBasketResponse;
        private FcsResponse<SignOffTPosResponse> receivedSignOffTPosResponse;
        private FcsResponse<SetPumpResponse> receivedSetPumpResponse;
        private FcsResponse<SetPriceResponse> receivedSetPriceResponse;
        private FcsResponse<GetReportResponse> receivedGetReportResponse;
        private FcsResponse<GetReceiptResponse> receivedGetReceiptResponse;
        private FcsResponse<GetReceiptDataResponse> receivedGetReceiptDataResponse;
        private readonly IMvxLog log;
        private FcsMessageSerializer fcsMessageSerializer;
        private CancellationTokenSource fcsServiceCancellationTokenSource;
        private Task fcsClientTask;
        private byte[] unprocessedData;

        public bool IsConnected
        {
            get
            {
                bool? isConnected = fcsTcpPeer?.TcpClient.Connected;
                return isConnected.HasValue ? isConnected.Value : false;
            }
        }

        public FcsStatus CurrentFcsStatus { get; set; }
        public ConfigurationRequest FCSConfig { get; set; }

        public event EventHandler<bool> ConnectionStatusChanged;
        public event EventHandler<FcsStatus> FcsStatusChanged;
        public event EventHandler<PriceChangedEvent> FuelPriceChanged;
        public event EventHandler<PrepayEvent> FcsPrepayEvent;
        public event EventHandler<BasketRequest> FcsReceivedBasketEvent; // Event Handler that subscribes the receiceved basket event from fcs.
        public event EventHandler<ConfigurationRequest> FcsReceivedConfigurationEvent; //Event handler that subscribes the received configuration event from fcs.

        public FcsService(IMvxLog log, FcsMessageSerializer fcsMessageSerializer)
        {
            signOnWaitEvent = new AutoResetEvent(false);
            signOnTPosWaitEvent = new AutoResetEvent(false);
            prePayWaitEvent = new AutoResetEvent(false);
            basketRemovalWaitEvent = new AutoResetEvent(false);
            signOffTPosWaitEvent = new AutoResetEvent(false);
            setPumpWaitEvent = new AutoResetEvent(false);
            setPriceWaitEvent = new AutoResetEvent(false);
            getReportWaitEvent = new AutoResetEvent(false);
            getReceiptWaitEvent = new AutoResetEvent(false);
            getReceiptDataWaitEvent = new AutoResetEvent(false);

            this.log = log;
            this.fcsMessageSerializer = fcsMessageSerializer;
        }



        public Task ConnectAsync(string ipAddress, int port)
        {
            ResetFcsClient();
            log.Info("FcsService: Fcs Connect Async.");
            log.Debug("FcsService: Establishing connection at ipAddress:{0} and port{1}.", ipAddress, port);
            fcsClient = new AsyncNetTcpClient(ipAddress, port);

            log.Debug("FcsService: Subscribing Fcs ConnectionEstablished,ConnectionClosed and FrameArrived events.");
            fcsClient.ConnectionEstablished += FcsClient_ConnectionEstablished;
            fcsClient.ConnectionClosed += FcsClient_ConnectionClosed;
            fcsClient.FrameArrived += FcsClient_FrameArrived;

            log.Debug("FcsService: Creating a new instance of CancellationTokenSource.");
            fcsServiceCancellationTokenSource = new CancellationTokenSource();
            fcsClientTask = fcsClient.StartAsync(fcsServiceCancellationTokenSource.Token);
            log.Debug("FcsService: Return fcsClientTask.");
            return fcsClientTask;
        }

        private void ResetFcsClient()
        {
            log.Info("FcsService: Reset Fcs client.");
            if (fcsClient != null)
            {
                log.Debug("FcsService: Unsubsribing fcsClient ConnectionEstablished,ConnectionClosed,FrameArrived events.");
                fcsClient.ConnectionEstablished -= FcsClient_ConnectionEstablished;
                fcsClient.ConnectionClosed -= FcsClient_ConnectionClosed;
                fcsClient.FrameArrived -= FcsClient_FrameArrived;
            }

            if (fcsServiceCancellationTokenSource != null)
            {
                log.Debug("FcsService: If Fcs service cancellation token source is not null, then cancel it.");
                fcsServiceCancellationTokenSource.Cancel();

                try
                {
                    log.Debug("FcsService: Waiting for fcs client task to set.");
                    fcsClientTask.Wait();
                }
                catch (Exception ex)
                {
                    log.DebugException("FcsService: exception: {0}", ex);
                }
                finally
                {
                    log.Debug("FcsService: Dispose the fcs service cancellation token source.");
                    fcsServiceCancellationTokenSource.Dispose();
                }
            }
            fcsTcpPeer = null;
        }

        public async Task<SignOnResponse> SignOnAsync(string posId, string version)
        {
            log.Info("FcsService: Signing on...");
            log.Debug("FcsService: Signing on posid:{0}, version:{1}.", posId, version);
            if (IsConnected)
            {
                log.Debug("FcsService: If Fcs is connected create a new instance of signOn request.");
                SignOnRequest signOnRequest = new SignOnRequest() { PosId = posId, Version = version };
                log.Debug("FcsService: Create a fcs command for sign on.");
                FcsCommand<SignOnRequest> signOnCommand = new FcsCommand<SignOnRequest>() { Command = signOnRequest };
                FcsMessageSerializer serializer = fcsMessageSerializer;
                log.Debug("FcsService: Serialize signOn command with fcsMessage Serializer and send it to fcsTcpPeer.");
                byte[] serializedBytes = serializer.Serialize<FcsCommand<SignOnRequest>>(signOnCommand);
                await fcsTcpPeer.SendAsync(serializedBytes).ConfigureAwait(false);

                log.Debug("FcsService: Waiting for sign on wait event to set.");
                signOnWaitEvent.WaitOne();

                var signOnResponse = this.receivedSignOnResponse;
                this.receivedSignOnResponse = null;
                log.Debug("FcsService: return sign on response command.");
                return signOnResponse.Command;
            }
            else
            {
                log.Debug("FcsService: Fcs is not connected.");
                return new SignOnResponse()
                {
                    PosId = posId,
                    Version = version,
                    Result = "Err"
                };
            }
        }

        public async Task<SignOnTPosResponse> SignOnTPosAsync(string posId,
                                                             string version,
                                                             POSType posType = POSType.TPOS,
                                                             string userId = "",
                                                             int tillId = 0,
                                                             int shift = 0)
        {
            log.Info("FcsService: Signing on TPos...");
            log.Debug("FcsService: Signing on Tpos with posid:{0}, version:{1}.", posId, version);
            if (IsConnected)
            {
                log.Debug("FcsService: If Fcs is connected, create a new instance of signOn request.");
                SignOnTPosRequest signOnRequest = new SignOnTPosRequest()
                {
                    PosId = posId,
                    Version = version,
                    POSType = posType.ToString(),
                    UserID = userId,
                    TillID = tillId,
                    Shift = shift
                };
                log.Debug("FcsService: Create a fcs command for signon Tpos.");
                FcsCommand<SignOnTPosRequest> signOnCommand = new FcsCommand<SignOnTPosRequest>() { Command = signOnRequest };
                FcsMessageSerializer serializer = fcsMessageSerializer;
                log.Debug("FcsService: Serialize signOn command with fcsMessage Serializer and send it to fcsTcpPeer.");
                byte[] serializedBytes = serializer.Serialize<FcsCommand<SignOnTPosRequest>>(signOnCommand);
                await fcsTcpPeer.SendAsync(serializedBytes).ConfigureAwait(false);

                log.Debug("FcsService: Waiting for sign on Tpos wait event to set.");
                signOnTPosWaitEvent.WaitOne();

                var signOnResponse = this.receivedSignOnTPosResponse;
                this.receivedSignOnTPosResponse = null;
                log.Debug("FcsService: return sign on Tpos response command.");
                return signOnResponse.Command;
            }
            else
            {
                log.Debug("FcsService: Fcs is not connected.");
                return new SignOnTPosResponse()
                {
                    PosId = posId,
                    Version = version,
                    Result = "Err"
                };
            }
        }

        public async Task<SignOffTPosResponse> SignOffTPosAsync(string posId)
        {
            log.Info("FcsService: Signing off TPos...");
            log.Debug("FcsService: Signing off Tpos with posid:{0}, version:{1}.", posId);
            if (IsConnected)
            {
                log.Debug("FcsService: If Fcs is connected, create a new instance of signOff request.");
                SignOffTPosRequest signOffTPosRequest = new SignOffTPosRequest() { PosId = posId };
                log.Debug("FcsService: Create a fcs command for signoff Tpos.");
                FcsCommand<SignOffTPosRequest> signOffTPosCommand = new FcsCommand<SignOffTPosRequest>() { Command = signOffTPosRequest };
                FcsMessageSerializer serializer = fcsMessageSerializer;
                log.Debug("FcsService: Serialize signOff command with fcsMessage Serializer and send it to fcsTcpPeer.");
                byte[] serializedBytes = serializer.Serialize<FcsCommand<SignOffTPosRequest>>(signOffTPosCommand);
                await fcsTcpPeer.SendAsync(serializedBytes).ConfigureAwait(false);

                log.Debug("FcsService: Waiting for sign off Tpos wait event to set.");
                signOffTPosWaitEvent.WaitOne();

                var signOffTPosResponse = this.receivedSignOffTPosResponse;
                this.receivedSignOnTPosResponse = null;
                log.Debug("FcsService: return sign off Tpos response command.");
                return signOffTPosResponse.Command;
            }
            else
            {
                log.Debug("FcsService: Fcs is not connected.");
                return new SignOffTPosResponse()
                {
                    PosId = posId,
                    Result = "Err"
                };
            }
        }

        public Task<SetPumpResponse> StartPump(int pumpID)
        {
            log.Debug("FcsService: Start pump {0}.", pumpID);
            return SendSetPumpRequest(SetPumpRequestType.Start, pumpID);
        }

        public Task<SetPumpResponse> StopPump(int pumpID)
        {
            log.Debug("FcsService: Stop pump {0}.", pumpID);
            return SendSetPumpRequest(SetPumpRequestType.Stop, pumpID);
        }

        public Task<SetPumpResponse> SetPumpOnNow(int pumpID)
        {
            log.Debug("FcsService: Set pump on now {0}.", pumpID);
            return SendSetPumpRequest(SetPumpRequestType.OnNow, pumpID);
        }

        public Task<SetPumpResponse> AuthorizePump(int pumpID, string grade = "All", string payType = "Credit")
        {
            log.Debug("FcsService: Authorize pump {0}.", pumpID);
            return SendSetPumpRequest(SetPumpRequestType.Authorize, pumpID, grade, payType);
        }

        public async Task<GetReportResponse> GetReport(ReportType reportType, GetReportCriteria criteria)
        {
            log.Info("FcsService: Requesting report to FCS...");
            log.Debug("FcsService: ReportType:{0}.", reportType);
            log.Debug("FcsService: Criteria:{0}.", criteria);
            if (IsConnected)
            {
                log.Debug("FcsService: If Fcs is connected, create a new instance of GetReport request.");
                GetReportRequest getReportRequest = new GetReportRequest()
                {
                    Type = reportType,
                    Criteria = criteria
                };
                log.Debug("FcsService: Create a fcs command for GetReport.");
                FcsCommand<GetReportRequest> getReportCommand = new FcsCommand<GetReportRequest>() { Command = getReportRequest };
                FcsMessageSerializer serializer = fcsMessageSerializer;
                log.Debug("FcsService: Serialize GetReport command with fcsMessage Serializer and send it to fcsTcpPeer.");
                byte[] serializedBytes = serializer.Serialize<FcsCommand<GetReportRequest>>(getReportCommand);
                await fcsTcpPeer.SendAsync(serializedBytes).ConfigureAwait(false);

                log.Debug("FcsService: Waiting for get report wait event to set.");
                getReportWaitEvent.WaitOne();

                var getReportResponse = this.receivedGetReportResponse;
                this.receivedGetReportResponse = null;
                log.Debug("FcsService: return GetReport response command.");
                return getReportResponse.Command;
            }
            else
            {
                log.Debug("FcsService: Fcs is not connected.");
                return new GetReportResponse()
                {
                    Result = "Err"
                };
            }
        }

        //Todo: Could not test it during the implementation please test and remove the comment
        public async Task<GetReceiptResponse> GetReceipt(ReceiptType receiptType, GetReceiptCriteria criteria)
        {
            log.Info("FcsService: Requesting receipt to FCS...");
            log.Debug("FcsService: ReceiptType:{0}.", receiptType);
            log.Debug("FcsService: Criteria:{0}.", criteria);
            if (IsConnected)
            {
                log.Debug("FcsService: If Fcs is connected, create a new instance of GetReceipt request.");
                GetReceiptRequest getReceiptRequest = new GetReceiptRequest()
                {
                    Type = receiptType,
                    Criteria = criteria
                };
                log.Debug("FcsService: Create a fcs command for GetReceipt.");
                FcsCommand<GetReceiptRequest> getReceiptCommand = new FcsCommand<GetReceiptRequest>() { Command = getReceiptRequest };
                FcsMessageSerializer serializer = fcsMessageSerializer;
                log.Debug("FcsService: Serialize GetReport command with fcsMessage Serializer and send it to fcsTcpPeer.");
                byte[] serializedBytes = serializer.Serialize<FcsCommand<GetReceiptRequest>>(getReceiptCommand);
                await fcsTcpPeer.SendAsync(serializedBytes).ConfigureAwait(false);

                log.Debug("FcsService: Waiting for get receipt wait event to set.");
                getReceiptWaitEvent.WaitOne();

                var getReceiptResponse = this.receivedGetReceiptResponse;
                this.receivedGetReceiptResponse = null;
                log.Debug("FcsService: return GetReceipt response command.");
                return getReceiptResponse.Command;
            }
            else
            {
                log.Debug("FcsService: Fcs is not connected.");
                return new GetReceiptResponse()
                {
                    Result = "Err"
                };
            }
        }

        //Todo: Could not test it during the implementation please test and remove the comment
        public async Task<GetReceiptDataResponse> GetReceiptData(ReceiptType receiptType, string invoiceNumber)
        {
            log.Info("FcsService: Requesting receipt data to FCS...");
            log.Debug("FcsService: ReceiptType:{0}, InvoiceNumber:{1}.", receiptType, invoiceNumber);
            if (IsConnected)
            {
                log.Debug("FcsService: If Fcs is connected, create a new instance of GetReceiptData request.");
                GetReceiptDataRequest getReceiptDataRequest = new GetReceiptDataRequest()
                {
                    Type = receiptType,
                    Criteria = new GetReceiptDataCriteria()
                    {
                        InvoiceNumber = invoiceNumber
                    }
                };
                log.Debug("FcsService: Create a fcs command for GetReceiptData.");
                FcsCommand<GetReceiptDataRequest> getReceiptDataCommand = new FcsCommand<GetReceiptDataRequest>() { Command = getReceiptDataRequest };
                FcsMessageSerializer serializer = fcsMessageSerializer;
                log.Debug("FcsService: Serialize GetReport command with fcsMessage Serializer and send it to fcsTcpPeer.");
                byte[] serializedBytes = serializer.Serialize<FcsCommand<GetReceiptDataRequest>>(getReceiptDataCommand);
                await fcsTcpPeer.SendAsync(serializedBytes).ConfigureAwait(false);

                log.Debug("FcsService: Waiting for get receipt wait event to set.");
                getReceiptDataWaitEvent.WaitOne();

                var getReceiptDataResponse = this.receivedGetReceiptDataResponse;
                this.receivedGetReceiptDataResponse = null;
                log.Debug("FcsService: return GetReceipt response command.");
                return getReceiptDataResponse.Command;
            }
            else
            {
                log.Debug("FcsService: Fcs is not connected.");
                return new GetReceiptDataResponse()
                {
                    Result = "Err"
                };
            }
        }

        public async Task<SetPriceResponse> SetPrice(List<PriceChange> changes)
        {
            log.Debug("FcsService: Sending Set Price Request...");

            if (IsConnected)
            {
                log.Debug("FcsService: Fcs is connected.Creating a new instance of SetPriceRequest.");
                SetPriceRequest setPriceRequest = new SetPriceRequest()
                {
                    PriceChanges = changes
                };

                log.Debug("FcsService: Create a fcs command for set price request.");
                FcsCommand<SetPriceRequest> setPriceCommand = new FcsCommand<SetPriceRequest>()
                {
                    Command = setPriceRequest
                };

                log.Debug("FcsService: Serialize set price request command with fcsMessage Serializer and send it to fcsTcpPeer.");
                byte[] serializedBytes = fcsMessageSerializer.Serialize<FcsCommand<SetPriceRequest>>(setPriceCommand);
                await fcsTcpPeer.SendAsync(serializedBytes).ConfigureAwait(false);
                setPriceWaitEvent.WaitOne();
                log.Debug("FcsService: Waiting for setPriceWaitEvent to set.");

                var setPriceResponse = this.receivedSetPriceResponse;
                this.receivedSetPriceResponse = null;
                return setPriceResponse.Command;
            }
            else
            {
                log.Debug("FcsService: Fcs is not connected. Can't set price");
                return new SetPriceResponse() { Result = "Err" };
            }
        }

        private async Task<SetPumpResponse> SendSetPumpRequest(SetPumpRequestType requestType,
                                                         int pumpID,
                                                         string grade = "All",
                                                         string payType = "Credit")
        {
            log.Debug("FcsService: Sending Set Pump Request...");
            log.Debug("FcsService: RequestType:{0} , PumpId{1}.", requestType.ToString(), pumpID);

            if (IsConnected)
            {
                log.Debug("FcsService: Fcs is connected.Creating a new instance of SetPumpRequest.");
                SetPumpRequest setPumpRequest = new SetPumpRequest()
                {
                    Type = requestType.ToString(),
                    PumpID = pumpID
                };
                log.Debug("FcsService: If request type is authorize,then set grade and paytype");
                if (requestType == SetPumpRequestType.Authorize)
                {
                    setPumpRequest.Grade = grade;
                    setPumpRequest.PayType = payType;
                }

                log.Debug("FcsService: Create a fcs command for set pump request.");
                FcsCommand<SetPumpRequest> setPumpCommand = new FcsCommand<SetPumpRequest>()
                {
                    Command = setPumpRequest
                };

                log.Debug("FcsService: Serialize set pump request command with fcsMessage Serializer and send it to fcsTcpPeer.");
                byte[] serializedBytes = fcsMessageSerializer.Serialize<FcsCommand<SetPumpRequest>>(setPumpCommand);
                await fcsTcpPeer.SendAsync(serializedBytes).ConfigureAwait(false);
                setPumpWaitEvent.WaitOne();
                log.Debug("FcsService: Waiting for setPumpWaitEvent to set.");

                var setPumpResponse = this.receivedSetPumpResponse;
                this.receivedSetPumpResponse = null;
                return setPumpResponse.Command;
            }
            else
            {
                log.Debug("FcsService: Fcs is not connected.Can't authorize pump");
                return new SetPumpResponse()
                {
                    Type = requestType.ToString(),
                    PumpID = pumpID,
                    Result = "Err"
                };
            }
        }

        public Task<PrepayResponse> PrepaySwitchAsync(int pumpID, int oldPumpId)
        {
            log.Debug("FcsService: Prepay hold with pumpId:{0}.", pumpID);
            return SendPrepayRequest(PrepayRequestType.Switch, pumpID, 0, "", "", oldPumpId);
        }

        public Task<PrepayResponse> PrepayHoldAsync(int pumpID)
        {
            log.Debug("FcsService: Prepay hold with pumpId:{0}.", pumpID);
            return SendPrepayRequest(PrepayRequestType.Hold, pumpID);
        }

        public Task<PrepayResponse> PrepaySetAsync(int pumpID,
                                                   double amount,
                                                   string invoiceID,
                                                   string posID,
                                                   double totalPaid = 0.0,
                                                   double change = 0.0,
                                                   string receipt = "")
        {
            log.Debug("FcsService: Prepay set with pumpID:{0}, amount:{1},invoiceID:{2},posID:{3}.", pumpID, amount, invoiceID, posID);
            return SendPrepayRequest(PrepayRequestType.Set, pumpID, amount, invoiceID, posID, 0, totalPaid, change, receipt);
        }

        public Task<PrepayResponse> PrepayCancelHoldAsync(int pumpID)
        {
            log.Debug("FcsService: Prepay Cancel hold with pumpID:{0}.", pumpID);
            return SendPrepayRequest(PrepayRequestType.Cancel, pumpID);
        }

        public async Task<PrepayResponse> PrepayCancelHoldRemoveAsync(int pumpID)
        {
            log.Debug("FcsService: Prepay cancel hold remove with pumpID:{0}.", pumpID);

            var cancelHoldRemovePrepayResponse = await SendPrepayRequest(PrepayRequestType.CancelHoldRemove, pumpID);

            if (cancelHoldRemovePrepayResponse.ResultOk)
            {
                log.Debug("FcsService: Cancel hold remove response is ok.");
            }
            else
            {
                log.Debug("FcsService: Cancel hold remove response is not ok.");
            }

            log.Debug("FcsService: return cancelHoldRemovePrepayResponse.");
            return cancelHoldRemovePrepayResponse;
        }

        public async Task<PrepayResponse> PrepayHoldRemoveAsync(int pumpID)
        {
            log.Debug("FcsService: Prepay hold remove with pumpID:{0}.", pumpID);

            var holdRemovePrepayResponse = await SendPrepayRequest(PrepayRequestType.HoldRemove, pumpID);

            if (holdRemovePrepayResponse.ResultOk)
            {
                log.Debug("FcsService: Hold remove response is ok.");
            }
            else
            {
                log.Debug("FcsService: Hold remove response is not ok.");
            }

            log.Debug("FcsService: return removePrepayResponse.");
            return holdRemovePrepayResponse;
        }

        public async Task<PrepayResponse> PrepayRemoveAsync(int pumpID)
        {
            log.Debug("FcsService: Prepay remove with pumpID:{0}.", pumpID);

            var removePrepayResponse = await SendPrepayRequest(PrepayRequestType.Remove, pumpID);

            log.Debug("FcsService: return removePrepayResponse.");
            return removePrepayResponse;
        }

        private async Task<PrepayResponse> SendPrepayRequest(PrepayRequestType requestType,
                                                             int pumpID,
                                                             double amount = 0.0,
                                                             string invoiceID = "",
                                                             string posID = "",
                                                             int oldPumpId = 0,
                                                             double totalPaid = 0.0,
                                                             double change = 0.0,
                                                             string receipt = "")
        {
            log.Debug("FcsService: Sending prepay request...");
            log.Debug("FcsService: requestType:{0},PumpID:{1},amount:{2},invoiceID:{3},posID:{4}.", requestType.ToString(), pumpID, amount, invoiceID, posID);
            if (IsConnected)
            {
                log.Debug("FcsService: Fcs is connected.Create a new instance of prepay request.");
                PrepayRequest prepayRequest = new PrepayRequest() { Type = requestType.ToString(), PumpID = pumpID };
                if (requestType == PrepayRequestType.Set)
                {
                    prepayRequest.Amount = amount;
                    prepayRequest.InvoiceID = invoiceID;
                    prepayRequest.PayType = "Credit";
                    prepayRequest.PosID = posID;
                    prepayRequest.TotalPaid = totalPaid;
                    prepayRequest.Change = change != 0.0 ? -change : change;
                    prepayRequest.Receipt = receipt;
                }
                else if (requestType == PrepayRequestType.Switch)
                {
                    prepayRequest.OldPumpID = oldPumpId;
                }

                log.Debug("FcsService: Create a fcs command for prepay set.");
                FcsCommand<PrepayRequest> prepaySetCommand = new FcsCommand<PrepayRequest>() { Command = prepayRequest };
                FcsMessageSerializer serializer = fcsMessageSerializer;
                log.Debug("FcsService: Serialize prepayset command with fcsMessage Serializer and send it to fcsTcpPeer.");
                byte[] serializedBytes = serializer.Serialize<FcsCommand<PrepayRequest>>(prepaySetCommand);
                await fcsTcpPeer.SendAsync(serializedBytes).ConfigureAwait(false);

                log.Debug("FcsService: Waiting for prepay wait event to set.");
                prePayWaitEvent.WaitOne();

                var prepayResponse = this.receivedPrepayResponse;
                this.receivedPrepayResponse = null;
                log.Debug("FcsService: return prepay response command.");
                return prepayResponse.Command;
            }
            else
            {
                log.Debug("FcsService: Fcs is not connected.");
                return new PrepayResponse()
                {
                    PumpID = pumpID,
                    OldPumpID = "Old" + pumpID,
                    Result = "Ok"
                };
            }
        }

        //this method sends request for holding basket to fcs and returns the basket response of fcs.
        public async Task<BasketResponse> HoldBasket(string basketID)
        {
            log.Debug("FcsService: Holding basket with basketID:{0}..", basketID);
            BasketDetail basketDetailForHoldingBasket = new BasketDetail()
            {
                BasketID = basketID,
                Type = BasketRequestType.Hold
            };

            log.Debug("FcsService: Create a new instance of basket holding request.");
            BasketRequest basketHoldingRequest = new BasketRequest() { Type = "Hold", BasketDetail = basketDetailForHoldingBasket };
            log.Debug("FcsService: Create a Fcs Command for holding basket.");
            FcsCommand<BasketRequest> basketHoldingCommand = new FcsCommand<BasketRequest>() { Command = basketHoldingRequest };
            log.Debug("FcsService: Serialize basket holding  command with fcsMessage Serializer and send it to fcsTcpPeer.");
            byte[] serializedBytes = fcsMessageSerializer.Serialize<FcsCommand<BasketRequest>>(basketHoldingCommand);
            await fcsTcpPeer.SendAsync(serializedBytes).ConfigureAwait(false);

            log.Debug("FcsService: Waiting for basket removal wait event to set.");
            basketRemovalWaitEvent.WaitOne();

            var basketHoldingResponse = this.receivedBasketResponse;
            this.receivedBasketResponse = null;
            log.Debug("FcsService: Returning basket holding response command.");
            return basketHoldingResponse.Command;
            //todo:log the exception
        }

        //this method sends request for Canceling hold basket to fcs and returns the basket response of fcs.
        public async Task<BasketResponse> CancelHoldBasket(string basketID)
        {
            log.Debug("FcsService: Canceling hold basket with basketID:{0}..", basketID);
            BasketDetail basketDetailForCancelingHoldBasket = new BasketDetail()
            {
                BasketID = basketID,
                Type = BasketRequestType.Cancel
            };

            log.Debug("FcsService: Create a new instance of basket canceling hold request.");
            BasketRequest basketCancelingHoldRequest = new BasketRequest() { Type = "Cancel", BasketDetail = basketDetailForCancelingHoldBasket };
            log.Debug("FcsService: Create a Fcs Command for canceling hold basket.");
            FcsCommand<BasketRequest> basketCancelingHoldCommand = new FcsCommand<BasketRequest>() { Command = basketCancelingHoldRequest };
            log.Debug("FcsService: Serialize basket canceling hold  command with fcsMessage Serializer and send it to fcsTcpPeer.");
            byte[] serializedBytes = fcsMessageSerializer.Serialize<FcsCommand<BasketRequest>>(basketCancelingHoldCommand);
            await fcsTcpPeer.SendAsync(serializedBytes).ConfigureAwait(false);

            log.Debug("FcsService: Waiting for basket removal wait event to set.");
            basketRemovalWaitEvent.WaitOne();

            var basketCancelingHoldResponse = this.receivedBasketResponse;
            this.receivedBasketResponse = null;
            log.Debug("FcsService: Returning basket canceling hold response command.");
            return basketCancelingHoldResponse.Command;

        }

        //this method sends request for removing basket to fcs and returns the basket response of fcs.
        public async Task<BasketResponse> RemoveBasket(BasketDetail basketDetail,
                                                       double totalPaid = 0.0,
                                                       double change = 0.0,
                                                       string receipt = "",
                                                       string invoiceNo = null)
        {
            // to do: fix post pay invoice id
            var InvoiceId = invoiceNo;
            if (InvoiceId == null || InvoiceId.Length <= 0)
            {
                InvoiceId = "123456";
            }

            if (basketDetail.InvoiceType.Length == 0 || basketDetail.InvoiceType == null)
            {
                basketDetail.InvoiceType = "Sale";
            }

            BasketDetail basketDetailForRemoveBasket = new BasketDetail()
            {
                Type = BasketRequestType.Remove,
                PayType = basketDetail.PayType,
                PayDesc = basketDetail.PayDesc,
                BasketID = basketDetail.BasketID,
                InvoiceID = InvoiceId,
                InvoiceType = basketDetail.InvoiceType,
                UnitPrice = basketDetail.UnitPrice,
                TotalPaid = totalPaid,
                Change = change != 0.0 ? -change : change,
                Receipt = receipt
            };

            log.Debug("FcsService: Removing basket : {0}..", basketDetailForRemoveBasket);

            log.Debug("FcsService: Create a new instance of basket removal request.");
            BasketRequest basketRemovalRequest = new BasketRequest() { Type = "Remove", BasketDetail = basketDetailForRemoveBasket };
            log.Debug("FcsService: Create a fcs command for removing basket.");
            FcsCommand<BasketRequest> basketRemovalCommand = new FcsCommand<BasketRequest>() { Command = basketRemovalRequest };
            log.Debug("FcsService: Serialize basket removal  command with fcsMessage Serializer and send it to fcsTcpPeer.");
            byte[] serializedBytes = fcsMessageSerializer.Serialize<FcsCommand<BasketRequest>>(basketRemovalCommand);
            await fcsTcpPeer.SendAsync(serializedBytes).ConfigureAwait(false);

            log.Debug("FcsService: Waiting for basket removal wait event to set.");
            basketRemovalWaitEvent.WaitOne();

            var basketRemovalResponse = this.receivedBasketResponse;
            this.receivedBasketResponse = null;
            log.Debug("FcsService: Returning basket removal response command.");
            return basketRemovalResponse.Command;
            //todo:log the exception
        }

        public async Task<BasketResponse> HoldAndRemoveBasket(BasketDetail basketDetail,
                                                       double totalPaid = 0.0,
                                                       double change = 0.0,
                                                       string receipt = "",
                                                       string invoiceNo = null)
        {
            try
            {
                var response = await HoldBasket(basketDetail.BasketID);
                if (!response.ResultOK)
                    return response;

                response = await RemoveBasket(basketDetail,
                                              totalPaid,
                                              change,
                                              receipt,
                                              invoiceNo);
                return response;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private void FcsClient_ConnectionEstablished(object sender, ConnectionEstablishedEventArgs eventArgs)
        {
            log.Debug("FcsService: FcsClient_ConnectionEstablished fires when connection is established..");
            fcsTcpPeer = eventArgs.RemoteTcpPeer;
            OnConnectionStatusChange(true);
        }

        private void FcsClient_ConnectionClosed(object sender, ConnectionClosedEventArgs e)
        {
            log.Debug("FcsService: FcsClient_ConnectionClosed fires when connection is closed.");
            fcsTcpPeer = null;
            OnConnectionStatusChange(false);
        }


        private void FcsClient_FrameArrived(object sender, TcpFrameArrivedEventArgs e)
        {
            log.Debug("FcsService: FcsClient_FrameArrived fires on arrival of any frame.");
            byte[] bytesToProcess = null;
            if (unprocessedData?.Length > 0)
            {
                // There are some unprocessed data we should include them in the processing
                log.Debug("FcsService: Process the unprocessed data.");
                bytesToProcess = new byte[unprocessedData.Length + e.FrameData.Length];
                Buffer.BlockCopy(unprocessedData, 0, bytesToProcess, 0, unprocessedData.Length);
                Buffer.BlockCopy(e.FrameData, 0, bytesToProcess, unprocessedData.Length, e.FrameData.Length);
                unprocessedData = null;
            }
            else
            {
                bytesToProcess = e.FrameData;
            }
            FcsMessageSerializer fcsMessageSerializer = this.fcsMessageSerializer;

            bool continueProcess;
            do
            {
                continueProcess = false;
                log.Debug("FcsSercvice: DeSerialize byte to process by fcsMessageSerializer.");
                var result = fcsMessageSerializer.Deserialize(bytesToProcess);
                if (result?.IsSuccessful == true)
                {
                    log.Debug("FcsService: Various actions depend on MessageType " + result.MessageType);
                    switch (result.MessageType)
                    {
                        case FcsMessageType.SignOnResponse:
                            OnReceivedSignOnResponse((FcsResponse<SignOnResponse>)result.FcsMessage);
                            break;
                        case FcsMessageType.SignOnTPosResponse:
                            OnReceivedSignOnTPosResponse((FcsResponse<SignOnTPosResponse>)result.FcsMessage);
                            break;
                        case FcsMessageType.SignOffTPosResponse:
                            OnReceivedSignOffTPosResponse((FcsResponse<SignOffTPosResponse>)result.FcsMessage);
                            break;
                        case FcsMessageType.EventStatus:
                            OnReceiveFcsStatus((FcsEvent<FcsStatus>)result.FcsMessage);
                            break;
                        case FcsMessageType.EventPriceChanged:
                            OnFuelPriceChangeEvent((FcsEvent<PriceChangedEvent>)result.FcsMessage);
                            break;
                        case FcsMessageType.PrepayResponse:
                            OnRecievePrepayResponse((FcsResponse<PrepayResponse>)result.FcsMessage);
                            break;
                        case FcsMessageType.EventPrepay:
                            OnReceivePrepayEvent((FcsEvent<PrepayEvent>)result.FcsMessage);
                            break;
                        case FcsMessageType.BasketCommand:
                            OnReceiveBasketCommand((FcsCommand<BasketRequest>)result.FcsMessage);
                            break;
                        case FcsMessageType.BasketResponse:
                            OnReceiveBasketResponse((FcsResponse<BasketResponse>)result.FcsMessage);
                            break;
                        case FcsMessageType.ConfigurationCommand:
                            OnReceivedConfigurationCommand((FcsCommand<ConfigurationRequest>)result.FcsMessage);
                            break;
                        case FcsMessageType.SetPumpResponse:
                            OnReceivedSetPumpResponse((FcsResponse<SetPumpResponse>)result.FcsMessage);
                            break;
                        case FcsMessageType.SetPriceResponse:
                            OnReceivedSetPriceResponse((FcsResponse<SetPriceResponse>)result.FcsMessage);
                            break;
                        case FcsMessageType.GetReportResponse:
                            OnReceivedGetReportResponse((FcsResponse<GetReportResponse>)result.FcsMessage);
                            break;
                        case FcsMessageType.GetReceiptResponse:
                            OnReceivedGetReceiptResponse((FcsResponse<GetReceiptResponse>)result.FcsMessage);
                            break;
                        case FcsMessageType.GetReceiptDataResponse:
                            OnReceivedGetReceiptDataResponse((FcsResponse<GetReceiptDataResponse>)result.FcsMessage);
                            break;
                        default:// Todo: if there is something needs to be done here??
                            break;
                    }
                }

                if (result.ByteProcessed < bytesToProcess.Length)
                {
                    log.Debug("FcsService: Processing the unprocessed bytes of the result.");
                    int bytesLeft = bytesToProcess.Length - result.ByteProcessed;
                    byte[] tempBytes = new byte[bytesLeft];
                    Buffer.BlockCopy(bytesToProcess, result.ByteProcessed, tempBytes, 0, bytesLeft);
                    bytesToProcess = tempBytes;
                    continueProcess = result.IsSuccessful;
                }
                else
                {
                    bytesToProcess = null;
                }
            } while (continueProcess && bytesToProcess?.Length > 0);

            if (bytesToProcess?.Length > 0)
            {
                unprocessedData = bytesToProcess;
            }
        }

        private void OnReceivedGetReportResponse(FcsResponse<GetReportResponse> getReportResponse)
        {
            log.Debug("FcsService: Receiving Get Report response: {0}", getReportResponse.ToString());
            this.receivedGetReportResponse = getReportResponse;
            try
            {
                log.Debug("FcsService: Set getReportWaitEvent.");
                this.getReportWaitEvent.Set();
            }
            catch (Exception ex)
            {
                log.DebugException("FcsService: exception: {0}", ex);
            }
        }

        private void OnReceivedSetPriceResponse(FcsResponse<SetPriceResponse> setPriceResponse)
        {
            log.Debug("FcsService: Receiving Set price response: {0}", setPriceResponse.ToString());
            this.receivedSetPriceResponse = setPriceResponse;
            try
            {
                log.Debug("FcsService: Set setPriceWaitEvent.");
                this.setPriceWaitEvent.Set();
            }
            catch (Exception ex)
            {
                log.ErrorException("FcsService: exception: {0}", ex);
            }
        }

        private void OnReceivedSetPumpResponse(FcsResponse<SetPumpResponse> setPumpReponse)
        {
            log.Debug("FcsService: Receiving Set pump response: {0}", setPumpReponse.ToString());
            this.receivedSetPumpResponse = setPumpReponse;
            try
            {
                log.Debug("FcsService: Set setPumpWaitEvent.");
                this.setPumpWaitEvent.Set();
            }
            catch (Exception ex)
            {
                log.DebugException("FcsService: exception: {0}", ex);
            }
        }

        // this method create Configuration Response commmand and send it to fcs as a response.
        private async void OnReceivedConfigurationCommand(FcsCommand<ConfigurationRequest> configurationCommand)
        {
            try
            {
                log.Debug("FcsService: Create configuration response command");
                ConfigurationResponse configurationResponse = new ConfigurationResponse()
                {
                    Result = "OK",
                    ErrMsg = "No error occured"
                };

                FcsResponse<ConfigurationResponse> configurationResponseCommand = new FcsResponse<ConfigurationResponse>()
                {
                    Command = configurationResponse
                };

                log.Debug("FcsService: Send configuration response command after FcsMessageSerializer serialized it.");
                byte[] serializedBytes = fcsMessageSerializer.Serialize<FcsResponse<ConfigurationResponse>>(configurationResponseCommand);
                await fcsTcpPeer.SendAsync(serializedBytes).ConfigureAwait(false);

                FCSConfig = configurationCommand.Command;
                log.Debug("FcsService: FcsReceivedConfigurationEvent is invoked.");
                this.FcsReceivedConfigurationEvent?.Invoke(this, configurationCommand.Command);
            }
            catch (Exception ex)
            {
                log.DebugException("FcsService: exception: {0}", ex);
            }
        }

        private void OnReceivedSignOffTPosResponse(FcsResponse<SignOffTPosResponse> fcsMessage)
        {
            log.Debug("FcsService: Receiving sign off TPos reponse.");
            this.receivedSignOffTPosResponse = fcsMessage;
            try
            {
                log.Debug("FcsService: Set sign off Tpos wait event.");
                this.signOffTPosWaitEvent.Set();
            }
            catch (Exception ex)
            {
                log.DebugException("FcsService: exception: {0}", ex);
            }
        }

        private void OnReceivedSignOnTPosResponse(FcsResponse<SignOnTPosResponse> fcsMessage)
        {
            log.Debug("FcsService: Receiving sign on TPos reponse.");
            this.receivedSignOnTPosResponse = fcsMessage;
            try
            {
                log.Debug("FcsService: Set sign on Tpos wait event.");
                this.signOnTPosWaitEvent.Set();
            }
            catch (Exception ex)
            {
                log.DebugException("FcsService: exception: {0}", ex);
            }
        }

        private void OnReceiveBasketResponse(FcsResponse<BasketResponse> basketResponse)
        {
            log.Debug("FcsService: Receiving basket response.");
            this.receivedBasketResponse = basketResponse;
            try
            {
                log.Debug("FcsService: Set basket removal wait event.");
                this.basketRemovalWaitEvent.Set();
            }
            catch (Exception ex)
            {
                log.DebugException("FcsService: exception:{0}.", ex);
            }
        }

        // this method create Basket Response commmand and send it to fcs as a response.
        private async void OnReceiveBasketCommand(FcsCommand<BasketRequest> basketCommand)
        {
            try
            {
                log.Debug("FcsService: Create basket response command.");
                basketCommand.Command.BasketDetail.Type = (BasketRequestType)Enum.Parse(typeof(BasketRequestType), basketCommand.Command.Type);
                BasketResponse basketResponse = new BasketResponse() { Type = basketCommand.Command.Type, BasketID = basketCommand.Command.BasketDetail.BasketID, Result = "OK" };
                FcsResponse<BasketResponse> basketResponseCommand = new FcsResponse<BasketResponse>() { Command = basketResponse };
                log.Debug("FcsService: Send basket response command after FcsMessageSerializer serialized it.");
                byte[] serializedBytes = fcsMessageSerializer.Serialize<FcsResponse<BasketResponse>>(basketResponseCommand);
                await fcsTcpPeer.SendAsync(serializedBytes).ConfigureAwait(false);
                log.Debug("FcsService: FcsReceivedBasketEvent is invoked.");
                this.FcsReceivedBasketEvent?.Invoke(this, basketCommand.Command);
            }
            catch (Exception ex)
            {
                log.DebugException("FcsService: exception:{0}.", ex);
            }
        }


        private void OnReceivePrepayEvent(FcsEvent<PrepayEvent> prepayEvent)
        {
            try
            {
                log.Debug("FcsService: Fcs prepay event is invoked.");
                this.FcsPrepayEvent?.Invoke(this, prepayEvent.Command);
            }
            catch (Exception ex)
            {
                log.DebugException("FcsService: exception:{0}.", ex);
            }
        }

        private void OnFuelPriceChangeEvent(FcsEvent<PriceChangedEvent> priceChangeEvent)
        {
            try
            {
                if (FCSConfig != null)
                {
                    int totalChangesMade = 0;
                    priceChangeEvent.Command.PriceChanges.ForEach(change =>
                    {
                        FCSConfig.FuelPrice.Grades.ForEach(grade =>
                        {
                            if (grade.IsType(change.Grade))
                            {
                                grade.Price.UnitPrice = change.CurrentPrice;
                                totalChangesMade++;
                            }
                        });
                    });

                    if (totalChangesMade > 0)
                    {
                        this.FuelPriceChanged?.Invoke(this, priceChangeEvent.Command);
                    }
                }
            }
            catch (Exception ex)
            {
                log.ErrorException("FcsService: exception:{0}.", ex);
            }
        }

        private void OnReceiveFcsStatus(FcsEvent<FcsStatus> statusEvent)
        {
            try
            {
                this.CurrentFcsStatus = statusEvent.Command;
                log.Debug("FcsService: Fcs status change event invoked and current fcs status is:{0}.", this.CurrentFcsStatus);
                this.FcsStatusChanged?.Invoke(this, this.CurrentFcsStatus);
            }
            catch (Exception ex)
            {
                log.DebugException("FcsService: exception:{0}.", ex);
            }
        }

        private void OnReceivedSignOnResponse(FcsResponse<SignOnResponse> response)
        {
            log.Debug("FcsService: Receiving sign on response..");
            this.receivedSignOnResponse = response;
            try
            {
                log.Debug("FcsService: Set sign on wait event..");
                this.signOnWaitEvent.Set();
            }
            catch (Exception ex)
            {
                log.DebugException("FcsService: exception:{0}.", ex);
            }
        }

        private void OnRecievePrepayResponse(FcsResponse<PrepayResponse> response)
        {
            log.Debug("FcsService: Receiving prepay response..");
            this.receivedPrepayResponse = response;
            try
            {
                log.Debug("FcsService: Set prepay wait event");
                this.prePayWaitEvent.Set();
            }
            catch (Exception ex)
            {
                log.DebugException("FcsService: exception:{0}.", ex);
            }
        }

        private void OnReceivedGetReceiptResponse(FcsResponse<GetReceiptResponse> getReceiptResponse)
        {
            log.Debug("FcsService: Receiving Get Receipt response: {0}", getReceiptResponse.ToString());
            this.receivedGetReceiptResponse = getReceiptResponse;
            try
            {
                log.Debug("FcsService: Set getReceipttWaitEvent.");
                this.getReceiptWaitEvent.Set();
            }
            catch (Exception ex)
            {
                log.DebugException("FcsService: exception: {0}", ex);
            }
        }

        private void OnReceivedGetReceiptDataResponse(FcsResponse<GetReceiptDataResponse> getReceiptDataResponse)
        {
            log.Debug("FcsService: Receiving Get Receipt Data response: {0}", getReceiptDataResponse.ToString());
            this.receivedGetReceiptDataResponse = getReceiptDataResponse;
            try
            {
                log.Debug("FcsService: Set getReportWaitEvent.");
                this.getReceiptDataWaitEvent.Set();
            }
            catch (Exception ex)
            {
                log.DebugException("FcsService: exception: {0}", ex);
            }
        }

        protected virtual void OnConnectionStatusChange(bool connectionStatus)
        {
            try
            {
                log.Debug("FcsService: Fcs connection status change event is invoked.");
                this.ConnectionStatusChanged?.Invoke(this, connectionStatus);
            }
            catch (Exception ex)
            {
                log.DebugException("FcsService: exception:{0}.", ex);
            }
        }
    }
}
