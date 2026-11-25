using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VIALE_ServicioMigracionRMS.Models
{
    public class StockTransferData
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }

        public DateTime DocDate { get; set; }

        public string CardCode { get; set; }
        public string CardName { get; set; }

        public string DocCur { get; set; }
        public decimal DocTotal { get; set; }
    }
}
