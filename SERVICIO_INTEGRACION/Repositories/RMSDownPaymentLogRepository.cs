using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VIALE_ServicioMigracionRMS.Repositories
{
    public class RMSDownPaymentLogRepository
    {
        private readonly string _connectionString;

        public RMSDownPaymentLogRepository()
        {
            _connectionString = ConfigurationManager.AppSettings["Rms.ConnectionString"];
        }

        public void RegistrarResultado(int receiptNo, string receiptId, int? docEntry, int? docNum, string estado, string mensaje)
        {
            using (var cn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("SP_RMS_RegistrarResultadoDownPayment", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 120;
                cmd.Parameters.Add("@ReceiptNo", SqlDbType.Int).Value = receiptNo;
                cmd.Parameters.Add("@ReceiptId", SqlDbType.VarChar, 100).Value = receiptId;
                cmd.Parameters.Add("@DocEntry", SqlDbType.Int).Value = (object)docEntry ?? DBNull.Value;
                cmd.Parameters.Add("@DocNum", SqlDbType.Int).Value = (object)docNum ?? DBNull.Value;
                cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 20).Value = estado;
                cmd.Parameters.Add("@Mensaje", SqlDbType.NVarChar, -1).Value = mensaje ?? "";
                cn.Open();
                cmd.ExecuteNonQuery();
                cn.Close();
            }
        }

        public bool ExisteMigracionExitosa(int storeNo, string receiptId)
        {
            using (var cn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
            SELECT COUNT(1)
            FROM dbo.RMS_SBO_DownPayment_Log
            WHERE StoreNo = @StoreNo
              AND ReceiptId = @ReceiptId
              AND Estado = 'OK';", cn))
            {
                cmd.Parameters.Add("@StoreNo", SqlDbType.Int).Value = storeNo;
                cmd.Parameters.Add("@ReceiptId", SqlDbType.VarChar, 100).Value = receiptId;
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }
    }
}
