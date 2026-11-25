using SERVICIO_INTEGRACION.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using VIALE_ServicioMigracionRMS.Repositories;
using VIALE_ServicioMigracionRMS.Sap;

namespace VIALE_ServicioMigracionRMS.Processors
{
    public class GoodsReceiptReserveInvoiceIntegrationProcessor
    {
        private readonly SapGoodsReceiptReserveInvoiceRepository _repository;
        private readonly SapGoodsReceiptReserveInvoiceClient _client;
        private readonly GoodsReceiptReserveInvoiceLogRepository _log;
        private readonly RmsVoucherRepository _rmsVoucherRepository;

        public GoodsReceiptReserveInvoiceIntegrationProcessor(SapGoodsReceiptReserveInvoiceRepository repository, SapGoodsReceiptReserveInvoiceClient client, GoodsReceiptReserveInvoiceLogRepository log,
            RmsVoucherRepository rmsVoucherRepository)
        {
            _repository = repository;
            _client = client;
            _log = log;
            _rmsVoucherRepository = rmsVoucherRepository;
        }

        public void Run_EntradaMercancia_FR()
        {
            DateTime fechaDesde = DateTime.Parse(ConfigurationManager.AppSettings["Rms.DateFrom"]);
            DateTime fechaHasta = DateTime.Parse(ConfigurationManager.AppSettings["Rms.DateTo"]);

            if (fechaDesde > fechaHasta)
                throw new Exception("Rms.DateFrom no puede ser mayor que Rms.DateTo.");

            var facturas = _repository.ObtenerFacturasReservaImportacion(fechaDesde, fechaHasta);

            foreach (var factura in facturas)
            {
                try
                {
                    _log.RegistrarResultado(factura.DocEntry, factura.DocNum, null, null, "PROCESANDO", "Iniciando creación Entrada de mercancia.");

                    if (_log.ExisteEntradaExitosa(factura.DocEntry))
                        continue;

                    var detalle = _repository.ObtenerDetalleFacturaReservaImportacion(factura.DocEntry);

                    if (detalle == null || detalle.Count == 0)
                    {
                        _log.RegistrarResultado(factura.DocEntry, factura.DocNum, null, null, "ERROR", "La factura de reserva no tiene líneas.");
                        EnviarAlertaError("Entrada de Mercancia - Importación", $"Factura Reserva: {factura.DocNum}", "La factura de reserva no tiene líneas.");

                        continue;
                    }

                    // Obtener POIds RMS desde las OC SAP relacionadas
                    var poIds = _client.ObtenerRmsPOIdsPorFacturaReserva(factura.DocEntry);          

                    // Obtener vouchers y fecha más antigua
                    var rmsVoucherData = _rmsVoucherRepository.ObtenerVoucherNoPorPOIds(poIds);

                    string voucherNos = rmsVoucherData.VoucherNoConcatenados;

                    // Fecha del recibo más antiguo
                    if (!rmsVoucherData.ReceiveDate.HasValue)
                    {
                        throw new Exception($"No se encontraron fechas de recibos RMS para la factura {factura.DocNum}.");
                    }

                    DateTime fechaRecibo = rmsVoucherData.ReceiveDate.Value;

                    var entrada = _client.CrearEntradaMercancias(factura, detalle, voucherNos, fechaRecibo);

                    _log.RegistrarResultado(factura.DocEntry, factura.DocNum, entrada.DocEntry, entrada.DocNum, "OK", $"Entrada de mercancías creada correctamente.");

                    EnviarAlertaExito("Entrada de Mercancia - Importación", $"{factura.U_BPP_MDTD}{factura.U_BPP_MDSD}-{factura.U_BPP_MDCD}", entrada.DocNum.ToString());

                }
                catch (Exception ex)
                {
                    _log.RegistrarResultado(factura.DocEntry, factura.DocNum, null, null, "ERROR", ex.Message);
                    EnviarAlertaError("Entrada de Mercancia - Importación", $"Factura Reserva: {factura.DocNum}", ex.Message.ToString());

                }
            }
        }
        public static void EnviarAlertaError(string proceso, string documento, string mensajeError)
        {
            try
            {
                string smtpHost = ConfigurationManager.AppSettings["Mail.SmtpHost"];
                int smtpPort = int.Parse(ConfigurationManager.AppSettings["Mail.SmtpPort"]);
                string smtpUser = ConfigurationManager.AppSettings["Mail.User"];
                string smtpPassword = ConfigurationManager.AppSettings["Mail.Password"];
                string correoDestino = ConfigurationManager.AppSettings["Mail.To"];
                string correoCC = ConfigurationManager.AppSettings["Mail.CC"];

                using (MailMessage correo = new MailMessage())
                {
                    correo.From = new MailAddress(smtpUser);
                    correo.To.Add(correoDestino);

                    if (!string.IsNullOrWhiteSpace(correoCC))
                        correo.CC.Add(correoCC);

                    correo.Subject = $"🚨 ERROR MIGRACIÓN RMS - SAP | {proceso}";
                    correo.IsBodyHtml = true;
                    correo.Body = $@"
                    <html>

                    <head>
                        <meta charset='UTF-8'>
                    </head>

                    <body style='margin:0;padding:0;background-color:#F4F6F8;
                    font-family:Arial,Helvetica,sans-serif;color:#333333;'>

                        <table width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#F4F6F8;'>
                            <tr>
                                <td align='center' style='padding:25px 10px;'>

                                    <!-- CONTENEDOR PRINCIPAL -->
                                    <table width='800' cellpadding='0' cellspacing='0' border='0' style='width:800px;max-width:800px;background:#FFFFFF; border:1px solid #E1E5E9;'>

                                        <!-- CABECERA -->
                                        <tr>
                                            <td style='padding:28px 30px;background:#FFFFFF; border-bottom:4px solid #D32F2F;'>

                                                <div style='font-size:25px;font-weight:bold;color:#C62828;'>
                                                    &#128680; ERROR EN MIGRACIÓN RMS - SAP
                                                </div>

                                                <div style='font-size:14px;color:#777777;margin-top:10px;'>
                                                    Servicio de Migración RMS - SAP
                                                </div>

                                            </td>
                                        </tr>

                                        <!-- MENSAJE PRINCIPAL -->
                                        <tr>
                                            <td style='padding:25px 30px 15px 30px;'>

                                                <table width='100%' cellpadding='0' cellspacing='0' border='0'>
                                                    <tr>
                                                        <td style='background:#FDECEC;border-left:6px solid #D32F2F; padding:18px;font-size:16px;font-weight:bold;color:#9B1C1C;'>
                                                            Se ha detectado un error durante el proceso de migración.
                                                        </td>
                                                    </tr>
                                                </table>

                                            </td>
                                        </tr>

                                        <!-- INFORMACION DEL ERROR -->
                                        <tr>
                                            <td style='padding:15px 30px 25px 30px;'>

                                                <table width='100%' cellpadding='0' cellspacing='0' border='0' style='border-collapse:collapse;table-layout:fixed;'>

                                                    <!-- PROCESO -->
                                                    <tr>
                                                        <td width='180' valign='middle' style='width:180px;background:#F3F4F6;padding:16px; font-size:15px;font-weight:bold;border:1px solid #E5E7EB;'>
                                                            Proceso
                                                        </td>

                                                        <td valign='middle' style='padding:16px;font-size:15px;border:1px solid #E5E7EB;  word-wrap:break-word;'>
                                                            {WebUtility.HtmlEncode(proceso)}
                                                        </td>
                                                    </tr>

                                                    <!-- DOCUMENTO -->
                                                    <tr>
                                                        <td width='180' valign='middle' style='width:180px;background:#F3F4F6;padding:16px; font-size:15px;font-weight:bold;border:1px solid #E5E7EB;'>
                                                            Documento
                                                        </td>

                                                        <td valign='middle' style='padding:16px;font-size:15px;border:1px solid #E5E7EB; word-wrap:break-word;'>
                                                            {WebUtility.HtmlEncode(documento ?? "-")}
                                                        </td>
                                                    </tr>

                                                    <!-- FECHA -->
                                                    <tr>
                                                        <td width='180' valign='middle' style='width:180px;background:#F3F4F6;padding:16px; font-size:15px;font-weight:bold;border:1px solid #E5E7EB;'>
                                                            Fecha / Hora
                                                        </td>

                                                        <td valign='middle' style='padding:16px;font-size:15px;border:1px solid #E5E7EB;'>
                                                            {DateTime.Now:dd/MM/yyyy hh:mm:ss tt}
                                                        </td>
                                                    </tr>

                                                    <!-- SERVIDOR -->
                                                    <tr>
                                                        <td width='180' valign='middle' style='width:180px;background:#F3F4F6;padding:16px; font-size:15px;font-weight:bold;border:1px solid #E5E7EB;'>
                                                            Servidor
                                                        </td>

                                                        <td valign='middle' style='padding:16px;font-size:15px;border:1px solid #E5E7EB;'>
                                                            {Environment.MachineName}
                                                        </td>
                                                    </tr>

                                                </table>

                                            </td>
                                        </tr>

                                        <!-- DETALLE DEL ERROR -->
                                        <tr>
                                            <td style='padding:0px 30px 30px 30px;'>

                                                <div style='font-size:19px;font-weight:bold;color:#C62828;  margin-bottom:18px;'>
                                                    Detalle del error
                                                </div>

                                                <table width='100%' cellpadding='0' cellspacing='0' border='0'>
                                                    <tr>
                                                        <td style='background:#F8F9FA;border:1px solid #DDE1E5;
                    padding:22px;font-family:Consolas,Courier New,monospace;
                    font-size:14px;color:#333333;line-height:22px;
                    word-wrap:break-word;'>
                                                            {WebUtility.HtmlEncode(mensajeError)}
                                                        </td>
                                                    </tr>
                                                </table>

                                            </td>
                                        </tr>

                                        <!-- PIE DEL CORREO -->
                                        <tr>
                                            <td style='background:#F3F4F6;border-top:1px solid #E1E5E9;
                    padding:22px 30px;font-size:13px;color:#777777;
                    line-height:20px;'>

                                                Correo autom&aacute;tico generado por el Servicio de Migración RMS - SAP.
                                       

                                            </td>
                                        </tr>

                                    </table>
                                    <!-- FIN CONTENEDOR PRINCIPAL -->

                                </td>
                            </tr>
                        </table>

                    </body>

                    </html>";


                    using (SmtpClient smtp = new SmtpClient(smtpHost, smtpPort))
                    {
                        smtp.EnableSsl = true;
                        smtp.Credentials = new NetworkCredential(smtpUser, smtpPassword);
                        smtp.Timeout = 9999999;
                        smtp.Send(correo);
                    }
                }
            }
            catch (Exception ex)
            {
                // El fallo del correo no debe detener la integración.
                System.Diagnostics.EventLog.WriteEntry("SERVICIO_INTEGRACION", "Error enviando alerta por correo: " + ex.Message, System.Diagnostics.EventLogEntryType.Warning);
            }
        }
        public static void EnviarAlertaExito(string proceso, string documento, string documentoSAP)
        {
            try
            {
                string smtpHost = ConfigurationManager.AppSettings["Mail.SmtpHost"];
                int smtpPort = int.Parse(ConfigurationManager.AppSettings["Mail.SmtpPort"]);
                string smtpUser = ConfigurationManager.AppSettings["Mail.User"];
                string smtpPassword = ConfigurationManager.AppSettings["Mail.Password"];
                string correoDestino = ConfigurationManager.AppSettings["Mail.To"];
                string correoCC = ConfigurationManager.AppSettings["Mail.CC"];
                string correoCC1 = ConfigurationManager.AppSettings["Mail.CC1"];
                string correoCC2 = ConfigurationManager.AppSettings["Mail.CC2"];
                string correoCC3 = ConfigurationManager.AppSettings["Mail.CC3"];

                using (MailMessage correo = new MailMessage())
                {
                    correo.From = new MailAddress(smtpUser);
                    correo.To.Add(correoDestino);

                    if (!string.IsNullOrWhiteSpace(correoCC))
                        correo.CC.Add(correoCC);

                    if (!string.IsNullOrWhiteSpace(correoCC1))
                        correo.CC.Add(correoCC1);

                    if (!string.IsNullOrWhiteSpace(correoCC2))
                        correo.CC.Add(correoCC2);

                    if (!string.IsNullOrWhiteSpace(correoCC3))
                        correo.CC.Add(correoCC3);

                    correo.Subject = $"✅ MIGRACIÓN EXITOSA RMS - SAP | {proceso}";
                    correo.IsBodyHtml = true;
                    correo.Body = $@"
                    <html>

                    <head>
                        <meta charset='UTF-8'>
                    </head>

                    <body style='margin:0;padding:0;background-color:#F4F6F8;
                    font-family:Arial,Helvetica,sans-serif;color:#333333;'>

                        <table width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#F4F6F8;'>
                            <tr>
                                <td align='center' style='padding:25px 10px;'>

                                    <!-- CONTENEDOR PRINCIPAL -->
                                    <table width='800' cellpadding='0' cellspacing='0' border='0' style='width:800px;max-width:800px;background:#FFFFFF;
                    border:1px solid #E1E5E9;'>

                                        <!-- CABECERA -->
                                        <tr>
                                            <td style='padding:28px 30px;background:#FFFFFF;
                    border-bottom:4px solid #2E7D32;'>

                                                <div style='font-size:25px;font-weight:bold;color:#2E7D32;'>
                                                    &#9989; MIGRACIÓN EXITOSA RMS - SAP
                                                </div>

                                                <div style='font-size:14px;color:#777777;margin-top:10px;'>
                                                    Servicio de Migración RMS - SAP
                                                </div>

                                            </td>
                                        </tr>

                                        <!-- MENSAJE PRINCIPAL -->
                                        <tr>
                                            <td style='padding:25px 30px 15px 30px;'>

                                                <table width='100%' cellpadding='0' cellspacing='0' border='0'>
                                                    <tr>
                                                        <td style='background:#EAF5EC;border-left:6px solid #2E7D32;
                    padding:18px;font-size:16px;font-weight:bold;color:#1B5E20;'>
                                                            El documento ha sido creado correctamente en SAP Business One.
                                                        </td>
                                                    </tr>
                                                </table>

                                            </td>
                                        </tr>

                                        <!-- INFORMACION DEL DOCUMENTO -->
                                        <tr>
                                            <td style='padding:15px 30px 25px 30px;'>

                                                <table width='100%' cellpadding='0' cellspacing='0' border='0' style='border-collapse:collapse;table-layout:fixed;'>

                                                    <!-- PROCESO -->
                                                    <tr>
                                                        <td width='180' valign='middle' style='width:180px;background:#F3F4F6;padding:16px;
                    font-size:15px;font-weight:bold;border:1px solid #E5E7EB;'>
                                                            Proceso
                                                        </td>

                                                        <td valign='middle' style='padding:16px;font-size:15px;border:1px solid #E5E7EB;
                    word-wrap:break-word;'>
                                                            {WebUtility.HtmlEncode(proceso)}
                                                        </td>
                                                    </tr>

                                                    <!-- DOCUMENTO RMS -->
                                                    <tr>
                                                        <td width='180' valign='middle' style='width:180px;background:#F3F4F6;padding:16px;
                    font-size:15px;font-weight:bold;border:1px solid #E5E7EB;'>
                                                            Factura Reserva Nro
                                                        </td>

                                                        <td valign='middle' style='padding:16px;font-size:15px;border:1px solid #E5E7EB;
                    word-wrap:break-word;'>
                                                            {WebUtility.HtmlEncode(documento ?? "-")}
                                                        </td>
                                                    </tr>

                                                    <!-- DOCUMENTO SAP -->
                                                    <tr>
                                                        <td width='180' valign='middle' style='width:180px;background:#F3F4F6;padding:16px;
                    font-size:15px;font-weight:bold;border:1px solid #E5E7EB;'>
                                                            Entrada Mercancia SAP
                                                        </td>

                                                        <td valign='middle' style='padding:16px;font-size:15px;border:1px solid #E5E7EB;
                    word-wrap:break-word;color:#2E7D32;font-weight:bold;'>
                                                            {WebUtility.HtmlEncode(documentoSAP ?? "-")}
                                                        </td>
                                                    </tr>

                                                    <!-- FECHA -->
                                                    <tr>
                                                        <td width='180' valign='middle' style='width:180px;background:#F3F4F6;padding:16px;
                    font-size:15px;font-weight:bold;border:1px solid #E5E7EB;'>
                                                            Fecha / Hora
                                                        </td>

                                                        <td valign='middle' style='padding:16px;font-size:15px;border:1px solid #E5E7EB;'>
                                                            {DateTime.Now:dd/MM/yyyy hh:mm:ss tt}
                                                        </td>
                                                    </tr> 

                                                </table>

                                            </td>
                                        </tr>

                                        <!-- DETALLE DEL RESULTADO -->
                                        <tr>
                                            <td style='padding:0px 30px 30px 30px;'>

                                                <div style='font-size:19px;font-weight:bold;color:#2E7D32;
                    margin-bottom:18px;'>
                                                    Resultado del proceso
                                                </div>

                                                <table width='100%' cellpadding='0' cellspacing='0' border='0'>
                                                    <tr>
                                                        <td style='background:#F1F8F2;border:1px solid #C8E6C9;
                    padding:22px;font-size:14px;color:#2E7D32;line-height:22px;
                    word-wrap:break-word;'>
                                                            El proceso de integración finalizó satisfactoriamente.
                                                            El documento se encuentra registrado en SAP Business One.
                                                        </td>
                                                    </tr>
                                                </table>

                                            </td>
                                        </tr>

                                        <!-- PIE DEL CORREO -->
                                        <tr>
                                            <td style='background:#F3F4F6;border-top:1px solid #E1E5E9;
                    padding:22px 30px;font-size:13px;color:#777777;
                    line-height:20px;'>

                                                Correo autom&aacute;tico generado por el Servicio de Migración RMS - SAP.

                                            </td>
                                        </tr>

                                    </table>
                                    <!-- FIN CONTENEDOR PRINCIPAL -->

                                </td>
                            </tr>
                        </table>

                    </body>

                    </html>";


                    using (SmtpClient smtp = new SmtpClient(smtpHost, smtpPort))
                    {
                        smtp.EnableSsl = true;
                        smtp.Credentials = new NetworkCredential(smtpUser, smtpPassword);
                        smtp.Timeout = 9999999;
                        smtp.Send(correo);
                    }
                }
            }
            catch (Exception ex)
            {
                // El fallo del correo no debe detener la integración.
                System.Diagnostics.EventLog.WriteEntry("SERVICIO_INTEGRACION", "Error enviando alerta por correo: " + ex.Message, System.Diagnostics.EventLogEntryType.Warning);
            }
        }





    }
}
