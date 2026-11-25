using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VIALE_ServicioMigracionRMS.Models
{
    public class RMSDownPaymentLineData
    {
        public string ItemCode { get; set; }

        public int LineNum { get; set; }

        public string Dscription { get; set; }

        public decimal Quantity { get; set; }

        public decimal Price { get; set; }

        public decimal DiscPrcnt { get; set; }

        public string TaxCode { get; set; }

        public string WhsCode { get; set; }

        public string AccountCode { get; set; }

        public string CostingCode { get; set; }

        public string CostingCode2 { get; set; }

        public string CostingCode3 { get; set; }

        public string CostingCode4 { get; set; }

        public string CostingCode5 { get; set; }
    }
}
