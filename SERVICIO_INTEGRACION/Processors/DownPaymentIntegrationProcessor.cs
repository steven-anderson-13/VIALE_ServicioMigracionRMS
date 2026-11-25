using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VIALE_ServicioMigracionRMS.Models;
using VIALE_ServicioMigracionRMS.Repositories;
using VIALE_ServicioMigracionRMS.Sap;

namespace VIALE_ServicioMigracionRMS.Processors
{
    public class DownPaymentIntegrationProcessor : IDisposable
    {
        private readonly RMSDownPaymentRepository _repository;
        private readonly RMSDownPaymentLogRepository _logRepository;
        private readonly SapBusinessPartnerClient _sapBusinessPartnerClient;
        private Company _company;

        public DownPaymentIntegrationProcessor(RMSDownPaymentRepository repository, RMSDownPaymentLogRepository logRepository, SapBusinessPartnerClient sapBusinessPartnerClient)
        {
            _repository = repository;
            _logRepository = logRepository;
            _sapBusinessPartnerClient = sapBusinessPartnerClient;
        }

        public void RunFacturasAnticipos()
        {
            DateTime fechaDesde = DateTime.Parse(ConfigurationManager.AppSettings["Rms.DateFrom"]);
            DateTime fechaHasta = DateTime.Parse(ConfigurationManager.AppSettings["Rms.DateTo"]);

            if (fechaDesde > fechaHasta)
                throw new Exception("Rms.DateFrom no puede ser mayor que Rms.DateTo.");

            var pendientes = _repository.ObtenerAnticiposPendientes(fechaDesde, fechaHasta);

            foreach (var anticipo in pendientes)
            {
                int? docEntry = null;
                int? docNum = null;

                try
                {
                    _logRepository.RegistrarResultado(anticipo.ReceiptNo, anticipo.ReceiptId, null, null, "PROCESANDO", "Iniciando creación Factura de anticipo.");

                    // 1. Obtener información del cliente
                    var cliente = _repository.ObtenerCliente(anticipo.ReceiptNo, anticipo.ReceiptId);

                    var direcciones = _repository.ObtenerDirecciones(anticipo.ReceiptNo, anticipo.ReceiptId);

                    // 2. Obtener cabecera
                    var documento = _repository.ObtenerDocumento(anticipo.ReceiptNo, anticipo.ReceiptId);

                    // 3. Obtener detalle
                    var detalle = _repository.ObtenerDetalle(anticipo.ReceiptNo, anticipo.ReceiptId);

                    // NUEVO: obtener medios de pago RMS
                    var pagos = _repository.ObtenerPagos(anticipo.ReceiptNo, anticipo.ReceiptId);

                    if (detalle == null || detalle.Count == 0)
                        throw new Exception("El anticipo no tiene detalle.");

                    // 4. Crear cliente solamente si no existe
                    if (!_sapBusinessPartnerClient.ExisteCliente(cliente.CardCode))
                    {
                        _sapBusinessPartnerClient.CrearCliente(cliente, direcciones);
                    }

                    // Crear ODPI + ORCT dentro de una transacción
                    ProcesarDocumentosSAP(cliente, documento, detalle, pagos);

                }
                catch (Exception ex)
                {
                    // Registrar error
                    _logRepository.RegistrarResultado(anticipo.ReceiptNo, anticipo.ReceiptId, docEntry, docNum, "ERROR", ex.Message);
                    EnviarAlertaError("Factura de anticipo", $"Receipt No: {anticipo.ReceiptNo}", ex.Message.ToString());
           
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
        public void ProcesarDocumentosSAP(RMSDownPaymentCustomerData cliente, RMSDownPaymentData documento, List<RMSDownPaymentLineData> detalle, List<RMSDownPaymentPaymentData> pagos)
        {
            int docEntryAnticipo = 0;
            int docEntryPago = 0;
            int? docNum = null;

            bool transaccionIniciada = false;

            

            try
            {
                Connect();

                if (_company == null || !_company.Connected)
                    throw new Exception("SAP no está conectado.");

                // El cliente recibe LA MISMA conexión.
                var sapClient = new SapDownPaymentClient(_company);

                // Iniciar transacción sobre esa conexión.
                _company.StartTransaction();
                transaccionIniciada = true;

                // 1. Crear factura de anticipo
                docEntryAnticipo = sapClient.CrearFacturaAnticipo(cliente, documento, detalle);

                // 2. Crear pago recibido y aplicarlo al anticipo
                docEntryPago = sapClient.CrearPagoAnticipo(cliente.CardCode, documento, pagos, docEntryAnticipo);

                // 3. Confirmar ambos documentos
                if (_company.InTransaction)
                {
                    _company.EndTransaction(BoWfTransOpt.wf_Commit);
                    transaccionIniciada = false;
                }

                // 4. Obtener DocNum generado
                docNum = sapClient.ObtenerDocNum(docEntryAnticipo);

                // 5. Registrar OK
                _logRepository.RegistrarResultado(documento.ReceiptNo, documento.ReceiptId, docEntryAnticipo, docNum, "OK", "Factura de anticipo creada correctamente.");

            }
            catch
            {
                if (transaccionIniciada && _company.Connected && _company.InTransaction)
                {
                    _company.EndTransaction(BoWfTransOpt.wf_RollBack);
                }

                throw;
            }
            finally
            {
                Disconnect();
            }
        }
        private void Disconnect()
        {
            if (_company == null)
                return;

            try
            {
                if (_company.Connected)
                    _company.Disconnect();
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(_company);
                _company = null;
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
