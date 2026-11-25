using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VIALE_ServicioMigracionRMS.Models
{
    public class GoodsReceiptReserveInvoiceLineData
    {
        public int DocEntry { get; set; }
        public int LineNum { get; set; }

        public string ItemCode { get; set; }
        public string Dscription { get; set; }

        public decimal Quantity { get; set; }

        public string WhsCode { get; set; }
        public string UomCode { get; set; }

        public decimal Price { get; set; }
        public string Currency { get; set; }
        public string TaxCode { get; set; }
        public decimal DiscPrcnt { get; set; }

        public string AcctCode { get; set; }

        public string OcrCode { get; set; }
        public string OcrCode2 { get; set; }
        public string OcrCode3 { get; set; }
        public string OcrCode4 { get; set; }
        public string OcrCode5 { get; set; }
    }
}
