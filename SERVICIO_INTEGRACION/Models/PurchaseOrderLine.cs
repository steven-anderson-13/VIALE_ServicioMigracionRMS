using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SERVICIO_INTEGRACION.Models
{
    public class PurchaseOrderLine
    {
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemDescription { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public string WarehouseCode { get; set; }
        public string TaxCode { get; set; }
        public string U_BPP_OPER { get; set; }
        public string U_tipoOpT12 { get; set; }
        public string SKU { get; set; }
    }
}
