using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VIALE_ServicioMigracionRMS.Models;

namespace VIALE_ServicioMigracionRMS.Repositories
{
    public class SapStockTransferRepository
    {
        // ----------------------- CONEXION A ODBC -----------------------------
        public List<StockTransferData> ObtenerEntradasImportacionCosteadas(DateTime fechaDesde, DateTime fechaHasta)
        {
            var resultado = new List<StockTransferData>();

            using (OdbcConnection conn = SAPCONN.GetConnection())
            {
                using (OdbcCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"CALL ""SBO_VIALE_PRODUCCION"".""SP_VIA_ObtenerEntradasImportacionCosteadas"" (?, ?)";

                    cmd.CommandType = CommandType.Text;

                    // Los parámetros ODBC se envían por posición.
                    cmd.Parameters.Add("FechaDesde", OdbcType.Date).Value = fechaDesde.Date;
                    cmd.Parameters.Add("FechaHasta", OdbcType.Date).Value = fechaHasta.Date;

                    using (OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            resultado.Add(new StockTransferData
                            {
                                DocEntry = Convert.ToInt32(reader["DocEntry"]),
                                DocNum = Convert.ToInt32(reader["DocNum"]),
                                DocDate = Convert.ToDateTime(reader["DocDate"]),
                                CardCode = Convert.ToString(reader["CardCode"]),
                                CardName = Convert.ToString(reader["CardName"]),
                                DocCur = Convert.ToString(reader["DocCur"]),
                                DocTotal = Convert.ToDecimal(reader["DocTotal"])
                            });
                        }
                    }
                }
            }

            return resultado;
        }
        public List<StockTransferLineData> ObtenerDetalleEntrada(int docEntry)
        {
            var resultado = new List<StockTransferLineData>();
            using (OdbcConnection conn = SAPCONN.GetConnection())
            {
                using (OdbcCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"CALL ""SBO_VIALE_PRODUCCION"".""SP_VIA_ObtenerDetalleEntradaImportacion"" (?)";
                    cmd.CommandType = CommandType.Text;

                    // Los parámetros ODBC se envían por posición.
                    cmd.Parameters.Add("docEntry", OdbcType.Int).Value = docEntry;
                    using (OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            resultado.Add(new StockTransferLineData
                            {
                                DocEntry = Convert.ToInt32(reader["DocEntry"]),
                                LineNum = Convert.ToInt32(reader["LineNum"]),
                                ItemCode = Convert.ToString(reader["ItemCode"]),
                                Dscription = Convert.ToString(reader["Dscription"]),
                                Quantity = Convert.ToDecimal(reader["Quantity"]),
                                WhsCode = Convert.ToString(reader["WhsCode"]),
                                UomCode = Convert.ToString(reader["UomCode"])
                            });
                        }
                    }
                }
            }
            return resultado;
        }

    }
}
