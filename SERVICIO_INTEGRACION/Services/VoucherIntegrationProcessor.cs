using SERVICIO_INTEGRACION.Data;
using SERVICIO_INTEGRACION.Models;
using SERVICIO_INTEGRACION.Sap;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace SERVICIO_INTEGRACION.Services
{
    public class VoucherIntegrationProcessor
    {
        private readonly RmsVoucherRepository _repository;
        private readonly SapGoodsReceiptClient _sapClient;

        public VoucherIntegrationProcessor(RmsVoucherRepository repository, SapGoodsReceiptClient sap)
        {
            _repository = repository;
            _sapClient = sap;
        }

        public void RunEM()
        {
            DateTime from = DateTime.Parse(ConfigurationManager.AppSettings["Rms.DateFrom"]);
            DateTime to = DateTime.Parse(ConfigurationManager.AppSettings["Rms.DateTo"]);

            List<VoucherHeader> vouchers = _repository.GetVouchers(from, to);



            foreach (VoucherHeader header in vouchers)
            {
                bool sapCreationAttempted = false;

                try
                {
                    _repository.SaveResult(header.VoucherId, "PROCESANDO", null, null, null);

                    List<VoucherLine> lines = _repository.GetVoucherLines(header.VoucherId);

                    Validate(header, lines);

                    // A partir de aquí se intentará crear el documento en SAP.
                    sapCreationAttempted = true;

                    SapGoodsReceiptResult result = _sapClient.CreateGoodsReceipt(header, lines);
                    _repository.SaveResult(header.VoucherId, "OK", result.DocEntry, result.DocNum, null);

                    MarkVoucherMigrated(header.VoucherId);


                }
                catch (Exception ex)
                {
                    try
                    {
                        //// Si se intentó crear el documento y ocurrió un error,
                        //// no asumir que SAP no lo creó.
                        //if (sapCreationAttempted)
                        //{
                        //    mensaje = "Verificar en SAP antes de reintentar. " + ex.Message;
                        //}


                        // Si SAP devuelve este mensaje, considerar la EM como OK.
                        if (ex.Message.IndexOf("Ya se encuentra registrado la Entrada de Mercancia en SAP",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _repository.SaveResult(header.VoucherId, "OK", null, null, ex.Message);
                        }
                        else
                        {
                            _repository.SaveResult(header.VoucherId, "ERROR", null, null, ex.Message);
                            EnviarAlertaError("Entrada de Mercancia", $"{header.Comments}", ex.Message.ToString());

                        }

                        //_repository.SaveResult(header.VoucherId, "ERROR", null, null, ex.Message);
                    }
                    catch
                    {
                        // Registrar el fallo del log en el Event Log
                        // o en el sistema de logging del servicio.
                    }
                }
            }
        }

        public void MarkVoucherMigrated(Guid voucherId)
        {
            //ConfigurationManager.AppSettings["Rms.ConnectionString"];
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.AppSettings["Rms.ConnectionString"]))
            using (SqlCommand command = new SqlCommand("dbo.SP_RMS_MarcarVoucherMigrado", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add("@VoucherId", SqlDbType.UniqueIdentifier).Value = voucherId;

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private static void Validate(VoucherHeader header, List<VoucherLine> lines)
        {
            if (string.IsNullOrWhiteSpace(header.CardCode))
                throw new InvalidOperationException("El VOUCHER no tiene CardCode.");

            if (string.IsNullOrWhiteSpace(header.PONo))
                throw new InvalidOperationException("El VOUCHER no tiene número de orden de compra.");

            if (lines == null || lines.Count == 0)
                throw new InvalidOperationException("El VOUCHER no tiene detalle.");

            foreach (VoucherLine line in lines)
            {
                if (string.IsNullOrWhiteSpace(line.ItemCode))
                    throw new InvalidOperationException("Existe una línea sin código de artículo.");

                if (line.Quantity <= 0)
                    throw new InvalidOperationException("El artículo " + line.ItemCode + " tiene una cantidad inválida.");

                if (string.IsNullOrWhiteSpace(line.WarehouseCode))
                    throw new InvalidOperationException("No se encontró almacén para el artículo " + line.ItemCode + ".");
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

    }
}
