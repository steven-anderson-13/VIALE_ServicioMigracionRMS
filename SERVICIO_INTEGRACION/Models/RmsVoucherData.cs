using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VIALE_ServicioMigracionRMS.Models
{
    public class RmsVoucherData
    {
        public List<string> VoucherNo { get; set; } = new List<string>();

        public DateTime? ReceiveDate { get; set; }

        public string VoucherNoConcatenados =>
            string.Join(",", VoucherNo);
    }
}
