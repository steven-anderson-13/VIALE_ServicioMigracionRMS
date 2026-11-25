using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VIALE_ServicioMigracionRMS.Models
{
    public class RMSDownPaymentCustomerData
    {
        public string CardCode { get; set; }
        public int GroupCode { get; set; }

        public string CompanyName { get; set; }
        public string FederalTaxId { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }

        public string Email { get; set; }
        public string Phone1 { get; set; }
        public string Phone2 { get; set; }

        public string Address { get; set; }
        public string ZipCode { get; set; }

        public string U_BPP_BPTD { get; set; }
        public string U_BPP_BPTP { get; set; }

        public string U_VS_NOMCOM { get; set; }
        public string U_BPP_BPAP { get; set; }
        public string U_BPP_BPAM { get; set; }
        public string U_BPP_BPNO { get; set; }
        public string U_VS_EXCCOD { get; set; }
    }
}
