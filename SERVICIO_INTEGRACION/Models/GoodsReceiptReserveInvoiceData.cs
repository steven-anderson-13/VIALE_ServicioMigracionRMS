using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VIALE_ServicioMigracionRMS.Models
{
    public class GoodsReceiptReserveInvoiceData
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }

        public DateTime DocDate { get; set; }
        public DateTime DocDueDate { get; set; }

        public string CardCode { get; set; }
        public string CardName { get; set; }

        public string DocCur { get; set; }
        public decimal DocTotal { get; set; }

        public string U_BPP_MDTD { get; set; }
        public string U_BPP_MDSD { get; set; }
        public string U_BPP_MDCD { get; set; }

        public string Comments { get; set; }
        public string JournalMemo { get; set; }
    }
}
