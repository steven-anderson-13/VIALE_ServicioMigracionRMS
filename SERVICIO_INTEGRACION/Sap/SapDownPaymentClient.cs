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
    public class SapDownPaymentClient 
    {
        private readonly Company _company;
        public SapDownPaymentClient(Company company)
        {
            _company = company ?? throw new ArgumentNullException(nameof(company));

            if (!_company.Connected)
                throw new Exception("La conexión SAP no está activa.");
        }
        public int CrearFacturaAnticipo(RMSDownPaymentCustomerData cliente, RMSDownPaymentData documentoRMS, List<RMSDownPaymentLineData> detalle)
        {
            if (!_company.Connected)
                throw new Exception("SAP no está conectado.");

            Documents documento = null;

            try
            {
                if (cliente == null)
                    throw new Exception("No se recibió información del cliente.");

                if (documentoRMS == null)
                    throw new Exception("No se recibió información del anticipo.");

                if (detalle == null || detalle.Count == 0)
                    throw new Exception("El anticipo no tiene líneas de detalle.");

                documento = (Documents)_company.GetBusinessObject(BoObjectTypes.oDownPayments);
                documento.DownPaymentType = SAPbobsCOM.DownPaymentTypeEnum.dptInvoice;
                // CABECERA
                documento.CardCode = cliente.CardCode;
                documento.CardName = cliente.CompanyName;
                documento.DocDate = documentoRMS.DocDate;
                documento.TaxDate = documentoRMS.TaxDate;
                documento.DocDueDate = documentoRMS.DocDueDate;

                documento.DocCurrency = documentoRMS.DocCurrency;

                documento.NumAtCard = documentoRMS.NumAtCard;
                documento.Comments = $"Factura Anticipo de RMS {documentoRMS.ReceiptNo} {DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")}";
                documento.JournalMemo = documentoRMS.JrnlMemo;

                // CAMPOS SUNAT
                documento.UserFields.Fields.Item("U_BPP_MDTD").Value = documentoRMS.DocTypeSUNAT;
                documento.UserFields.Fields.Item("U_BPP_MDSD").Value = documentoRMS.Serie;
                documento.UserFields.Fields.Item("U_BPP_MDCD").Value = documentoRMS.Correlativo;

                // OTROS UDF
                documento.UserFields.Fields.Item("U_BPV_SERI").Value = documentoRMS.U_BPV_SERI;
                documento.UserFields.Fields.Item("U_BPV_NCON2").Value = documentoRMS.U_BPV_NCON2;
                documento.UserFields.Fields.Item("U_CL_REFPA1").Value = documentoRMS.U_CL_REFPA1;

                documento.UserFields.Fields.Item("U_VS_FRMSAP").Value = "65300";
                documento.UserFields.Fields.Item("U_VS_AFEDET").Value = "N";

                //CAMPOS MIGRACION
                documento.UserFields.Fields.Item("U_RMS_VALIDATX").Value = documentoRMS.U_RMS_VALIDATX;
                documento.UserFields.Fields.Item("U_VS_TRANS_RMS").Value = Convert.ToString(documentoRMS.ReceiptNo);
                documento.UserFields.Fields.Item("U_VS_COMEN1").Value = $"Factura Anticipo de RMS {documentoRMS.ReceiptNo}";


                // DETALLE
                for (int i = 0; i < detalle.Count; i++)
                {
                    RMSDownPaymentLineData linea = detalle[i];

                    if (i > 0)
                        documento.Lines.Add();

                    documento.Lines.ItemCode = linea.ItemCode;
                    documento.Lines.Quantity = Convert.ToDouble(linea.Quantity);
                    documento.Lines.Price = Convert.ToDouble(linea.Price);

                    documento.Lines.DiscountPercent = Convert.ToDouble(linea.DiscPrcnt);

                    //Código fiscal configurado en SAP
                    documento.Lines.TaxCode = "IEX";

                    documento.Lines.UserFields.Fields.Item("U_BPP_OPER").Value = "I";
                    documento.Lines.UserFields.Fields.Item("U_tipoOpT12").Value = "01";

                    //Tipo Afectación Impuesto
                    documento.Lines.UserFields.Fields.Item("U_VS_TIPAFE").Value = "30";

                    if (!string.IsNullOrWhiteSpace(linea.WhsCode))
                        documento.Lines.WarehouseCode = linea.WhsCode;

                    if (!string.IsNullOrWhiteSpace(linea.AccountCode))
                        documento.Lines.AccountCode = linea.AccountCode;

                    if (!string.IsNullOrWhiteSpace(linea.CostingCode))
                        documento.Lines.CostingCode = linea.CostingCode;

                    if (!string.IsNullOrWhiteSpace(linea.CostingCode2) & linea.CostingCode2 != "0")
                        documento.Lines.CostingCode2 = linea.CostingCode2;

                    if (!string.IsNullOrWhiteSpace(linea.CostingCode3) & linea.CostingCode3 != "0")
                        documento.Lines.CostingCode3 = linea.CostingCode3;

                    if (!string.IsNullOrWhiteSpace(linea.CostingCode4) & linea.CostingCode4 != "0")
                        documento.Lines.CostingCode4 = linea.CostingCode4;

                    if (!string.IsNullOrWhiteSpace(linea.CostingCode5) & linea.CostingCode5 != "0")
                        documento.Lines.CostingCode5 = linea.CostingCode5;
                }

                int resultado = documento.Add();
                if (resultado != 0)
                {
                    _company.GetLastError(out int codigoError, out string mensajeError);
                    throw new Exception($"{codigoError} - {mensajeError}");
                }

                return Convert.ToInt32(_company.GetNewObjectKey());
            }
            finally
            {
                if (documento != null)
                {
                    System.Runtime.InteropServices.Marshal
                        .ReleaseComObject(documento);

                    documento = null;
                }
            }
        }
        public int ObtenerDocNum(int docEntry)
        {
            Documents documento = null;

            try
            {
                documento = (Documents)_company.GetBusinessObject(BoObjectTypes.oDownPayments);

                if (!documento.GetByKey(docEntry))
                    throw new Exception($"No se encontró la ODPI DocEntry {docEntry}");

                return documento.DocNum;
            }
            finally
            {
                if (documento != null)
                {
                    System.Runtime.InteropServices.Marshal
                        .ReleaseComObject(documento);

                    documento = null;
                }
            }
        }
        public int CrearPagoAnticipo(string cardCode, RMSDownPaymentData documentoRMS, List<RMSDownPaymentPaymentData> pagos, int docEntryAnticipo)
        {
            if (!_company.Connected)
                throw new Exception("SAP no está conectado.");

            if (pagos == null || pagos.Count == 0)
                throw new Exception("El anticipo no tiene medios de pago.");

            Payments pago = null;

            try
            {
                pago = (Payments)_company.GetBusinessObject(BoObjectTypes.oIncomingPayments);

                // CABECERA DEL PAGO
                pago.CardCode = cardCode;
                pago.DocDate = documentoRMS.DocDate;
                pago.TaxDate = documentoRMS.TaxDate;
                pago.DueDate = documentoRMS.DocDueDate;

                pago.DocCurrency = documentoRMS.DocCurrency;
                pago.Remarks = $"Pago Anticipo RMS {documentoRMS.ReceiptNo} {DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")}";
                pago.JournalRemarks = $"Pago Anticipo RMS {documentoRMS.ReceiptNo}";

                pago.UserFields.Fields.Item("U_BPP_CHEQ").Value = "000";
                pago.UserFields.Fields.Item("U_BPP_EFEC").Value = "001";
                pago.UserFields.Fields.Item("U_BPP_LETR").Value = "000";
                pago.UserFields.Fields.Item("U_BPP_TARJ").Value = "000";
                pago.UserFields.Fields.Item("U_BPP_TRAN").Value = "000";

                RMSDownPaymentPaymentData linea_pagos = pagos[0];
                // Medio de pago: Fondos en efectivo
                pago.CashAccount = linea_pagos.AccountCode;
                pago.CashSum = Convert.ToDouble(documentoRMS.PayTotal);

                // APLICAR AL ANTICIPO ODPI
                pago.Invoices.DocEntry = docEntryAnticipo;
                pago.Invoices.InvoiceType = BoRcptInvTypes.it_DownPayment;
                pago.Invoices.SumApplied = Convert.ToDouble(documentoRMS.PayTotal);

                // CREAR ORCT
                int resultado = pago.Add();

                if (resultado != 0)
                {
                    _company.GetLastError(out int codigoError, out string mensajeError);
                    throw new Exception($"{codigoError} - {mensajeError}");
                }

                return Convert.ToInt32(_company.GetNewObjectKey());
            }
            finally
            {
                if (pago != null)
                {
                    System.Runtime.InteropServices.Marshal
                        .ReleaseComObject(pago);

                    pago = null;
                }
            }
        }
    }

}
