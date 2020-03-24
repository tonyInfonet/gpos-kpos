using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.DB.Entities
{
    public class Till
    {
        public int TillNo { get; set; }
        public bool Active { get; set; }
        public bool Process { get; set; }
        public int ShiftNo { get; set; }
        public int PosID { get; set; }
        public DateTime? ShiftDate { get; set; }
        public string UserLoggedOn { get; set; }
    }
}
