using SERVICIO_INTEGRACION.Data;
using SERVICIO_INTEGRACION.Sap;
using SERVICIO_INTEGRACION.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VIALE_ServicioMigracionRMS.Processors;
using VIALE_ServicioMigracionRMS.Repositories;
using VIALE_ServicioMigracionRMS.Sap;

namespace SERVICIO_INTEGRACION
{
    partial class ServicioIntegra : ServiceBase
    {
        private System.Timers.Timer _timer;
        private readonly object _lock = new object();
        private const int Minutos = 3;

        protected override void OnStart(string[] args)
        {
            _timer = new System.Timers.Timer(Minutos * 60 * 1000);
            _timer.AutoReset = false;
            _timer.Elapsed += (s, e) => Tick();

            // Iniciar Timer
            _timer.Start();

            // Primera ejecución inmediata
            ThreadPool.QueueUserWorkItem(_ => Tick());
        }
        protected override void OnStop()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }
        private void Tick()
        {
            if (!Monitor.TryEnter(_lock))
                return;

            try
            {
                EjecutarCiclo();
            }
            catch (Exception ex)
            {
                RegistrarError(ex);
            }
            finally
            {
                Monitor.Exit(_lock);

                // Programar siguiente ejecución
                _timer?.Start();
            }
        }
        private void EjecutarCiclo()
        {
            try
            {
                //--------- MIGRAR ORDENES DE COMPRA SOL --------//
                using (var sap = new SapDiApiClient())
                {
                    var repository = new RmsRepository();
                    var processor = new IntegrationProcessor(repository, sap);
                    processor.RunOC();
                }

                //--------- MIGRAR ORDENES DE COMPRA USD --------// CON ALERTA CONTAB
                using (var sap = new SapDiApiClient())
                {
                    var repository = new RmsRepository();
                    var processor = new IntegrationProcessor(repository, sap);
                    processor.RunOC_ME();
                }

                //--------- MIGRAR ENTRADA DE MERCACIAS POR COMPRA SOL --------//
                using (var sapVoucher = new SapGoodsReceiptClient())
                {
                    var repositoryVoucher = new RmsVoucherRepository();
                    var procesorVoucher = new VoucherIntegrationProcessor(repositoryVoucher, sapVoucher);
                    procesorVoucher.RunEM();
                }

                //--------- REGISTRAR ENTRADAS DE MERCANCIAS POR FACTURA RESERVA IMPORTACIONES --------//   CON ALERTA CONTAB
                using (var client = new SapGoodsReceiptReserveInvoiceClient())
                {
                    var repository = new SapGoodsReceiptReserveInvoiceRepository();
                    var log = new GoodsReceiptReserveInvoiceLogRepository();
                    var rmsVoucherRepository = new RmsVoucherRepository();
                    var processor = new GoodsReceiptReserveInvoiceIntegrationProcessor(repository, client, log, rmsVoucherRepository);
                    processor.Run_EntradaMercancia_FR();
                }

                //--------- REGISTRAR TRANSFERENCIA DE STOCK POR ENTRADA MERCABCIA IMPORTACIONES --------//
                using (var sapTransf = new SapStockTransferClient())
                {
                    var sapRepository = new SapStockTransferRepository();
                    var logRepository = new StockTransferLogRepository();
                    var processor = new StockTransferIntegrationProcessor(sapRepository, sapTransf, logRepository);
                    processor.RunImportTransfers();
                }

                ////--------- MIGRAR FACTURA DE ANTICIPO --------//
                //using (var sapBusinessPartner = new SapBusinessPartnerClient())
                //{
                //    var repository = new RMSDownPaymentRepository();
                //    var logRepository = new RMSDownPaymentLogRepository();
                //    var processor = new DownPaymentIntegrationProcessor(repository, logRepository, sapBusinessPartner);
                //    processor.RunFacturasAnticipos();
                //}


                //Log.Info("ServicioIntegracion finalizado OK.");
            }
            catch (Exception ex)
            {
                //Log.Error("ServicioIntegracion fallo crítico en la ejecución principal.", ex);
            }
        }

        private void RegistrarError(Exception ex)
        {
            // Aquí podemos integrar Serilog o EventLog
            System.Diagnostics.EventLog.WriteEntry(
                ServiceName,
                ex.ToString(),
                System.Diagnostics.EventLogEntryType.Error);
        }

    }
}
