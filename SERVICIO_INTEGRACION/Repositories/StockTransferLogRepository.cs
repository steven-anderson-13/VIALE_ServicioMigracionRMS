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
    public class StockTransferLogRepository
    {
        private readonly string _connectionString;

        public StockTransferLogRepository()
        {
            _connectionString = ConfigurationManager.AppSettings["Rms.ConnectionString"];
        }

        public bool ExisteTransferenciaExitosa(int docEntryOPDN)
        {
            using (var cn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
            SELECT COUNT(1)
            FROM dbo.RMS_SBO_StockTransfer_Log
            WHERE DocEntryOPDN = @DocEntry
              AND ((Estado = 'OK') 
			  OR (Estado='ERROR' AND FechaActualizacion>FechaRegistro)
			  )", cn))
            {
                cmd.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntryOPDN;
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public void RegistrarResultado(int docEntryOPDN, int docNumOPDN, int? docEntryOWTR, int? docNumOWTR, string estado, string mensaje)
        {
            using (var cn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.SP_RMS_RegistrarResultadoStockTransfer", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@DocEntryOPDN", SqlDbType.Int).Value = docEntryOPDN;
                cmd.Parameters.Add("@DocNumOPDN", SqlDbType.Int).Value = docNumOPDN;
                cmd.Parameters.Add("@DocEntryOWTR", SqlDbType.Int).Value = (object)docEntryOWTR ?? DBNull.Value;
                cmd.Parameters.Add("@DocNumOWTR", SqlDbType.Int).Value = (object)docNumOWTR ?? DBNull.Value;
                cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 20).Value = estado;
                cmd.Parameters.Add("@Mensaje", SqlDbType.NVarChar).Value = (object)mensaje ?? DBNull.Value;
                cn.Open();
                cmd.ExecuteNonQuery();
                cn.Close();
            }
        }
    }
}
