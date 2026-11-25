using SAPbobsCOM;
using SERVICIO_INTEGRACION.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SERVICIO_INTEGRACION.Sap
{
    public class SapDiApiClient : IDisposable
    {
        private Company _company;
        private SapItemClient _sapItemClient;

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


        public SapDocumentResult CreatePurchaseOrder(PurchaseOrderHeader header, System.Collections.Generic.IList<PurchaseOrderLine> lines, DateTime fechaDesde, DateTime fechaHasta)
        {
            if (lines == null || lines.Count == 0)
                throw new Exception("La orden no tiene detalle.");

            Connect();

            // Compartir conexión SAP existente
            if (_sapItemClient == null)
                _sapItemClient = new SapItemClient(_company);

            Documents document = null;

            try
            {
                document = (Documents)_company.GetBusinessObject(BoObjectTypes.oPurchaseOrders);
                document.DocType = BoDocumentTypes.dDocument_Items;
                document.CardCode = header.CardCode;
                document.CardName = header.CardName;
                //document.DocDate = ParseDate(header.DocDate);
                //document.DocDueDate = ParseDate(header.DocDueDate);

                // Fecha del documento: primer día del mes actual.
                document.DocDate = fechaDesde;

                // Fecha de vencimiento: último día del mes actual.
                document.DocDueDate = fechaHasta;

                document.DocCurrency = header.DocCurrency;
                document.Comments = header.Comments ?? "";
                document.JournalMemo = header.JournalMemo ?? "";
                SetUdf(document.UserFields, "U_VS_TIPOPER", header.U_VS_TIPOPER);
                SetUdf(document.UserFields, "U_VS_TIPOOC", header.U_VS_TIPOOC);

                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];

                    if (i > 0)
                        document.Lines.Add();

                    string itemCodeSAP = ObtenerItemCodePorSKU(line.SKU);
                    if (string.IsNullOrWhiteSpace(itemCodeSAP))
                    {
                        // Crear artículo padre e hijos faltantes desde RMS
                        _sapItemClient.ObtenerOCrearArticulo(line.SKU);

                        // Volver a consultar el ItemCode del SKU en SAP
                        itemCodeSAP = ObtenerItemCodePorSKU(line.SKU);
                    }

                    // Validar que el artículo exista antes de asignarlo
                    if (string.IsNullOrWhiteSpace(itemCodeSAP))
                    {
                        throw new Exception($"No se pudo obtener o crear el artículo SKU: {line.SKU}");
                    }

                    //document.Lines.ItemCode = line.ItemCode;
                    document.Lines.ItemCode = itemCodeSAP;
                    document.Lines.Quantity = (double)line.Quantity;
                    document.Lines.Price = (double)line.Price;
                    document.Lines.WarehouseCode = line.WarehouseCode;
                    document.Lines.TaxCode = line.TaxCode;
                    SetUdf(document.Lines.UserFields, "U_BPP_OPER", line.U_BPP_OPER);
                    SetUdf(document.Lines.UserFields, "U_tipoOpT12", line.U_tipoOpT12);
                }

                int result = document.Add();

                if (result != 0)
                {
                    _company.GetLastError(out int errorCode, out string errorMessage);
                    //throw new Exception("SAP rechazó la orden " + header.POId + " : " + errorCode + " - " + errorMessage);
                    throw new Exception(errorCode + " - " + errorMessage);
                }

                _company.GetNewObjectCode(out string newKey);
                int docEntry = int.Parse(newKey);
                Documents saved = null;

                try
                {
                    saved = (Documents)_company.GetBusinessObject(BoObjectTypes.oPurchaseOrders);

                    if (!saved.GetByKey(docEntry))
                        throw new Exception("Se creó la orden, pero no se pudo recuperar DocNum.");

                    return new SapDocumentResult
                    {
                        DocEntry = docEntry,
                        DocNum = saved.DocNum
                    };
                }
                finally
                {
                    if (saved != null)
                        Marshal.ReleaseComObject(saved);
                }
            }
            finally
            {
                if (document != null)
                    Marshal.ReleaseComObject(document);
            }
        }

        private static DateTime ParseDate(string value)
        {
            return DateTime.ParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture);
        }

        private static void SetUdf(UserFields fields, string name, object value)
        {
            if (value != null)
                fields.Fields.Item(name).Value = value;
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

        private string ObtenerItemCodePorSKU(string sku)
        {
            SAPbobsCOM.Recordset rs = null;

            try
            {
                rs = (SAPbobsCOM.Recordset)_company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

                string skuSeguro = sku.Replace("'", "''");

                string sql = @"
            SELECT ""ItemCode""
            FROM ""OITM""
            WHERE ""CodeBars"" = '" + skuSeguro + @"'
            AND ""InvntItem"" = 'Y'";

                rs.DoQuery(sql);

                if (!rs.EoF)
                {
                    return rs.Fields.Item("ItemCode").Value.ToString();
                }

                return null;
            }
            finally
            {
                if (rs != null)
                {
                    System.Runtime.InteropServices.Marshal
                        .ReleaseComObject(rs);
                }
            }
        }
    }
}
