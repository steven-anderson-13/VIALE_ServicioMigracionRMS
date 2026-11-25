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
    public class SapGoodsReceiptReserveInvoiceRepository
    {
        // ----------------------- CONEXION A ODBC -----------------------------
        public List<GoodsReceiptReserveInvoiceData> ObtenerFacturasReservaImportacion(DateTime fechaDesde, DateTime fechaHasta)
        {
            var lista = new List<GoodsReceiptReserveInvoiceData>();

            using (OdbcConnection conn = SAPCONN.GetConnection())
            {
                using (OdbcCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"CALL ""SBO_VIALE_PRODUCCION"".""SP_VIA_ObtenerFacturasReservaImportacion"" (?, ?)";

                    cmd.CommandType = CommandType.Text;

                    // Los parámetros ODBC se envían por posición.
                    cmd.Parameters.Add("FechaDesde", OdbcType.Date).Value = fechaDesde.Date;
                    cmd.Parameters.Add("FechaHasta", OdbcType.Date).Value = fechaHasta.Date;

                    using (OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new GoodsReceiptReserveInvoiceData
                            {
                                DocEntry = Convert.ToInt32(reader["DocEntry"]),
                                DocNum = Convert.ToInt32(reader["DocNum"]),
                                DocDate = Convert.ToDateTime(reader["TaxDate"]),
                                DocDueDate = Convert.ToDateTime(reader["DocDueDate"]),
                                CardCode = Convert.ToString(reader["CardCode"]),
                                CardName = Convert.ToString(reader["CardName"]),
                                DocCur = Convert.ToString(reader["DocCur"]),
                                DocTotal = Convert.ToDecimal(reader["DocTotal"]),
                                U_BPP_MDTD = Convert.ToString(reader["U_BPP_MDTD"]),
                                U_BPP_MDSD = Convert.ToString(reader["U_BPP_MDSD"]),
                                U_BPP_MDCD = Convert.ToString(reader["U_BPP_MDCD"]),
                                Comments = Convert.ToString(reader["Comments"]),
                                JournalMemo = Convert.ToString(reader["JrnlMemo"])
                            });
                        }
                    }
                }
            }

            return lista;
        }
        public List<GoodsReceiptReserveInvoiceLineData> ObtenerDetalleFacturaReservaImportacion(int docEntry)
        {
            var lista = new List<GoodsReceiptReserveInvoiceLineData>();

            using (OdbcConnection conn = SAPCONN.GetConnection())
            {
                using (OdbcCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"CALL ""SBO_VIALE_PRODUCCION"".""SP_VIA_ObtenerDetalleFacturaReservaImportacion"" (?)";
                    cmd.CommandType = CommandType.Text;

                    // Los parámetros ODBC se envían por posición.
                    cmd.Parameters.Add("docEntry", OdbcType.Int).Value = docEntry;
                    using (OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new GoodsReceiptReserveInvoiceLineData
                            {
                                DocEntry = Convert.ToInt32(reader["DocEntry"]),
                                LineNum = Convert.ToInt32(reader["LineNum"]),
                                ItemCode = Convert.ToString(reader["ItemCode"]),
                                Dscription = Convert.ToString(reader["Dscription"]),
                                Quantity = Convert.ToDecimal(reader["Quantity"]),
                                WhsCode = Convert.ToString(reader["WhsCode"]),
                                UomCode = Convert.ToString(reader["UomCode"]),
                                Price = Convert.ToDecimal(reader["Price"]),
                                Currency = Convert.ToString(reader["Currency"]),
                                TaxCode = Convert.ToString(reader["TaxCode"]),
                                DiscPrcnt = Convert.ToDecimal(reader["DiscPrcnt"]),
                                AcctCode = Convert.ToString(reader["AcctCode"]),
                                OcrCode = Convert.ToString(reader["OcrCode"]),
                                OcrCode2 = Convert.ToString(reader["OcrCode2"]),
                                OcrCode3 = Convert.ToString(reader["OcrCode3"]),
                                OcrCode4 = Convert.ToString(reader["OcrCode4"]),
                                OcrCode5 = Convert.ToString(reader["OcrCode5"])
                            });
                        }
                    }
                }
            }
            return lista;
        }

    }
}
