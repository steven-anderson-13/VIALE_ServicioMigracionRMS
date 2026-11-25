using SAPbobsCOM;
using SERVICIO_INTEGRACION.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SERVICIO_INTEGRACION.Sap
{
    public class SapGoodsReceiptClient : IDisposable
    {
        private Company _company;

        //public SapGoodsReceiptClient(Company company)
        //{
        //    _company = company ??
        //        throw new ArgumentNullException(nameof(company));
        //}


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

        public SapGoodsReceiptResult CreateGoodsReceipt(VoucherHeader header, List<VoucherLine> lines)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));

            if (lines == null || lines.Count == 0)
                throw new InvalidOperationException(
                    "El VOUCHER no tiene líneas para registrar.");

            Connect();

            Documents document = null;

            try
            {
                document = (Documents)_company.GetBusinessObject(BoObjectTypes.oPurchaseDeliveryNotes);

                // Cabecera
                document.DocDate = header.DocDate;
                document.DocDueDate = header.DocDueDate;
                document.CardCode = header.CardCode;
                document.JournalMemo = header.JournalMemo;
                document.Comments = header.Comments;
                document.NumAtCard = header.NumAtCard;

                if (!string.IsNullOrWhiteSpace(header.DocCurrency))
                    document.DocCurrency = header.DocCurrency;

                // UDF de cabecera
                SetHeaderUdf(document, "U_VS_TIPOPER", header.U_VS_TIPOPER);
                SetHeaderUdf(document, "U_VS_TIPOOC", header.U_VS_TIPOOC);
                SetHeaderUdf(document, "U_BPP_MDTD", header.U_BPP_MDTD);
                SetHeaderUdf(document, "U_BPP_MDSD", header.U_BPP_MDSD);
                SetHeaderUdf(document, "U_BPP_MDCD", header.U_BPP_MDCD);
                SetHeaderUdf(document, "U_BPV_SERI", header.U_BPV_SERI);
                SetHeaderUdf(document, "U_BPV_NCON2", header.U_BPV_NCON2);
                SetHeaderUdf(document, "U_VS_NATNUM", header.U_VS_NATNUM);

                // Detalle
                for (int i = 0; i < lines.Count; i++)
                {
                    VoucherLine line = lines[i];

                    // Buscar en SAP la línea correspondiente de la OC.
                    ObtenerReferenciaOrdenCompra(header, line);

                    if (line.BaseEntry == null || line.BaseLine == null)
                    {
                        throw new InvalidOperationException(
                            "La línea del artículo " + line.ItemCode +
                            " no tiene BaseEntry/BaseLine. " +
                            "No se puede vincular de forma segura " +
                            "con la orden de compra.");
                    }

                    if (line.BaseType != 22)
                    {
                        throw new InvalidOperationException(
                            "La línea del artículo " + line.ItemCode +
                            " debe tener BaseType = 22 para vincularse " +
                            "a una orden de compra.");
                    }

                    if (i > 0)
                        document.Lines.Add();

                    // Relacionar la línea de la entrada con la OC.
                    document.Lines.BaseType = 22;
                    document.Lines.BaseEntry = line.BaseEntry.Value;
                    document.Lines.BaseLine = line.BaseLine.Value;
                    document.Lines.Quantity = line.Quantity;

                    if (!string.IsNullOrWhiteSpace(line.WarehouseCode))
                        document.Lines.WarehouseCode = line.WarehouseCode;

                    if (!string.IsNullOrWhiteSpace(line.TaxCode))
                        document.Lines.TaxCode = line.TaxCode;

                    SetLineUdf(document.Lines, "U_BPP_OPER", line.U_BPP_OPER);
                    SetLineUdf(document.Lines, "U_tipoOpT12", line.U_tipoOpT12);
                }

                int result = document.Add();

                if (result != 0)
                {
                    int errorCode;
                    string errorMessage;

                    _company.GetLastError(out errorCode, out errorMessage);

                    if (errorMessage.IndexOf("No se puede crear la EM si ya venció la Orden de Compra base.", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Obtener el DocEntry de la OC base.
                        int baseEntry = ObtenerBaseEntry(document);

                        // Actualizar el vencimiento al fin de mes actual.
                        ActualizarVencimientoOrdenCompra(baseEntry);

                        // Reintentar la creación de la EM una sola vez.
                        result = document.Add();

                        if (result != 0)
                        {
                            _company.GetLastError(
                                out errorCode,
                                out errorMessage);

                            throw new InvalidOperationException(
                                "No se pudo crear la EM después de actualizar " +
                                "la fecha de vencimiento de la OC. SAP: " +
                                errorCode + " - " + errorMessage);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException(errorCode + " - " + errorMessage);
                    }
                    //throw new InvalidOperationException( errorCode +" - " + errorMessage);
                }

                string newObjectCode;
                _company.GetNewObjectCode(out newObjectCode);

                int docEntry;

                if (!int.TryParse(newObjectCode, out docEntry))
                {
                    int errorCode;
                    string errorMessage;

                    _company.GetLastError(out errorCode, out errorMessage);

                    throw new InvalidOperationException(
                        "La entrada se creó, pero no se pudo recuperar " +
                        "su DocEntry. Verifica el documento en SAP. " +
                        errorMessage);
                }

                //int docEntry;

                if (!int.TryParse(newObjectCode, out docEntry))
                {
                    throw new InvalidOperationException(
                        "SAP creó el documento, pero el DocEntry recibido " +
                        "no es válido. Verifica el documento antes de reintentar.");
                }

                Documents savedDocument = null;

                try
                {
                    savedDocument = (Documents)_company.GetBusinessObject(BoObjectTypes.oPurchaseDeliveryNotes);

                    if (!savedDocument.GetByKey(docEntry))
                    {
                        throw new InvalidOperationException(
                            "La entrada fue creada, pero no se pudo " +
                            "consultar su número en SAP. DocEntry: " +
                            docEntry);
                    }

                    return new SapGoodsReceiptResult
                    {
                        DocEntry = docEntry,
                        DocNum = savedDocument.DocNum
                    };
                }
                finally
                {
                    if (savedDocument != null)
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(savedDocument);
                }
            }
            finally
            {
                if (document != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(document);
            }
        }

        private static void SetHeaderUdf(Documents document, string field, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            document.UserFields.Fields.Item(field).Value = value;
        }

        private static void SetLineUdf(Document_Lines lines, string field, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            lines.UserFields.Fields.Item(field).Value = value;
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

        private void ObtenerReferenciaOrdenCompra(VoucherHeader header, VoucherLine line)
        {
            SAPbobsCOM.Recordset recordset = null;

            try
            {
                int numeroOC;

                if (!int.TryParse(header.PONo, out numeroOC))
                {
                    throw new InvalidOperationException("El número de OC no es válido: " + header.PONo);
                }

                string itemCodeSQL = line.ItemCode.Replace("'", "''");

                string sql =
                    "SELECT T0.\"DocEntry\", T1.\"LineNum\" " +
                    "FROM OPOR T0 " +
                    "INNER JOIN POR1 T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" " +
                    "WHERE T0.\"Comments\" = '" + numeroOC + "' " +
                    "AND T0.\"CANCELED\" <> 'Y' " +
                    "AND T1.\"ItemCode\" = '" + itemCodeSQL + "' " +
                    "AND T1.\"LineStatus\" = 'O' " +
                    "AND T1.\"OpenQty\" > 0";

                recordset = (SAPbobsCOM.Recordset)_company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

                recordset.DoQuery(sql);

                if (recordset.EoF)
                {
                    throw new InvalidOperationException(
                        "No se encontró una línea abierta para el artículo " +
                        line.ItemCode +
                        " en la orden de compra " + header.PONo);
                }

                int docEntry = Convert.ToInt32(recordset.Fields.Item("DocEntry").Value);
                int lineNum = Convert.ToInt32(recordset.Fields.Item("LineNum").Value);

                recordset.MoveNext();

                if (!recordset.EoF)
                {
                    throw new InvalidOperationException(
                        "Se encontraron varias líneas abiertas para el artículo " +
                        line.ItemCode +
                        " en la OC " + header.PONo +
                        ". No es posible determinar la línea correcta " +
                        "usando únicamente el ItemCode.");
                }

                line.BaseType = 22;
                line.BaseEntry = docEntry;
                line.BaseLine = lineNum;
            }
            finally
            {
                if (recordset != null)
                {
                    System.Runtime.InteropServices.Marshal
                        .ReleaseComObject(recordset);
                }
            }
        }
        private int ObtenerBaseEntry(SAPbobsCOM.Documents document)
        {
            int baseEntry = -1;

            for (int i = 0; i < document.Lines.Count; i++)
            {
                document.Lines.SetCurrentLine(i);

                if (document.Lines.BaseType != 22)
                {
                    continue;
                }

                int entryActual = document.Lines.BaseEntry;

                if (baseEntry == -1)
                {
                    baseEntry = entryActual;
                }
                else if (baseEntry != entryActual)
                {
                    throw new InvalidOperationException(
                        "La EM contiene líneas asociadas a diferentes OC. " +
                        "No se puede determinar una única OC para actualizar.");
                }
            }

            if (baseEntry == -1)
            {
                throw new InvalidOperationException(
                    "No se encontró el DocEntry de la OC base.");
            }

            return baseEntry;
        }
        private void ActualizarVencimientoOrdenCompra(int docEntry)
        {
            SAPbobsCOM.Documents ordenCompra = null;

            try
            {
                ordenCompra = (SAPbobsCOM.Documents)_company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);

                if (!ordenCompra.GetByKey(docEntry))
                {
                    throw new InvalidOperationException(
                        "No se encontró la OC en SAP. DocEntry: " +
                        docEntry);
                }

                // Último día del mes actual.
                DateTime finMes = new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    DateTime.DaysInMonth(
                        DateTime.Today.Year,
                        DateTime.Today.Month));

                ordenCompra.DocDueDate = finMes;

                int resultado = ordenCompra.Update();

                if (resultado != 0)
                {
                    int errorCode;
                    string errorMessage;

                    _company.GetLastError(out errorCode, out errorMessage);

                    throw new InvalidOperationException("No se pudo actualizar el vencimiento de la OC " +
                        docEntry + ". SAP: " +
                        errorCode + " - " + errorMessage);
                }
            }
            finally
            {
                if (ordenCompra != null)
                {
                    System.Runtime.InteropServices.Marshal
                        .ReleaseComObject(ordenCompra);
                }
            }
        }

    }
}
