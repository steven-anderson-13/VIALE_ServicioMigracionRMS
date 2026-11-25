using SERVICIO_INTEGRACION.Data;
using SERVICIO_INTEGRACION.Sap;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace SERVICIO_INTEGRACION.Services
{
    public class IntegrationProcessor
    {
        private readonly RmsRepository _repository;
        private readonly SapDiApiClient _sap;

        public IntegrationProcessor(RmsRepository repository, SapDiApiClient sap)
        {
            _repository = repository;
            _sap = sap;
        }

        public void RunOC()
        {
            DateTime from = DateTime.Parse(ConfigurationManager.AppSettings["Rms.DateFrom"]);
            DateTime to = DateTime.Parse(ConfigurationManager.AppSettings["Rms.DateTo"]);

            var headers = _repository.GetHeaders(from, to);



            // Fecha actual del servidor donde se ejecuta el servicio.
            DateTime fechaActual = DateTime.Today;
            // Primer día del mes actual.
            DateTime fechaDesde = new DateTime(fechaActual.Year, fechaActual.Month, 1);
            // Último día del mes actual.
            DateTime fechaHasta = fechaDesde.AddMonths(1).AddDays(-1);

            foreach (var header in headers)
            {
                bool sapCreationAttempted = false;

                try
                {
                    _repository.SaveResult(header.POId, "PROCESANDO", null, null, null);
                    var lines = _repository.GetLines(header.POId);

                    Validate(header, lines);

                    // Desde aquí un fallo puede requerir reconciliación
                    // antes de reintentar la creación en SAP.
                    sapCreationAttempted = true;
                    var result = _sap.CreatePurchaseOrder(header, lines, fechaDesde, fechaHasta);
                    _repository.SaveResult(header.POId, "OK", result.DocEntry, result.DocNum, null);
                }
                catch (Exception ex)
                {
                    try
                    {
                        // Si ya se intentó crear el documento, un fallo
                        // podría ser ambiguo. Marcar ERROR no implica
                        // que sea seguro volver a enviar el documento.
                        //_repository.SaveResult( header.POId, "ERROR", null, null, (sapCreationAttempted ? "Verificar en SAP antes de reintentar. " : "") + ex.Message);

                        //Error: Ya se encuentra registrado la Orden de Compra en SAP

                        // Si SAP devuelve este mensaje, considerar la OC como OK.
                        if (ex.Message.IndexOf("Ya se encuentra registrado la Orden de Compra en SAP",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _repository.SaveResult(header.POId, "OK", null, null, ex.Message);
                        }
                        else
                        {
                            _repository.SaveResult(header.POId, "ERROR", null, null, ex.Message);
                            EnviarAlertaError("Orden de Compra (SOL)", $"Orden PONo: {header.Comments}", ex.Message.ToString());

                        }

                        //_repository.SaveResult(header.POId, "ERROR", null, null, (ex.Message));
                    }
                    catch
                    {
                        // Registrar también en Event Log en producción.
                    }
                }
            }
        }

        public void RunOC_ME()
        {
            DateTime from = DateTime.Parse(ConfigurationManager.AppSettings["Rms.DateFrom"]);
            DateTime to = DateTime.Parse(ConfigurationManager.AppSettings["Rms.DateTo"]);

            var headers = _repository.GetHeaders_ME(from, to);



            // Fecha actual del servidor donde se ejecuta el servicio.
            DateTime fechaActual = DateTime.Today;
            // Primer día del mes actual.
            DateTime fechaDesde = new DateTime(fechaActual.Year, fechaActual.Month, 1);
            // Último día del mes actual.
            DateTime fechaHasta = fechaDesde.AddMonths(1).AddDays(-1);

            foreach (var header in headers)
            {
                bool sapCreationAttempted = false;

                try
                {
                    _repository.SaveResult(header.POId, "PROCESANDO", null, null, null);
                    var lines = _repository.GetLines(header.POId);

                    Validate(header, lines);

                    // Desde aquí un fallo puede requerir reconciliación
                    // antes de reintentar la creación en SAP.
                    sapCreationAttempted = true;
                    var result = _sap.CreatePurchaseOrder(header, lines, fechaDesde, fechaHasta);
                    _repository.SaveResult(header.POId, "OK", result.DocEntry, result.DocNum, null);

                    EnviarAlertaExito("Orden de Compra (USD)", $"Orden PONo: {header.Comments}", result.DocNum.ToString());

                }
                catch (Exception ex)
                {
                    try
                    {
                        // Si ya se intentó crear el documento, un fallo
                        // podría ser ambiguo. Marcar ERROR no implica
                        // que sea seguro volver a enviar el documento.
                        //_repository.SaveResult( header.POId, "ERROR", null, null, (sapCreationAttempted ? "Verificar en SAP antes de reintentar. " : "") + ex.Message);

                        //Error: Ya se encuentra registrado la Orden de Compra en SAP

                        // Si SAP devuelve este mensaje, considerar la OC como OK.
                        if (ex.Message.IndexOf("Ya se encuentra registrado la Orden de Compra en SAP",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _repository.SaveResult(header.POId, "OK", null, null, ex.Message);
                        }
                        else
                        {
                            _repository.SaveResult(header.POId, "ERROR", null, null, ex.Message);
                            EnviarAlertaError("Orden de Compra (USD)", $"Orden PONo: {header.Comments}", ex.Message.ToString());

                        }

                        //_repository.SaveResult(header.POId, "ERROR", null, null, (ex.Message));
                    }
                    catch
                    {
                        // Registrar también en Event Log en producción.
                    }
                }
            }
        }

        private static void Validate(Models.PurchaseOrderHeader header, System.Collections.Generic.IList<Models.PurchaseOrderLine> lines)
        {
            if (string.IsNullOrWhiteSpace(header.CardCode))
                throw new Exception("La orden no tiene CardCode.");

            if (lines == null || lines.Count == 0)
                throw new Exception("La orden no tiene líneas.");

            foreach (var line in lines)
            {
                //if (string.IsNullOrWhiteSpace(line.ItemCode))
                //    throw new Exception("Existe una línea sin ItemCode.");

                if (line.Quantity <= 0)
                    throw new Exception("La cantidad debe ser mayor que cero.");

                if (string.IsNullOrWhiteSpace(line.WarehouseCode))
                    throw new Exception("La línea no tiene almacén.");
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
                                    <table width='800' cellpadding='0' cellspacing='0' border='0' style='width:800px;max-width:800px;background:#FFFFFF;
                    border:1px solid #E1E5E9;'>

                                        <!-- CABECERA -->
                                        <tr>
                                            <td style='padding:28px 30px;background:#FFFFFF;
                    border-bottom:4px solid #D32F2F;'>

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
                                                        <td style='background:#FDECEC;border-left:6px solid #D32F2F;
                    padding:18px;font-size:16px;font-weight:bold;color:#9B1C1C;'>
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
                                                        <td width='180' valign='middle' style='width:180px;background:#F3F4F6;padding:16px;
                    font-size:15px;font-weight:bold;border:1px solid #E5E7EB;'>
                                                            Proceso
                                                        </td>

                                                        <td valign='middle' style='padding:16px;font-size:15px;border:1px solid #E5E7EB;
                    word-wrap:break-word;'>
                                                            {WebUtility.HtmlEncode(proceso)}
                                                        </td>
                                                    </tr>

                                                    <!-- DOCUMENTO -->
                                                    <tr>
                                                        <td width='180' valign='middle' style='width:180px;background:#F3F4F6;padding:16px;
                    font-size:15px;font-weight:bold;border:1px solid #E5E7EB;'>
                                                            Documento
                                                        </td>

                                                        <td valign='middle' style='padding:16px;font-size:15px;border:1px solid #E5E7EB;
                    word-wrap:break-word;'>
                                                            {WebUtility.HtmlEncode(documento ?? "-")}
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

                                                    <!-- SERVIDOR -->
                                                    <tr>
                                                        <td width='180' valign='middle' style='width:180px;background:#F3F4F6;padding:16px;
                    font-size:15px;font-weight:bold;border:1px solid #E5E7EB;'>
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

                                                <div style='font-size:19px;font-weight:bold;color:#C62828;
                    margin-bottom:18px;'>
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
                                                            Documento RMS
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
                                                            Documento SAP
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
