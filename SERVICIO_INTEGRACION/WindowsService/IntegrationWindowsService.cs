using SERVICIO_INTEGRACION.Data;
using SERVICIO_INTEGRACION.Sap;
using SERVICIO_INTEGRACION.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SERVICIO_INTEGRACION.WindowsService
{
    public partial class IntegrationWindowsService : ServiceBase
    {
        private CancellationTokenSource _cts;
        private Task _workerTask;

        public IntegrationWindowsService()
        {
            //InitializeComponent();

            ServiceName = "SERVICIO_INTEGRACION";
        }

        protected override void OnStart(string[] args)
        {
            _cts = new CancellationTokenSource();

            // Ejecutar el procesamiento en segundo plano
            _workerTask = Task.Run(
                () => EjecutarIntegracion(_cts.Token));
        }

        protected override void OnStop()
        {
            if (_cts != null)
            {
                _cts.Cancel();

                if (_workerTask != null)
                {
                    _workerTask.Wait(TimeSpan.FromSeconds(30));
                }

                _cts.Dispose();
            }
        }

        private void EjecutarIntegracion(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // ------------------------------------------
                    // INTEGRAR ORDENES DE COMPRA SQL
                    // ------------------------------------------
                    using (var sap = new SapDiApiClient())
                    {
                        var repository = new RmsRepository();

                        var processor = new IntegrationProcessor(repository, sap);

                        processor.RunOC();
                    }

                    // ------------------------------------------
                    // INTEGRAR ORDENES DE COMPRA USD
                    // ------------------------------------------
                    using (var sap = new SapDiApiClient())
                    {
                        var repository = new RmsRepository();

                        var processor = new IntegrationProcessor(repository, sap);

                        processor.RunOC_ME();
                    }

                    // ------------------------------------------
                    // INTEGRAR ENTRADA DE MERCADERIA
                    // ------------------------------------------
                    using (var sapVoucher = new SapGoodsReceiptClient())
                    {
                        var repositoryVoucher = new RmsVoucherRepository();

                        var processorVoucher = new VoucherIntegrationProcessor(repositoryVoucher, sapVoucher);

                        processorVoucher.RunEM();
                    }
                }
                catch (Exception ex)
                {
                    // Registrar error en log de Windows o archivo
                    RegistrarError(ex);
                }

                // Intervalo entre ejecuciones
                if (token.WaitHandle.WaitOne(
                    TimeSpan.FromMinutes(5)))
                {
                    break;
                }
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
