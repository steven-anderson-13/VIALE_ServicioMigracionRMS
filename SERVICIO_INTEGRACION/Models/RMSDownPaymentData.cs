using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VIALE_ServicioMigracionRMS.Models
{
    public class RMSDownPaymentData
    {
        public int StoreNo { get; set; }
        public string ReceiptId { get; set; }
        public int ReceiptNo { get; set; }
        public int CustomerNo { get; set; }

        public string NumAtCard { get; set; }

        public DateTime DocDate { get; set; }
        public DateTime TaxDate { get; set; }
        public DateTime DocDueDate { get; set; }

        public string DocCurrency { get; set; }
        public string Comments { get; set; }

        public string DocTypeSUNAT { get; set; }
        public string Serie { get; set; }
        public string Correlativo { get; set; }

        public string U_BPV_SERI { get; set; }
        public string U_BPV_NCON2 { get; set; }
        public string JrnlMemo { get; set; }

        public string U_CL_REFPA1 { get; set; }
        public string U_RMS_VALIDATX { get; set; }

        public decimal PayTotal { get; set; }
    }
}
