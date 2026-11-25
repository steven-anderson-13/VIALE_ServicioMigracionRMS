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

namespace VIALE_ServicioMigracionRMS.Sap
{
    public class SapGoodsReceiptReserveInvoiceClient : IDisposable
    {
        private Company _company;
        public void Connect()
        {
            if (_company != null && _company.Connected)
                return;

            _company = new Company();
            _company.Server = ConfigurationManager.AppSettings["Sap.Server"];
            _company.CompanyDB = ConfigurationManager.AppSettings["Sap.CompanyDB"];
            _company.UserName = ConfigurationManager.AppSettings["Sap.UserName"];
            _company.Password = ConfigurationManager.AppSettings["Sap.Password"];
            _company.DbUserName = ConfigurationManager.AppSettings["Sap.DbUserName"];
            _company.DbPassword = ConfigurationManager.AppSettings["Sap.DbPassword"];
            _company.LicenseServer = ConfigurationManager.AppSettings["Sap.LicenseServer"];
            _company.DbServerType = BoDataServerTypes.dst_HANADB;
            _company.language = BoSuppLangs.ln_Spanish;

            int result = _company.Connect();

            if (result != 0)
            {
                _company.GetLastError(out int errorCode, out string errorMessage);
                Marshal.ReleaseComObject(_company);
                _company = null;
                throw new Exception("Error conectando a SAP: " + errorCode + " - " + errorMessage);
            }
        }
        public GoodsReceiptReserveInvoiceResult CrearEntradaMercancias(GoodsReceiptReserveInvoiceData factura, List<GoodsReceiptReserveInvoiceLineData> detalle, string voucherNos, DateTime fechaRecibo)
        {
            Connect();

            Documents documento = null;

            try
            {
                documento = (Documents)_company.GetBusinessObject(BoObjectTypes.oPurchaseDeliveryNotes);

                // Fecha actual del servidor donde se ejecuta el servicio.
                DateTime fechaActual = DateTime.Today;

                // Primer día del mes actual.
                DateTime fechaDesde = new DateTime(fechaActual.Year, fechaActual.Month, 1);

                // Último día del mes actual.
                DateTime fechaHasta = fechaDesde.AddMonths(1).AddDays(-1);

                // Fecha del recibo RMS más antiguo.
                documento.DocDate = fechaRecibo;


                // Calcular vencimiento según el mes del recibo RMS.
                if (fechaRecibo.Year == fechaActual.Year && fechaRecibo.Month == fechaActual.Month)
                {
                    // Si el recibo pertenece al mes actual: último día del mes.
                    documento.DocDueDate = fechaHasta;
                }
                else
                {
                    // Si el recibo pertenece a otro mes: día 15 del mes actual.
                    documento.DocDueDate = new DateTime(fechaActual.Year, fechaActual.Month, 15);
                }

                // Fecha tributaria de la Factura de Reserva SAP
                documento.TaxDate = factura.DocDate;

                documento.CardCode = factura.CardCode;
                documento.Comments = $"Recibo de RMS {voucherNos} {DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")}";
                documento.JournalMemo = factura.CardName;

                // Referencia de documento tributario
                documento.UserFields.Fields.Item("U_BPP_MDTD").Value = factura.U_BPP_MDTD;
                documento.UserFields.Fields.Item("U_BPP_MDSD").Value = factura.U_BPP_MDSD;
                documento.UserFields.Fields.Item("U_BPP_MDCD").Value = factura.U_BPP_MDCD;

                documento.UserFields.Fields.Item("U_VS_NATNUM").Value = "T";

                documento.UserFields.Fields.Item("U_BPV_SERI").Value = "";
                documento.UserFields.Fields.Item("U_BPV_NCON2").Value = "";

                documento.UserFields.Fields.Item("U_VS_TIPOPER").Value = "01";

