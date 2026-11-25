using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SERVICIO_INTEGRACION.Models
{
    public class PurchaseOrderHeader
    {
        public Guid POId { get; set; }
        public string DocType { get; set; }
        public string DocDate { get; set; }
        public string DocDueDate { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string DocCurrency { get; set; }
        public string U_VS_TIPOPER { get; set; }
        public string U_VS_TIPOOC { get; set; }
        public string Comments { get; set; }
        public string JournalMemo { get; set; }
        public string StatusCode { get; set; }
    }
}
