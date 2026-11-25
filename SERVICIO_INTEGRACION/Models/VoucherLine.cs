using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SERVICIO_INTEGRACION.Models
{
    public class VoucherLine
    {
        public Guid VoucherId { get; set; }
        public string ItemCode { get; set; }
        public string ItemDescription { get; set; }
        public double Quantity { get; set; }
        public double Price { get; set; }
        public string WarehouseCode { get; set; }
        public int? BaseType { get; set; }
        public int? BaseEntry { get; set; }
        public int? BaseLine { get; set; }
        public string TaxCode { get; set; }
        public string U_BPP_OPER { get; set; }
        public string U_tipoOpT12 { get; set; }
        public string MeasureUnit { get; set; }
        public string PONo { get; set; }
        public string SKU { get; set; }
    }
}
