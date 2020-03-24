using MvvmCross.ViewModels;
using InfonetPos.FcsIntegration.Entities;
using PropertyChanged;
using InfonetPos.FcsIntegration.Enums;
using System.ComponentModel;

namespace InfonetPOS.Core.Entities
{
    [AddINotifyPropertyChangedInterface]
    public class PumpDetails : INotifyPropertyChanged
    {
        #region enums
        private enum PrepaySwitchCandidateType
        {
            None,
            Source,
            PossibleDestination
        }
        #endregion

        #region public props
        public int Id { get; set; }
        public string Status { get; set; }
        public PrepayStatus PumpPrepayStatus { get; set; }
        public string PrepayStatusStr
        {
            get
            {
                if (PumpPrepayStatus == null || PumpPrepayStatus == PrepayStatus.NoPrepay)
                    return "";

                return PumpPrepayStatus.ToString();
            }
        }
        public bool CashierAuthorize { get; set; }
        public bool AllowPostpay { get; set; }
        public double PrepayAmount { get; set; }
        public bool ShouldShowPrepayAmount
        {
            get => PrepayAmount > 0;
        }

        private MvxObservableCollection<BasketDetail> baskets;
        public MvxObservableCollection<BasketDetail> Baskets
        {
            get => baskets;
            set
            {
                baskets = value;
                RaiseAllPropertyChanged();
            }
        }
        public MvxObservableCollection<BasketDetail> TwoBaskets
        {
            get
            {
                if(Baskets.Count > 2)
                {
                    return new MvxObservableCollection<BasketDetail>()
                    {
                        Baskets[Baskets.Count - 2],
                        Baskets[Baskets.Count - 1]
                    };
                }

                return Baskets;
            }
        }
        #endregion

        #region property change event
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaiseAllPropertyChanged()
        {
            this.PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(string.Empty));
        }
        #endregion

        #region prepay switch candidate
        private PrepaySwitchCandidateType switchStatus = PrepaySwitchCandidateType.None;
        public void SetSwitchStatus(int sourcePumpId)
        {
            if (sourcePumpId == Id)
                switchStatus = PrepaySwitchCandidateType.Source;
            else
                switchStatus = PrepaySwitchCandidateType.PossibleDestination;

            RaiseAllPropertyChanged();
        }
        public void ClearSwitchStatus()
        {
            switchStatus = PrepaySwitchCandidateType.None;
            RaiseAllPropertyChanged();
        }
        private bool NoSwitch()
        {
            return switchStatus == PrepaySwitchCandidateType.None;
        }
        #endregion

        public PumpDetails()
        {
            Baskets = new MvxObservableCollection<BasketDetail>();
        }

        #region status
        public bool CanPrepay
        {
            get => NoSwitch()
                && Status == PumpStatus.Idle.ToString()
                && PumpPrepayStatus == PrepayStatus.NoPrepay
                && Baskets.Count < 2;
        }

        public bool CanSwitchPrepay
        {
            get => NoSwitch()
                && Status == PumpStatus.Idle.ToString()
                && PumpPrepayStatus == PrepayStatus.Set;
        }

        public bool CanCancelPrepaySwitch
        {
            get => switchStatus == PrepaySwitchCandidateType.Source;
        }

        public bool CanConfirmPrepaySwitch
        {
            get => switchStatus == PrepaySwitchCandidateType.PossibleDestination
                && PumpPrepayStatus == PrepayStatus.NoPrepay
                && Status == PumpStatus.Idle.ToString()
                && Baskets.Count < 2;
        }

        public bool CanDeletePrepay
        {
            get => NoSwitch()
                && Status == PumpStatus.Idle.ToString()
                && PumpPrepayStatus == PrepayStatus.Set;
        }

        public bool CanAuthorize
        {
            get => NoSwitch()
                && AllowPostpay && CashierAuthorize
                && PumpPrepayStatus == PrepayStatus.NoPrepay
                && Status == PumpStatus.Calling.ToString()
                && Baskets.Count < 2;
        }

        public bool CanStop
        {
            get => Connected()
              //  && App.PosType == POSType.KPOS
                && NoSwitch()
                && Status != PumpStatus.Stopped.ToString();
        }

        public bool CanStart
        {
            get => NoSwitch()
             //   && App.PosType == POSType.KPOS
                && Status == PumpStatus.Stopped.ToString();
        }
        #endregion

        private bool Connected()
        {
            return Status != PumpStatus.Offline.ToString()
                && Status != PumpStatus.NoConnection.ToString()
                && Status != PumpStatus.None.ToString();
        }
    }
}
