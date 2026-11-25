using SERVICIO_INTEGRACION.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VIALE_ServicioMigracionRMS.Models;

namespace SERVICIO_INTEGRACION.Data
{
    public class RmsVoucherRepository
    {
        private readonly string _connectionString;

        public RmsVoucherRepository()
        {
            _connectionString = ConfigurationManager.AppSettings["Rms.ConnectionString"];
        }
        public List<VoucherHeader> GetVouchers(DateTime fechaDesde, DateTime fechaHasta)
        {
            var vouchers = new List<VoucherHeader>();

            using (var cn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.SP_RMS_ObtenerVouchers", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@FechaDesde", SqlDbType.Date).Value = fechaDesde.Date;
                cmd.Parameters.Add("@FechaHasta", SqlDbType.Date).Value = fechaHasta.Date;

                cn.Open();

                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var voucher = new VoucherHeader
                        {
                            VoucherId = Guid.Parse(dr["VoucherId"].ToString()),
                            DocDate = ParseDate(dr["DocDate"]),
                            DocDueDate = ParseDate(dr["DocDueDate"]),
                            CardCode = GetString(dr, "CardCode"),
                            JournalMemo = GetString(dr, "JournalMemo"),
                            DocCurrency = GetString(dr, "DocCurrency"),
                            DocRate = GetNullableDouble(dr, "DocRate"),
                            Comments = GetString(dr, "Comments"),
                            U_VS_TIPOPER = GetString(dr, "U_VS_TIPOPER"),
                            U_VS_TIPOOC = GetString(dr, "U_VS_TIPOOC"),
                            U_BPP_MDTD = GetString(dr, "U_BPP_MDTD"),
                            U_BPP_MDSD = GetString(dr, "U_BPP_MDSD"),
                            U_BPP_MDCD = GetString(dr, "U_BPP_MDCD"),
                            U_BPV_SERI = GetString(dr, "U_BPV_SERI"),
                            U_BPV_NCON2 = GetString(dr, "U_BPV_NCON2"),
                            U_VS_NATNUM = GetString(dr, "U_VS_NATNUM"),
                            NumAtCard = GetString(dr, "NumAtCard"),
                            DocType = GetString(dr, "DocType"),
                            MigracionSBO = GetString(dr, "MigracionSBO"),
                            PONo = GetString(dr, "PONo")
                        };
                        vouchers.Add(voucher);
                    }
                }

