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
    public class GoodsReceiptReserveInvoiceLogRepository
    {
        private readonly string _connectionString;

        public GoodsReceiptReserveInvoiceLogRepository()
        {
            _connectionString = ConfigurationManager.AppSettings["Rms.ConnectionString"];
        }

        public bool ExisteEntradaExitosa(int docEntryOPCH)
        {
            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(@"
            SELECT COUNT(1)
            FROM RMS_SBO_GoodsReceiptReserveInvoice_Log
            WHERE DocEntryOPCH = @DocEntryOPCH
              AND ((Estado = 'OK') 
			  OR (Estado='ERROR' AND FechaActualizacion>FechaRegistro)
			  )", cn))
            {
                cmd.Parameters.Add("@DocEntryOPCH", SqlDbType.Int).Value = docEntryOPCH;

                cn.Open();

                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public void RegistrarResultado(int docEntryOPCH, int docNumOPCH, int? docEntryOPDN, int? docNumOPDN, string estado, string mensaje)
        {
            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("SP_RMS_RegistrarResultadoGoodsReceiptReserveInvoice", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@DocEntryOPCH", SqlDbType.Int).Value = docEntryOPCH;
                cmd.Parameters.Add("@DocNumOPCH", SqlDbType.Int).Value = docNumOPCH;
                cmd.Parameters.Add("@DocEntryOPDN", SqlDbType.Int).Value = (object)docEntryOPDN ?? DBNull.Value;
                cmd.Parameters.Add("@DocNumOPDN", SqlDbType.Int).Value = (object)docNumOPDN ?? DBNull.Value;
                cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 20).Value = estado;
                cmd.Parameters.Add("@Mensaje", SqlDbType.NVarChar, -1).Value = (object)mensaje ?? DBNull.Value;

                cn.Open();
                cmd.ExecuteNonQuery();
                cn.Close();
            }
        }
    }
}
