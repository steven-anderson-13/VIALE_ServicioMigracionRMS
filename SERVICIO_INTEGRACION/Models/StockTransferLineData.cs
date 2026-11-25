using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VIALE_ServicioMigracionRMS.Models
{
    public class StockTransferLineData
    {
        public int DocEntry { get; set; }
        public int LineNum { get; set; }

        public string ItemCode { get; set; }
        public string Dscription { get; set; }

        public decimal Quantity { get; set; }

        public string WhsCode { get; set; }
        public string UomCode { get; set; }
    }
}
