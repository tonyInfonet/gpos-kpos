using InfonetPos.FcsIntegration.Entities;
using InfonetPos.FcsIntegration.Enums;
using InfonetPos.FcsIntegration.Helpers;
using InfonetPos.FcsIntegration.Services.Interfaces;
using InfonetPOS.Core.DB.Entities;
using InfonetPOS.Core.DB.Enums;
using InfonetPOS.Core.DB.Interface;
using InfonetPOS.Core.Entities;
using InfonetPOS.Core.Enums;
using InfonetPOS.Core.Interfaces;
using MvvmCross;
using MvvmCross.Logging;
using MvvmCross.ViewModels;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InfonetPOS.Core.Helpers
{
    [XmlType("PosSale")]
    public class PosSale
    {
        public int PumpId { get; set; }

        [XmlArray("SaleStatuses"), XmlArrayItem(typeof(SaleStatus))]
        public List<SaleStatus> SaleStatuses { get; set; }
    }

    [AddINotifyPropertyChangedInterface]
    public class PosManager
    {
        private readonly IAppSettings appSettings;
        private readonly IFcsService fcsService;
        private readonly IMvxLog log;
        private readonly IDBAccess dbAccess;

        private Dictionary<int, IList<SaleStatus>> currentSales;
        private Dictionary<int, IList<SaleStatus>> backupPrepaySales;
        private string oldSaleFileLocation;

        public Company PosCompany { get; private set; }
        private List<Tender> tenders;

        public PosManager(IAppSettings appSettings,
                          IFcsService fcsService,
                          IMvxLog log,
                          IDBAccess dbAccess)
        {
            this.appSettings = appSettings;
            this.fcsService = fcsService;
            this.log = log;
            this.dbAccess = dbAccess;

            oldSaleFileLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            oldSaleFileLocation = Path.Combine(oldSaleFileLocation, "OldSales.xml");
            PosCompany = this.dbAccess.GetCompany();
            CanCloseApp = true;
            basketQueue = new Queue<BasketRequest>();
        }

        public SaleStatus ActiveSale { get; private set; }

        #region logged in user data
        public string UserName { get; private set; }
        public int TillNo { get; private set; }
        public int ShiftNo { get; private set; }
        public DateTime? ShiftDate { get; private set; }
        public bool CanCloseApp { get; set; }
        private bool isInitialized = false;
        private Queue<BasketRequest> basketQueue;

        private void ClearLoggedInUserData()
        {
            log.Debug("PosManager: Clear logged in user data.");
            UserName = null;
            TillNo = 0;
            ShiftNo = 0;
        }
        public bool IsUserLoggedIn()
        {
            log.Debug("PosManager: Check if user LoggedIn or not.");
            return UserName != null;
        }
        #endregion

        public event EventHandler<int> BasketChangedEvent;
        public event EventHandler<KeyValuePair<int, int>> PrepaySalePumpSwitchEvent;

        private KeyValuePair<string, string> defaultRemoveBasketTenderInfo =
            new KeyValuePair<string, string>(
                TenderClass.CRCARD.ToString(),
                "Credit");

        private KeyValuePair<string, string> GetRemoveBasketTenderInfo(SaleStatus currentSale)
        {
            log.Debug("PosManager: Get Remove Basket tender info.");
            if (currentSale.SoldBasket.Prepay == null
                && currentSale.SaleTender != null)
            {
                log.Debug("PosManager: SaleType is not prepay.Sale tender exists.");
                return new KeyValuePair<string, string>(
                   currentSale.SaleTender.Class.ToString(),
                   currentSale.SaleTender.Description);
            }
            else if (currentSale.SoldBasket.Prepay != null
                && currentSale.RefundTender != null)
            {
                log.Debug("PosManager: SaleType is prepay and Refund tender exists.");
                return new KeyValuePair<string, string>(
                   currentSale.RefundTender.Class.ToString(),
                   currentSale.RefundTender.Description);
            }
            return defaultRemoveBasketTenderInfo;
        }

        private KeyValuePair<string, string> GetAutoRemoveBasketTenderInfo(SaleStatus currentSale)
        {
            log.Debug("PosManager: Get auto remove basket tender info.");
            if (currentSale.SaleTender != null)
            {
                log.Debug("PosManager: SaleTender is not null.");
                return new KeyValuePair<string, string>(
                    currentSale.SaleTender.Class.ToString(),
                    currentSale.SaleTender.Description);
            }

            return defaultRemoveBasketTenderInfo;
        }

        private string GetInvoiceNo(SaleStatus sale)
        {
            log.Debug("PosManager: Get invoice no.");
            string invoiceNo = null;
            if (sale?.SoldBasket?.Prepay != null)
            {
                invoiceNo = sale?.SoldBasket?.Prepay?.PrepayInvoice;
            }
            else
            {
                invoiceNo = sale?.InvoiceNo;
            }

            if (invoiceNo != null)
            {
                log.Debug("PosManager: Invoice no is:{0}", invoiceNo);
            }
            else
            {
                log.Error("PosManager: Invoice no is null.");
            }

            return invoiceNo;
        }

        public async Task CleanUpSale(SaleStatus currentSale, int pumpId, bool cleanBasket)
        {
            var log = Mvx.IoCProvider.Resolve<IMvxLog>();
            var fcsService = Mvx.IoCProvider.Resolve<IFcsService>();
            var appSettings = Mvx.IoCProvider.Resolve<IAppSettings>();
            bool clearSale = true;

            log.Debug("PosManager: Clean up sale.");

            if (currentSale?.SoldBasket != null && cleanBasket)
            {
                log.Debug("PosManager: Trying to remove basket.");

                currentSale.SoldBasket.SetPayTenderInfo(GetRemoveBasketTenderInfo(currentSale));


                var response = await fcsService?.RemoveBasket(currentSale.SoldBasket,
                                                             currentSale.TotalPaid,
                                                             currentSale.Change,
                                                             currentSale.Receipt,
                                                             GetInvoiceNo(currentSale));
                if (!response.ResultOK)
                {
                    //to do basket remove error
                    log.Error("PosManager: Remove basket error.");
                    clearSale = false;
                }
                else
                {
                    log.Debug("PosManager: Remove basket successfully.");
                }

            }

            if (currentSale?.SaleType == SaleType.Prepay)
            {
                var currentPrepayStatus = fcsService?.CurrentFcsStatus?.GetPrepayStatus(pumpId);
                if (currentSale.IsPrepayHoldRemove && (currentPrepayStatus == PrepayStatus.Hold))
                {
                    log.Debug("PosManager: If prepay is set before delete, then remove prepay set.");

                    var response = await fcsService?.PrepayRemoveAsync(pumpId);

                    if (response.ResultOk)
                    {
                        log.Debug("PosManager: If prepay set is removed successfully then make IsPrepaySet in currentSale as false.");
                        currentSale.IsPrepaySet = false;
                        currentSale.IsPrepayHoldRemove = false;
                    }
                    else
                    {
                        log.Error("PosManager: Prepay remove error.");
                    }
                }
                else if (currentSale.IsPrepaySet && (currentPrepayStatus == PrepayStatus.Set))
                {
                    log.Debug("PosManager: If prepay is set,then remove prepay set.");

                    if ((await fcsService.PrepayHoldRemoveAsync(pumpId)).ResultOk)
                    {
                        log.Debug("PosManager: Prepay hold remove successfully.");
                        var response = await fcsService?.PrepayRemoveAsync(pumpId);

                        if (response.ResultOk)
                        {
                            log.Debug("PosManager: If prepay set is removed successfully then make IsPrepaySet in currentSale as false.");
                            currentSale.IsPrepaySet = false;
                        }
                        else
                        {
                            log.Error("PosManager: Prepay remove error");
                        }
                    }
                    else
                    {
                        log.Error("PosManager: Prepay hold remove error.");
                    }
                }
                else if (currentSale.IsPrepayHold && (currentPrepayStatus == PrepayStatus.Hold))
                {
                    log.Debug("PosManager: If prepay is hold, then cancel prepayHold.");
                    var response = await fcsService?.PrepayCancelHoldAsync(pumpId);
                    if (response.ResultOk)
                    {
                        log.Debug("PosManager: Make IsPrepayHold in currentSale as false as prepay hold is cancelled.");
                        currentSale.IsPrepayHold = false;
                    }
                    else
                    {
                        log.Error("PosManager: Prepay cancel hold error.");
                    }
                }
            }

            if (clearSale == true)
            {
                log.Debug("PosManager: ClearSale the sale.");
                IList<SaleStatus> sales = currentSales[pumpId];
                if (sales != null)
                {
                    if (sales.Contains(currentSale))
                    {
                        sales.Remove(currentSale);
                    }
                }

            }
            else
            {
                log.Error("PosManager: Clear sale is false.");
            }
        }

        public void StartNewSale(int pumpId)
        {
            log.Debug("PosManager: Start new sale.");
            var newSale = CreateNewSale(pumpId);
            if (newSale != null)
                ActiveSale = newSale;
        }

        private SaleStatus CreateNewSale(int pumpId)
        {
            log.Debug("PosManager: Create new sale for pumpId {0}", pumpId);
            var sales = GetSales(currentSales, pumpId);

            if (sales != null)
            {
                SaleStatus sale = new SaleStatus();
                sale.InitiateSale();
                sale.SaleTime = DateTime.Now;
                sale.PumpId = pumpId;
                sales.Add(sale);
                log.Debug("PosManager: Create sale successfully.");
                return sale;
            }

            log.Error("PosManager: Create sale failure.");
            return null;
        }

        public async void ProcessQueuedBaskets()
        {
            log.Debug("PosManager: Process queued baskets.");
            while (true)
            {
                try
                {
                    var basket = basketQueue.Dequeue();
                    await OnBasketReceived(basket);
                }
                catch (InvalidOperationException ex)
                {
                    log.DebugException("PosManager: Queue is empty.", ex);
                    break;
                }
            }
        }

        public void ClearActiveSale()
        {
            log.Debug("PosManager: Clear active sale.");
            ActiveSale = null;
        }

        public void SetActiveSale(int pumpId, string basketID)
        {
            log.Debug("PosManager: Set Active sale for pump {0} and BasketID:{1}", pumpId, basketID);
            var sales = GetSales(currentSales, pumpId);
            if (sales != null)
            {
                var sale = sales.FirstOrDefault(saleStatus => saleStatus?.SoldBasket?.BasketID == basketID);
                if (sale != null)
                {
                    log.Debug("PosManager: Find sale successfully.");
                    ActiveSale = sale;
                    ActiveSale.PumpId = pumpId;
                }
                else
                {
                    log.Error("PosManager: Failed to find sale.");
                }
            }
            else
            {
                log.Error("PosManager: Sales are null.");
            }
        }

        public async Task OnBasketReceived(BasketRequest basketRequest)
        {
            log.Debug("PosManager: On basket received.");
            if (basketRequest == null || basketRequest.BasketDetail == null)
            {
                log.Error("PosManager: BasketRequest is null or BasketDetail is null.Simply return.");
                return;
            }

            if (isInitialized == false)
            {
                log.Error("PosManager: Pos is not initialized.Enqueue BasketQueue.");
                basketQueue.Enqueue(basketRequest);
                return;
            }

            switch (basketRequest.Type.ToLower())
            {
                case "create":
                    await CreateBasket(basketRequest.BasketDetail);
                    break;
                case "clear":
                    ClearBasket(basketRequest.BasketDetail);
                    break;
            }
        }

        private void ClearBasket(BasketDetail basketDetail)
        {
            log.Debug("PosManager: Clear Basket.");
            var removedSale = RemoveSaleWithBasketId(currentSales, basketDetail.BasketID);
            if (removedSale != null)
            {
                log.Debug("PosManager: RemovedSale is found.");
                if (removedSale.SoldBasket.Prepay != null)
                {
                    log.Debug("PosManager: RemovedSale.SoldBasket.Prepay is not null.Add removed sale to back up sales.");
                    AddSale(backupPrepaySales, removedSale);
                }

                InvokeBasketChangedEventForPump(true, removedSale.PumpId);
            }
            else
            {
                log.Error("PosManager: RemovedSale not found.");
            }
        }

        private bool IsSamePrepay(BasketDetail first, BasketDetail second)
        {
            log.Debug("PosManager: Check if same prepay or not.");
            if ((first == null || second == null)
                || first.Prepay == null || second.Prepay == null
                || first.Prepay.PrepayInvoice == null || second.Prepay.PrepayInvoice == null)
            {
                log.Debug("PosManager: IsSamePrepay is false.");
                return false;
            }

            if (first.Prepay.PrepayInvoice == second.Prepay.PrepayInvoice)
            {
                log.Debug("PosManager: IsSamePrepay is true.");
                return true;
            }
            else
            {
                log.Debug("PosManager:IsSamePrepay is false.");
                return false;
            }
        }

        private SaleStatus GetSaleWithSameBasketId(IList<SaleStatus> sales,
            string basketId)
        {
            log.Debug("PosManager: Get sale with same BasketID.");
            if (sales != null && basketId != null)
            {
                foreach (var sale in sales)
                {
                    if (sale.SoldBasket != null
                        && sale.SoldBasket.BasketID == basketId)
                    {
                        log.Debug("PosManager: Find sale successfully.");
                        return sale;
                    }
                }
            }

            log.Error("PosManager: Can't get sale with same BasketID.");
            return null;
        }

        private SaleStatus RemoveSaleWithBasketId(Dictionary<int, IList<SaleStatus>> dictionary,
            string basketId)
        {
            log.Debug("PosManager: RemoveSale with BasketID:{0}", basketId);
            foreach (var saleCollection in dictionary)
            {
                var sales = saleCollection.Value;
                var targetSale = GetSaleWithSameBasketId(sales, basketId);
                if (targetSale != null)
                {
                    sales.Remove(targetSale);
                    log.Debug("PosManager: Remove sale successfully.");
                    return targetSale;
                }
            }

            log.Error("PosManager: Remove sale failure.");
            return null;
        }

        private bool AddSale(Dictionary<int, IList<SaleStatus>> dictionary,
            SaleStatus sale)
        {
            log.Debug("PosManager: Adding sale.");
            if (sale == null)
            {
                log.Error("PosManager: Can't add null sale.Simply return.");
                return false;
            }

            var sales = GetSales(dictionary, sale.PumpId);
            if (sales == null)
            {
                log.Error("PosManager: Sales are null.Can't add any sale.");
                return false;
            }

            log.Debug("PosManager: Added sale successfully.");
            sales.Add(sale);
            return true;
        }

        public void StartPos()
        {
            log.Debug("PosManager: Starting Pos.");
            if (currentSales == null)
            {
                log.Debug("PosManager: CurrentSales are not null.");
                currentSales = new Dictionary<int, IList<SaleStatus>>();
                backupPrepaySales = new Dictionary<int, IList<SaleStatus>>();

                if (App.PosType == POSType.GPOS)
                {
                    LoadPumpsGpos();
                }
                else
                {
                    LoadPumpsKpos();
                }

                LoadOldSales();
                isInitialized = true;
            }
            else
            {
                log.Error("PosManager: CurrentSales are null.");
            }
        }

        public async Task StopPos()
        {
            log.Debug("PosManager: Stop Pos.");
            isInitialized = false;
            if (currentSales != null)
            {
                log.Debug("PosManager: CurrentSales is not null.Save Old sales.");
                SaveOldSales();
                currentSales = null;
                backupPrepaySales = null;
            }
            else
            {
                log.Error("PosManager: CurrentSales is null.");
            }

            if (App.IsFcsConnected && App.IsSignOnDone)
            {
                log.Debug("PosManager: Fcs is connected and SignOn done.Trying to sign off.");
                await App.SignOffAsync();
            }
            else
            {
                log.Error("PosManager: Fcs is not connected or SignOn is not done.");
            }
        }

        private SaleStatus GetPrepaySaleNoBasket(int pumpId, List<PrepayStatus> acceptableStatuses)
        {
            log.Debug("PosManager: Get PrepaySale  for NoBasket.");
            var prepayStatus = fcsService?.CurrentFcsStatus?.GetPrepayStatus(pumpId);
            var acceptable = false;
            foreach (var status in acceptableStatuses)
            {
                if (status == prepayStatus)
                    acceptable = true;
            }
            if (!acceptable)
            {
                log.Debug("PosManager: Can't get prepay sale for NoBasket.");
                return null;
            }

            var sales = currentSales[pumpId];
            if (sales == null)
            {
                log.Debug("PosManager: Can't get prepay sale for NoBasket.");
                return null;
            }

            foreach (var sale in sales)
            {
                if (sale.SaleType == SaleType.Prepay
                    && sale.IsPrepaySet
                    && sale.SoldBasket == null)
                {
                    log.Debug("PosManager: Successfully get prepay sale for NoBasket.");
                    return sale;
                }
            }

            log.Debug("PosManager: Can't get prepay sale for NoBasket.");
            return null;
        }

        public bool SwitchPrepaySale(int pumpId, int oldPumpId)
        {
            log.Debug("PosManager: Switching prepay sale.PumpId:{0},OldPumpId:{1}", pumpId, oldPumpId);
            var prepaySale = GetPrepaySaleNoBasket(oldPumpId, new List<PrepayStatus>()
            {
                PrepayStatus.Set
            });

            if (prepaySale == null)
            {
                log.Error("PosManager: Can't switch prepay sale.");
                return false;
            }

            currentSales[oldPumpId].Remove(prepaySale);

            currentSales[pumpId].Add(prepaySale);
            prepaySale.PumpId = pumpId;

            PrepaySalePumpSwitchEvent?.Invoke(this,
                new KeyValuePair<int, int>(pumpId, oldPumpId));

            log.Debug("PosManager:Switch prepay sale successfully.");
            return true;
        }

        public bool SetPrepaySaleAsActive(int pumpId)
        {
            log.Debug("PosManager: Set prepay sale as active for pump {0}", pumpId);
            var sale = GetPrepaySaleNoBasket(pumpId, new List<PrepayStatus>()
            {
                PrepayStatus.Set
            });

            if (sale == null)
            {
                log.Debug("PosManager: sale is null.Return false.");
                return false;
            }

            ActiveSale = sale;
            log.Debug("PosManager: Succesfully set prepay sale as active.");
            return true;
        }

        public double GetPrepayAmount(int pumpId)
        {
            log.Debug("PosManager: Get prepay amount for pump {0}", pumpId);
            var sale = GetPrepaySaleNoBasket(pumpId, new List<PrepayStatus>()
            {
                PrepayStatus.Set,
                PrepayStatus.Pumping
            });

            if (sale == null)
            {
                log.Debug("PosManager: Sale is null.Preay amount is 0.");
                return 0;
            }

            log.Debug("PosManager: Prepay amount is {0}", sale.Amount);
            return sale.Amount;
        }

        public List<int> GetSupportedPumpIds()
        {
            log.Debug("PosManager: Fetching supported pump Ids.");
            return currentSales?.Keys.ToList();
        }

        private void LoadPumpsKpos()
        {
            log.Debug("PosManager: Loading Pumps for KPOS.");
            foreach (var pumpConfig in fcsService.FCSConfig.Pumps)
            {
                currentSales.Add(Convert.ToInt32(pumpConfig.PumpID), new List<SaleStatus>());
                backupPrepaySales.Add(Convert.ToInt32(pumpConfig.PumpID), new List<SaleStatus>());
            }
        }

        private void LoadPumpsGpos()
        {
            log.Debug("PosManager: Loading Pumps for GPOS."); 
            foreach (var pumpId in this.appSettings.PumpIds)
            {
                currentSales.Add(pumpId, new List<SaleStatus>());
                backupPrepaySales.Add(pumpId, new List<SaleStatus>());
            }
        }

        private void LoadOldSales()
        {
            if (File.Exists(oldSaleFileLocation))
            {
                log.Debug("Loading old sales from file: {0}", File.ReadAllText(oldSaleFileLocation));
                var oldSales = XmlHelper.Deserialize<List<PosSale>>(File.ReadAllText(oldSaleFileLocation));
                var currentTime = DateTime.Now;

                foreach (var oldSale in oldSales)
                {
                    if (oldSale?.SaleStatuses?.Count() > 0)
                    {
                        if (currentSales.ContainsKey(oldSale.PumpId))
                        {
                            IList<SaleStatus> saleList = new List<SaleStatus>();
                            foreach (var sale in oldSale.SaleStatuses)
                            {
                                if (currentTime.Subtract(sale.SaleTime).Days < 1)
                                {
                                    saleList.Add(sale);
                                }
                            }
                            currentSales[oldSale.PumpId] = saleList;
                        }
                    }
                }

                File.Delete(oldSaleFileLocation);
            }
        }

        private void SaveOldSales()
        {
            log.Debug("Saving sales data to file");

            try
            {
                var currentTime = DateTime.Now;
                var oldSales = new List<PosSale>();
                bool hasPendingSale = false;


                foreach (var sale in currentSales)
                {
                    var saleStatusListForPump = new PosSale();
                    saleStatusListForPump.PumpId = sale.Key;
                    saleStatusListForPump.SaleStatuses = new List<SaleStatus>();
                    foreach (var saleStatus in sale.Value)
                    {
                        // If sale is older that a day then no need to save it
                        if (currentTime.Subtract(saleStatus.SaleTime).Days < 1)
                        {
                            if (saleStatus.SaleType != SaleType.Postpay)
                            {
                                hasPendingSale = true;
                                saleStatusListForPump.SaleStatuses.Add(saleStatus);
                            }
                        }
                    }
                    oldSales.Add(saleStatusListForPump);
                }

                if (hasPendingSale)
                {
                    var oldSalesInXml = XmlHelper.Serialize<List<PosSale>>(oldSales);

                    using (var writer = new StreamWriter(oldSaleFileLocation))
                    {
                        writer.WriteLine(oldSalesInXml);
                        writer.Close();
                    }
                }

            }
            catch (Exception ex)
            {
                log.DebugException(string.Format("PosManager: Exception:{0}", ex.Message), ex);
            }
            log.Debug("Saving sales data to file complete");

        }


        private async Task CreateBasket(BasketDetail basketDetail)
        {
            log.Debug("PosManager: Creating basket.");
            if (basketDetail.Prepay != null)
            {
                log.Debug("PosManager: Prepay basket with basket Id:{0}", basketDetail.BasketID);
                await CreatePrepayBasket(basketDetail);
            }
            else
            {
                log.Debug("PosManager: Postpay basket with basket Id:{0}", basketDetail.BasketID);
                CreatePostpayBasket(basketDetail);
            }
        }

        private void CreatePostpayBasket(BasketDetail basketDetail)
        {
            // trying to remove existing sale with same basket because
            // FCS sends unnecessary BasketCreate to the POS who originally 
            // called BasketCancelHold
            log.Debug("PosManager: Creating Postpay basket.");
            RemoveSaleWithBasketId(currentSales, basketDetail.BasketID);

            log.Debug("PosManager:Handling postpay basket");
            int pumpId = int.Parse(basketDetail.PumpID);

            log.Debug("PosManager: PostpayBasket with basketId:{0} and basketType:Create.", basketDetail.BasketID);

            var sale = CreateNewSale(pumpId);
            if (sale == null)
                return;

            sale.SaleType = SaleType.Postpay;
            sale.SoldBasket = basketDetail;
            sale.Amount = basketDetail.Amount;

            InvokeBasketChangedEventForPump(true, pumpId);
        }

        private async Task CreatePrepayBasket(BasketDetail basketDetail)
        {
            int pumpId = int.Parse(basketDetail.PumpID);
            log.Debug("PosManager: Creating prepay basket for pump {0}", pumpId);

            if (RestorePrepaySaleFromBackup(basketDetail))
            {
                log.Debug("PosManager: Prepay sale with basket restored from backup");
                InvokeBasketChangedEventForPump(true, pumpId);
            }
            else
            {
                log.Debug("PosManager: Handling new prepay basket");
                var sales = GetSales(currentSales, pumpId);
                bool isBasketChanged = false;

                if (sales != null)
                {
                    var sale = sales.FirstOrDefault(saleStatus => saleStatus?.InvoiceNo == basketDetail?.Prepay?.PrepayInvoice);
                    if (sale != null)
                    {
                        log.Debug("PosManager: Prepay basket with basketId:{0} and basketTye create.", basketDetail.BasketID);
                        if (basketDetail.IsRefundAvailable())
                        {
                            log.Debug("PosManager: There is refund for this prepay basket.");
                            if (sale.SoldBasket == null)
                            {
                                sale.SoldBasket = basketDetail;
                                isBasketChanged = true;
                            }
                        }
                        else
                        {
                            log.Debug("PosManager: There is no refund for this prepay basket.Hold and Remove basket");

                            basketDetail.SetPayTenderInfo(GetAutoRemoveBasketTenderInfo(sale));

                            var response = await fcsService?.HoldAndRemoveBasket(basketDetail,
                                                                                 sale.TotalPaid,
                                                                                 sale.Change,
                                                                                 sale.Receipt,
                                                                                 GetInvoiceNo(sale));
                            if (response.ResultOK)
                            {
                                log.Debug("PosManager: Hold and remove basket successfully.Remove Sale from sale status list.");
                                sales.Remove(sale);
                            }
                            else
                            {
                                log.Error("PosManager: Hold and remove  Basket failed.");
                            }
                        }
                    }
                }
                InvokeBasketChangedEventForPump(isBasketChanged, pumpId);
            }
        }

        private bool RestorePrepaySaleFromBackup(BasketDetail basketDetail)
        {
            log.Debug("PosManager: Restore prepay sale from back up sales.");
            var removedSale = RemoveSaleWithBasketId(backupPrepaySales, basketDetail.BasketID);
            if (removedSale != null && IsSamePrepay(removedSale.SoldBasket, basketDetail))
            {
                return AddSale(currentSales, removedSale);
            }

            return false;
        }

        private void InvokeBasketChangedEventForPump(bool isBasketChanged, int pumpId)
        {
            log.Debug("PosManager: Invoke Basket changed event for pump {0}.", pumpId);
            if (isBasketChanged)
            {
                log.Debug("PosManger: Baskets have changed for pumpId:{0}.Invoke BasketChangedEvent.", pumpId);
                BasketChangedEvent?.Invoke(this, pumpId);
            }
        }

        public MvxObservableCollection<BasketDetail> GetBasketsForPump(int pumpId)
        {
            log.Debug("PosManager: Sending baskets for pumpId:{0}.", pumpId);
            var sales = GetSales(currentSales, pumpId);
            MvxObservableCollection<BasketDetail> baskets = new MvxObservableCollection<BasketDetail>();

            if (sales != null)
            {

                foreach (var sale in sales)
                {
                    if (sale.SoldBasket != null)
                    {
                        log.Debug("PosManager: Sending basket with basketId:{0}", sale.SoldBasket.BasketID);
                        baskets.Add(sale.SoldBasket);
                    }
                }

            }
            return baskets;
        }

        private IList<SaleStatus> GetSales(Dictionary<int, IList<SaleStatus>> dictionary,
            int pumpId)
        {
            IList<SaleStatus> sales;
            dictionary.TryGetValue(pumpId, out sales);
            return sales;
        }

        public bool PosLogin(string userName, int shiftNo, int tillNo)
        {
            log.Debug("PosManager: Login with new till");
          
            UserName = userName;
            TillNo = tillNo;
            ShiftNo = shiftNo;
            ShiftDate = DateTime.Now;

            log.Debug("PosManager: Pos login with username:{0}, shiftno:{1}, tillno:{2}.",
                UserName, ShiftNo, TillNo);

            return dbAccess.OpenTill(UserName, ShiftNo, TillNo, ShiftDate);

        }

        public bool PosLogin(Till till)
        {
            log.Debug("PosManager: Login with old till");
            
            UserName = till.UserLoggedOn;
            TillNo = till.TillNo;
            ShiftNo = till.ShiftNo;
            ShiftDate = till.ShiftDate;

            log.Debug("PosManager: Pos login with username:{0}, shiftno:{1}, tillno:{2}.",
                UserName, ShiftNo, TillNo);

            return dbAccess.StartTill(TillNo);
        }

        public bool PosLogout(bool closeTill)
        {
            log.Debug("PosManager: Logout pos.");
            bool result = true;
            if (closeTill)
            {
                result = dbAccess.CSCTillsMoveRecords(ShiftDate, TillNo);
                if (result)
                {
                    result = dbAccess.CloseTill(TillNo);
                }
            }
            else
            {
                result = dbAccess.StopTill(TillNo);
            }
            if (result)
            {
                log.Debug("PosManager: Log out successfully.Clear loggedIn user data.");
                ClearLoggedInUserData();
            }
            else
            {
                log.Error("PosManager: Log out failed.");
            }

            return result;
        }

        public void PreparePrepayDelete(SaleStatus sale)
        {
            log.Debug("PosManager: Prepare prepay delete.");
            sale.IsPrepayHoldRemove = true;
            sale.RefundReason = RefundReason.PrepayRemoved;
        }

        public void ResetPrepayDelete(SaleStatus sale)
        {
            log.Debug("PosManager: Reset prepay delete.");
            sale.IsPrepayHoldRemove = false;
            sale.RefundReason = RefundReason.None;
        }

        public List<Tender> GetTenders()
        {
            log.Debug("PosManager: Get tender.");
            if (tenders == null)
            {
                log.Debug("PosManager: Tenders is null.Fetch Tenders List and keep it in memory.");
                tenders = dbAccess.GetTenders();
            }
            return tenders;
        }
    }
}
