using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VIALE_ServicioMigracionRMS.Models
{
    public class RMSDownPaymentAddressData
    {
        public string AddressName { get; set; }
        public string Giro { get; set; }
        public string CCalle { get; set; }
        public string CCuadra { get; set; }

        public string ZipCode { get; set; }
        public string CRegion { get; set; }
        public string CCodPais { get; set; }

        public string Street { get; set; }
        public string AddressName2 { get; set; }
        public string AddressName3 { get; set; }

        public string EmailAddress { get; set; }
        public string BilltoDefault { get; set; }
        public string FederalTaxId { get; set; }
        public string ShipToDefault { get; set; }

        public string City { get; set; }
        public string Country { get; set; }
        public string County { get; set; }
        public string Stat { get; set; }
        public string TaxCode { get; set; }

        public string AddressType { get; set; }
    }
}