                cn.Close();
            }

            return vouchers;
        }
        public List<VoucherLine> GetVoucherLines(Guid voucherId)
        {
            var lines = new List<VoucherLine>();

            using (var cn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.SP_RMS_ObtenerDetalleVoucher", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@VoucherId", SqlDbType.UniqueIdentifier).Value = voucherId;

                cn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var line = new VoucherLine
                        {
                            VoucherId = voucherId,
                            ItemCode = GetString(dr, "ItemCode"),
                            ItemDescription = GetString(dr, "ItemDescription"),
                            Quantity = GetDouble(dr, "Quantity"),
                            Price = GetDouble(dr, "Price"),
                            WarehouseCode = GetString(dr, "WarehouseCode"),
                            BaseType = GetNullableInt(dr, "BaseType"),
                            BaseEntry = GetNullableInt(dr, "BaseEntry"),
                            BaseLine = GetNullableInt(dr, "BaseLine"),
                            TaxCode = GetString(dr, "TaxCode"),
                            U_BPP_OPER = GetString(dr, "U_BPP_OPER"),
                            U_tipoOpT12 = GetString(dr, "U_tipoOpT12"),
                            MeasureUnit = GetString(dr, "MeasureUnit"),
                            PONo = GetString(dr, "PONo"),
                            SKU = GetString(dr, "SKU")
                        };
                        lines.Add(line);
                    }
                }

                cn.Close();
            }

            return lines;
        }
        public void SaveResult(Guid voucherId, string estado, int? docEntrySAP, int? docNumSAP, string mensajeError)
        {
            using (var cn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.SP_RMS_RegistrarResultadoVoucher", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@VoucherId", SqlDbType.UniqueIdentifier).Value = voucherId;
                cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 20).Value = estado;
                cmd.Parameters.Add("@DocEntrySAP", SqlDbType.Int).Value = (object)docEntrySAP ?? DBNull.Value;
                cmd.Parameters.Add("@DocNumSAP", SqlDbType.Int).Value = (object)docNumSAP ?? DBNull.Value;
                cmd.Parameters.Add("@MensajeError", SqlDbType.NVarChar, -1).Value = (object)mensajeError ?? DBNull.Value;

                cn.Open();
                cmd.ExecuteNonQuery();
                cn.Close();
            }
        }
        private static string GetString(SqlDataReader dr, string column)
        {
            if (dr[column] == DBNull.Value)
                return null;

            return Convert.ToString(dr[column]).Trim();
        }
        private static double GetDouble(SqlDataReader dr, string column)
        {
            if (dr[column] == DBNull.Value)
                return 0;

            return Convert.ToDouble(dr[column]);
        }
        private static double? GetNullableDouble(SqlDataReader dr, string column)
        {
            if (dr[column] == DBNull.Value)
                return null;

            double value;

            if (double.TryParse(Convert.ToString(dr[column]), out value))
            {
                return value;
            }

            return null;
        }
        private static int? GetNullableInt(SqlDataReader dr, string column)
        {
            if (dr[column] == DBNull.Value)
                return null;

            int value;

            if (int.TryParse(Convert.ToString(dr[column]), out value))
            {
                return value;
            }

            return null;
        }
        private static DateTime ParseDate(object value)
        {
            if (value == null || value == DBNull.Value)
                throw new InvalidOperationException("La fecha recibida desde RMS está vacía.");

            string texto = Convert.ToString(value).Trim();

            DateTime fecha;

            if (DateTime.TryParseExact(texto, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out fecha))
            {
                return fecha;
            }

            return Convert.ToDateTime(value);
        }
        public RmsVoucherData ObtenerVoucherNoPorPOIds(List<int> poIds)
        {
            var resultado = new RmsVoucherData();

            if (poIds == null || poIds.Count == 0)
                return resultado;

            using (SqlConnection cn = new SqlConnection(_connectionString))
            {
                cn.Open();

                // Parámetros dinámicos seguros
                var parametros = poIds
                    .Distinct()
                    .Select((id, index) => new
                    {
                        Nombre = "@POId" + index,
                        Valor = id
                    })
                    .ToList();

                string inClause = string.Join(",", parametros.Select(p => p.Nombre));

                string query = $@"
                SELECT 
                    V.VoucherNo,V.ReceiveDate
                FROM [RETAILDATA].[dbo].VOUCHER V
                INNER JOIN [RETAILDATA].[dbo].PO P ON P.POId=V.POId
                WHERE P.PONo IN ({inClause})
                  AND V.VoucherNo IS NOT NULL
                ORDER BY V.ReceiveDate, V.VoucherNo";

                using (SqlCommand cmd = new SqlCommand(query, cn))
                {
                    foreach (var parametro in parametros)
                    {
                        cmd.Parameters.Add(parametro.Nombre, SqlDbType.Int).Value = parametro.Valor;
                    }

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string voucherNo = Convert.ToString(reader["VoucherNo"]);        

                            if (!string.IsNullOrWhiteSpace(voucherNo))
                            {
                                resultado.VoucherNo.Add(voucherNo.Trim());
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("ReceiveDate")))
                            {
                                DateTime fecha = Convert.ToDateTime(reader["ReceiveDate"]);

                                if (!resultado.ReceiveDate.HasValue || fecha < resultado.ReceiveDate.Value)
                                {
                                    resultado.ReceiveDate = fecha;
                                }
                            }
                        }
                    }
                }
            }

            resultado.VoucherNo = resultado.VoucherNo.Distinct().ToList();

            return resultado;
        }
        public string ObtenerVoucherNoConcatenados(List<int> poIds)
        {
            var vouchers = ObtenerVoucherNoPorPOIds(poIds);

            return string.Join(",", vouchers);
        }

    }
}
