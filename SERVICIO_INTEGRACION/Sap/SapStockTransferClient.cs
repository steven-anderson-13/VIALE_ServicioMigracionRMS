using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VIALE_ServicioMigracionRMS.Models;

namespace VIALE_ServicioMigracionRMS.Sap
{
    public class SapStockTransferClient : IDisposable
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
        public StockTransferResult CrearTransferencia(StockTransferData entrada, System.Collections.Generic.List<StockTransferLineData> lineas)
        {
            Connect();

            StockTransfer transfer = null;

            try
            {
                transfer = (StockTransfer)_company.GetBusinessObject(BoObjectTypes.oStockTransfer);

                // Fecha actual del servidor donde se ejecuta el servicio.
                DateTime fechaActual = entrada.DocDate;

                // Primer día del mes actual.
                DateTime fechaDesde = new DateTime(fechaActual.Year, fechaActual.Month, 1);

                // Último día del mes actual.
                DateTime fechaHasta = fechaDesde.AddMonths(1).AddDays(-1);


                transfer.DocDate = entrada.DocDate;
                transfer.TaxDate = entrada.DocDate;
                //transfer.DueDate = DateTime.Today;


                // Calcular vencimiento según el mes del recibo RMS.
                if (entrada.DocDate.Year == fechaActual.Year && entrada.DocDate.Month == fechaActual.Month)
                {
                    // Si el recibo pertenece al mes actual: último día del mes.
                    transfer.DueDate = fechaHasta;
                }
                else
                {
                    // Si el recibo pertenece a otro mes: día 15 del mes actual.
                    transfer.DueDate = new DateTime(fechaActual.Year, fechaActual.Month, 15);
                }

                //transfer.DueDate = entrada.d;
                transfer.CardCode = entrada.CardCode;
                transfer.CardName = entrada.CardName;
                transfer.FromWarehouse = "IM01"; //origen
                transfer.ToWarehouse = "1017";   //destino
                transfer.Comments = $"Transferencia importación EM {entrada.DocNum} {DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")}";
                transfer.JournalMemo = $"Transferencia importación EM {entrada.DocNum}";
                SetUdf(transfer.UserFields, "U_BPV_NCON2", "000");
                SetUdf(transfer.UserFields, "U_BPP_MDTD", "");
                SetUdf(transfer.UserFields, "U_BPP_MDSD", "000");
                SetUdf(transfer.UserFields, "U_BPP_MDCD", "000");

                SetUdf(transfer.UserFields, "U_BPP_MDMT", "01");
                SetUdf(transfer.UserFields, "U_BPP_MDTS", "TSE");
                SetUdf(transfer.UserFields, "U_VS_TIPOPER", "01");

                foreach (var linea in lineas)
                {  
                    transfer.Lines.BaseType = InvBaseDocTypeEnum.PurchaseDeliveryNotes;
                    transfer.Lines.BaseEntry = entrada.DocEntry;
                    transfer.Lines.BaseLine = linea.LineNum;

                    transfer.Lines.ItemCode = linea.ItemCode;
                    transfer.Lines.Quantity = Convert.ToDouble(linea.Quantity);
                    transfer.Lines.FromWarehouseCode = "IM01";
                    transfer.Lines.WarehouseCode = "1017";
                    transfer.Lines.UserFields.Fields.Item("U_BPP_OPER").Value = "A";
                    transfer.Lines.UserFields.Fields.Item("U_tipoOpT12").Value = "99";
                    transfer.Lines.UserFields.Fields.Item("U_VS_T12IN").Value = "99";
                    transfer.Lines.Add();
                }

                int result = transfer.Add();
                if (result != 0)
                {
                    _company.GetLastError(out int errorCode, out string errorMessage);

                    throw new Exception(errorCode + " - " + errorMessage);
                }

                // Obtener DocEntry generado
                int docEntry = Convert.ToInt32(_company.GetNewObjectKey());

                // Obtener DocNum generado
                int docNum = ObtenerDocNumTransferencia(docEntry);

                return new StockTransferResult
                {
                    DocEntry = docEntry,
                    DocNum = docNum
                };
            }
            finally
            {
                if (transfer != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(transfer);

                if (_company != null)
                {
                    if (_company.Connected)
                        _company.Disconnect();

                    Marshal.ReleaseComObject(_company);
                    _company = null;
                }
            }
        }
        private static void SetUdf(UserFields fields, string name, object value)
        {
            if (value != null)
                fields.Fields.Item(name).Value = value;
        }
        private int ObtenerDocNumTransferencia(int docEntry)
        {
            Recordset rs = null;

            try
            {
                rs = (Recordset)_company.GetBusinessObject(
                    BoObjectTypes.BoRecordset);

                rs.DoQuery($@"
            SELECT ""DocNum""
            FROM OWTR
            WHERE ""DocEntry"" = {docEntry}");

                if (rs.EoF)
                    throw new Exception(
                        $"No se encontró DocNum para OWTR DocEntry {docEntry}.");

                return Convert.ToInt32(
                    rs.Fields.Item("DocNum").Value);
            }
            finally
            {
                if (rs != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
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
    }
}
