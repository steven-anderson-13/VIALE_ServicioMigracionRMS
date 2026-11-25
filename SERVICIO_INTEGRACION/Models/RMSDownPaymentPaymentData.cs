using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VIALE_ServicioMigracionRMS.Models
{
    public class RMSDownPaymentPaymentData
    {
        public string AccountCode { get; set; }
        public string Currency { get; set; }

        public DateTime TaxDate { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime DueDate { get; set; }

        public int TenderId { get; set; }

        public string Voucher { get; set; }
        public string CrTypeCode { get; set; }
        public string CardNumber { get; set; }

        public decimal PayTotal { get; set; }
        public decimal PayTotalFC { get; set; }

        public string TransferRef { get; set; }

        public string CardExpDate { get; set; }
        public string CardAuthorization { get; set; }
        public string CardHolderName { get; set; }

        public int Quotas { get; set; }

        public string CodigoAut { get; set; }
        public string CodigoBanco { get; set; }

        public string RutCheque { get; set; }
        public string NumCheque { get; set; }
        public string CtaCte { get; set; }
        public string SucBco { get; set; }
        public DateTime? FecCheque { get; set; }

        public string CountryCode { get; set; }

        public string MPPG { get; set; }
        public string TPOOPERI { get; set; }
        public string NCUserType { get; set; }

        public string CreditCardStoreNo { get; set; }

        public string U_BPP_EFEC { get; set; }
        public string U_BPP_TARJ { get; set; }
        public string U_BPP_TRAN { get; set; }
        public string U_BPP_CHEQ { get; set; }
        public string U_BPP_LETR { get; set; }

        public decimal U_VS_PRECIO_DOLARES { get; set; }

        public string U_CL_NROVAL { get; set; }
        public string ForeignPay { get; set; }
        public string JournalRemarks { get; set; }
    }
}
