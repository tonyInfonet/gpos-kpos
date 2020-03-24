using System.Xml.Serialization;

namespace InfonetPos.FcsIntegration.Entities
{
    public enum PumpStatus
    {
        Idle,// (I) -> Pump is ready for next customer.
        Calling,// (C) -> Pump nozzle is lifted and grade selected.
        Authorized,// (A) -> Pump is authorized.
        Pumping,// (P)-> Fueling in progress.
        Stopped,// (S)-> Pump is stopped.
        Offline,// (O) -> Pump is inactive and can not be used.
        Finished,// (F)-> Pumping is finished.
        NoConnection,// (N) -> No connection to pump.
        Holding,// (H) -> Pump on Hold
        None
    }

    public enum PrepayStatus
    {
        NoPrepay,// (N)
        Hold,// (H)
        Set,// (S)
        Pumping,// (P)
    }

    [XmlType("Status")]
    public class FcsStatus
    {
        [XmlElement("Pump")]
        public string PumpsStatus { get; set; }
        [XmlElement("Prepay")]
        public string PrepaysStatus { get; set; }
        [XmlElement("Reader")]
        public string ReadersStatus { get; set; }

        public PumpStatus GetPumpStatus(int pumpID)
        {
            PumpStatus status = PumpStatus.None;
            int pumpIndex = pumpID - 1;
            switch(PumpsStatus[pumpIndex])
            {
                case 'I':
                    status = PumpStatus.Idle;
                    break;
                case 'C':
                    status = PumpStatus.Calling;
                    break;
                case 'A':
                    status = PumpStatus.Authorized;
                    break;
                case 'P':
                    status = PumpStatus.Pumping;
                    break;
                case 'S':
                    status = PumpStatus.Stopped;
                    break;
                case 'O':
                    status = PumpStatus.Offline;
                    break;
                case 'F':
                    status = PumpStatus.Finished;
                    break;
                case 'N':
                    status = PumpStatus.NoConnection;
                    break;
                case 'H':
                    status = PumpStatus.Holding;
                    break;
            }

            return status;
        }

        public PrepayStatus GetPrepayStatus(int pumpID)
        {
            int pumpIndex = pumpID - 1;
            PrepayStatus status = PrepayStatus.NoPrepay;
            switch(PrepaysStatus[pumpIndex])
            {
                case 'H':
                    status = PrepayStatus.Hold;
                    break;
                case 'S':
                    status = PrepayStatus.Set;
                    break;
                case 'P':
                    status = PrepayStatus.Pumping;
                    break;
            }

            return status;
        }

        public override string ToString()
        {
            return string.Format("PumpStatus:[{0}], PrepayStatus[{1}], ReaderStatus[{2}]", this.PumpsStatus, this.PrepaysStatus, this.ReadersStatus);
        }
    }
}