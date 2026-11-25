using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SERVICIO_INTEGRACION.Models
{
    public class VoucherHeader
    {
        public Guid VoucherId { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime DocDueDate { get; set; }
        public string CardCode { get; set; }
        public string JournalMemo { get; set; }
        public string DocCurrency { get; set; }
        public double? DocRate { get; set; }
        public string Comments { get; set; }
        public string U_VS_TIPOPER { get; set; }
        public string U_VS_TIPOOC { get; set; }
        public string U_BPP_MDTD { get; set; }
        public string U_BPP_MDSD { get; set; }
        public string U_BPP_MDCD { get; set; }
        public string U_BPV_SERI { get; set; }
        public string U_BPV_NCON2 { get; set; }
        public string U_VS_NATNUM { get; set; }
        public string NumAtCard { get; set; }
        public string DocType { get; set; }
        public string MigracionSBO { get; set; }
        public string PONo { get; set; }
    }
}
