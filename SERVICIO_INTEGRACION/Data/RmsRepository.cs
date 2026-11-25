using SERVICIO_INTEGRACION.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SERVICIO_INTEGRACION.Data
{
    public class RmsRepository
    {
        private readonly string _connectionString;

        public RmsRepository()
        {
            _connectionString = ConfigurationManager.AppSettings["Rms.ConnectionString"];
        }

        public List<PurchaseOrderHeader> GetHeaders(DateTime dateFrom, DateTime dateTo)
        {
            var result = new List<PurchaseOrderHeader>();

            using (var cn = new SqlConnection(_connectionString))
            //using (var cn = BDComun.ObtnerConexion("biotime"))
            using (var cmd = new SqlCommand("dbo.SP_RMS_ObtenerOrdenesCompra", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@FechaDesde", SqlDbType.Date).Value = dateFrom.Date;
                cmd.Parameters.Add("@FechaHasta", SqlDbType.Date).Value = dateTo.Date;

                cn.Open();

                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        result.Add(new PurchaseOrderHeader
                        {
                            POId = rd.GetGuid(rd.GetOrdinal("POId")),
                            DocType = ReadString(rd, "DocType"),
                            DocDate = ReadString(rd, "DocDate"),
                            DocDueDate = ReadString(rd, "DocDueDate"),
                            CardCode = ReadString(rd, "CardCode"),
                            CardName = ReadString(rd, "CardName"),
                            DocCurrency = ReadString(rd, "DocCurrency"),
                            U_VS_TIPOPER = ReadString(rd, "U_VS_TIPOPER"),
                            U_VS_TIPOOC = ReadString(rd, "U_VS_TIPOOC"),
                            Comments = ReadString(rd, "Comments"),
                            JournalMemo = ReadString(rd, "JournalMemo"),
                            StatusCode = ReadString(rd, "StatusCode")
                        });
                    }
                }

                cn.Close();
            }

            return result;
        }

        public List<PurchaseOrderHeader> GetHeaders_ME(DateTime dateFrom, DateTime dateTo)
        {
            var result = new List<PurchaseOrderHeader>();

            using (var cn = new SqlConnection(_connectionString))
            //using (var cn = BDComun.ObtnerConexion("biotime"))
            using (var cmd = new SqlCommand("dbo.SP_RMS_ObtenerOrdenesCompra_ME", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@FechaDesde", SqlDbType.Date).Value = dateFrom.Date;
                cmd.Parameters.Add("@FechaHasta", SqlDbType.Date).Value = dateTo.Date;

                cn.Open();

                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        result.Add(new PurchaseOrderHeader
                        {
                            POId = rd.GetGuid(rd.GetOrdinal("POId")),
                            DocType = ReadString(rd, "DocType"),
                            DocDate = ReadString(rd, "DocDate"),
                            DocDueDate = ReadString(rd, "DocDueDate"),
                            CardCode = ReadString(rd, "CardCode"),
                            CardName = ReadString(rd, "CardName"),
                            DocCurrency = ReadString(rd, "DocCurrency"),
                            U_VS_TIPOPER = ReadString(rd, "U_VS_TIPOPER"),
                            U_VS_TIPOOC = ReadString(rd, "U_VS_TIPOOC"),
                            Comments = ReadString(rd, "Comments"),
                            JournalMemo = ReadString(rd, "JournalMemo"),
                            StatusCode = ReadString(rd, "StatusCode")
                        });
                    }
                }

                cn.Close();
            }

            return result;
        }

        public List<PurchaseOrderLine> GetLines(Guid poId)
        {
            var result = new List<PurchaseOrderLine>();

            using (var cn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.SP_RMS_ObtenerDetalleOrdenCompra", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@POId", SqlDbType.UniqueIdentifier).Value = poId;

                cn.Open();

                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        result.Add(new PurchaseOrderLine
                        {
                            LineNum = Convert.ToInt32(rd["LineNum"]),
                            ItemCode = ReadString(rd, "ItemCode"),
                            ItemDescription = ReadString(rd, "ItemDescription"),
                            Quantity = Convert.ToDecimal(rd["Quantity"]),
                            Price = Convert.ToDecimal(rd["Price"]),
                            WarehouseCode = ReadString(rd, "WarehouseCode"),
                            TaxCode = ReadString(rd, "TaxCode"),
                            U_BPP_OPER = ReadString(rd, "U_BPP_OPER"),
                            U_tipoOpT12 = ReadString(rd, "U_tipoOpT12"),
                            SKU = ReadString(rd, "SKU")
                        });
                    }
                }

                cn.Close();
            }

            return result;
        }

        public void SaveResult(Guid poId, string status, int? docEntry, int? docNum, string error)
        {
            using (var cn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.SP_RMS_RegistrarResultadoOC", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@POId", SqlDbType.UniqueIdentifier).Value = poId;
                cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 20).Value = status;
                cmd.Parameters.Add("@DocEntrySAP", SqlDbType.Int).Value = (object)docEntry ?? DBNull.Value;
                cmd.Parameters.Add("@DocNumSAP", SqlDbType.Int).Value = (object)docNum ?? DBNull.Value;
                cmd.Parameters.Add("@MensajeError", SqlDbType.NVarChar, -1).Value = (object)error ?? DBNull.Value;

                cn.Open();
                cmd.ExecuteNonQuery();
                cn.Close();
            }
        }

        private static string ReadString(IDataRecord reader, string column)
        {
            int index = reader.GetOrdinal(column);

            return reader.IsDBNull(index)
                ? null
                : Convert.ToString(reader.GetValue(index));
        }
    }
}
