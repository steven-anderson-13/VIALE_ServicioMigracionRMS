using SERVICIO_INTEGRACION.WindowsService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SERVICIO_INTEGRACION
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ServicioIntegra()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }

    //internal static class Program
    //{
    //    static void Main()
    //    {
    //        ServiceBase.Run(
    //            new IntegrationWindowsService());
    //    }
    //}
}