                foreach (var linea in detalle)
                {
                    // OPCH = 18
                    documento.Lines.BaseType = (int)BoObjectTypes.oPurchaseInvoices;
                    documento.Lines.BaseEntry = factura.DocEntry;
                    documento.Lines.BaseLine = linea.LineNum;

                    documento.Lines.ItemCode = linea.ItemCode;
                    documento.Lines.Quantity = Convert.ToDouble(linea.Quantity);
                    documento.Lines.WarehouseCode = linea.WhsCode;

                    documento.Lines.UserFields.Fields.Item("U_BPP_OPER").Value = "I"; //Inafecta
                    documento.Lines.UserFields.Fields.Item("U_tipoOpT12").Value = "02";
                    documento.Lines.UserFields.Fields.Item("U_VS_T12IN").Value = "";

                    // Centro de costo
                    if (!string.IsNullOrWhiteSpace(linea.OcrCode))
                        documento.Lines.CostingCode = linea.OcrCode;

                    if (!string.IsNullOrWhiteSpace(linea.OcrCode2))
                        documento.Lines.CostingCode2 = linea.OcrCode2;

                    if (!string.IsNullOrWhiteSpace(linea.OcrCode3))
                        documento.Lines.CostingCode3 = linea.OcrCode3;

                    if (!string.IsNullOrWhiteSpace(linea.OcrCode4))
                        documento.Lines.CostingCode4 = linea.OcrCode4;

                    if (!string.IsNullOrWhiteSpace(linea.OcrCode5))
                        documento.Lines.CostingCode5 = linea.OcrCode5;

                    documento.Lines.Add();
                }

                int resultado = documento.Add();
                if (resultado != 0)
                {
                    _company.GetLastError(out int errorCode, out string errorMessage);
                    throw new Exception(errorCode + " - " + errorMessage);
                }

                int docEntry = Convert.ToInt32(_company.GetNewObjectKey());

                int docNum = ObtenerDocNumEntrada(docEntry);

                return new GoodsReceiptReserveInvoiceResult
                {
                    DocEntry = docEntry,
                    DocNum = docNum
                };
            }
            finally
            {
                if (documento != null)
                    Marshal.ReleaseComObject(documento);

                if (_company != null)
                {
                    if (_company.Connected)
                        _company.Disconnect();

                    Marshal.ReleaseComObject(_company);
                    _company = null;
                }
            }
        }

        private int ObtenerDocNumEntrada(int docEntry)
        {
            Recordset rs = null;

            try
            {
                rs = (Recordset)_company.GetBusinessObject(
                    BoObjectTypes.BoRecordset);

                rs.DoQuery($@"
                SELECT ""DocNum""
                FROM OPDN
                WHERE ""DocEntry"" = {docEntry}");

                if (rs.EoF)
                    throw new Exception(
                        $"No se encontró DocNum para OPDN DocEntry {docEntry}.");

                return Convert.ToInt32(
                    rs.Fields.Item("DocNum").Value);
            }
            finally
            {
                if (rs != null)
                    Marshal.ReleaseComObject(rs);
            }
        }
        public void Dispose()
        {
            if (_company != null)
            {
                if (_company.Connected)
                    _company.Disconnect();

                Marshal.ReleaseComObject(_company);
                _company = null;
            }
        }

        // --------------------------------CAMBIO DE CONEXION A ODBC-----------------------------
        public List<int> ObtenerRmsPOIdsPorFacturaReserva(int docEntryOPCH)
        {
            var lista = new List<int>();

            using (OdbcConnection conn = SAPCONN.GetConnection())
            {
                using (OdbcCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT DISTINCT
                    T2.""Comments"" AS ""RMS_OC""
                FROM ""SBO_VIALE_PRODUCCION"".""PCH1"" T1
                INNER JOIN ""SBO_VIALE_PRODUCCION"".""OPOR"" T2
                    ON T2.""DocEntry"" = T1.""BaseEntry""
                WHERE
                    T1.""DocEntry"" = ?
                    AND T1.""BaseType"" = 22
                    AND T2.""Comments"" IS NOT NULL
                    AND TRIM(T2.""Comments"") <> ''";

                    cmd.CommandType = CommandType.Text;

                    cmd.Parameters.Add("DocEntryOPCH", OdbcType.Int).Value = docEntryOPCH;

                    using (OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string rmsOC = Convert.ToString(reader["RMS_OC"]);

                            if (int.TryParse(rmsOC, out int poId))
                            {
                                lista.Add(poId);
                            }
                        }
                    }
                }
            }

            return lista.Distinct().ToList();
        }
    }
}
