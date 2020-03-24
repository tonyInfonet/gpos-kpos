using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.DB.Entities
{
    public class Company
    {
        public string CompanyName { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string Province { get; set; }

        public void Print(StringWriter streamWriter)
        {
            streamWriter.WriteLine(CompanyName);
            streamWriter.WriteLine(Address1);
            if (Address2?.Length > 0)
                streamWriter.WriteLine(Address2);
            streamWriter.WriteLine("{0}, {1}", City, Province);
        }
    }
}
