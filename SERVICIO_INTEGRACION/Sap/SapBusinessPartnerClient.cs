using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VIALE_ServicioMigracionRMS.Models;

namespace VIALE_ServicioMigracionRMS.Sap
{
    public class SapBusinessPartnerClient : IDisposable
    {
        private Company _company;
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
        public bool ExisteCliente(string cardCode)
        {
            using (OdbcConnection conn = SAPCONN.GetConnection())
            {
                using (OdbcCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT COUNT(1)
                FROM ""SBO_VIALE_PRODUCCION"".""OCRD""
                WHERE ""CardCode"" = ?
                  AND ""CardType"" = 'C'";

                    cmd.CommandType = CommandType.Text;

                    cmd.Parameters.Add("CardCode", OdbcType.VarChar).Value = cardCode;

                    int cantidad = Convert.ToInt32(cmd.ExecuteScalar());

                    return cantidad > 0;
                }
            }
        }
        public void CrearCliente(RMSDownPaymentCustomerData cliente, List<RMSDownPaymentAddressData> direcciones)
        {
            if (cliente == null)
                throw new Exception("No se recibió información del cliente RMS.");

            if (string.IsNullOrWhiteSpace(cliente.CardCode))
                throw new Exception("El CardCode del cliente está vacío.");

            if (ExisteCliente(cliente.CardCode))
                return;

            Connect();

            BusinessPartners bp = (BusinessPartners)_company.GetBusinessObject(BoObjectTypes.oBusinessPartners);

            bp.CardCode = cliente.CardCode;
            bp.CardName = !string.IsNullOrWhiteSpace(cliente.CompanyName)
                ? cliente.CompanyName
                : $"{cliente.FirstName} {cliente.LastName}".Trim();

            bp.CardType = BoCardTypes.cCustomer;
            bp.GroupCode = cliente.GroupCode;
            bp.FederalTaxID = cliente.FederalTaxId;
            bp.EmailAddress = cliente.Email;
            bp.Phone1 = cliente.Phone1;
            bp.Phone2 = cliente.Phone2;

            // UDF de identificación / datos SUNAT
            bp.UserFields.Fields.Item("U_BPP_BPTD").Value = cliente.U_BPP_BPTD ?? "";
            bp.UserFields.Fields.Item("U_BPP_BPTP").Value = cliente.U_BPP_BPTP ?? "";
            bp.UserFields.Fields.Item("U_VS_NOMCOM").Value = cliente.U_VS_NOMCOM ?? "";
            bp.UserFields.Fields.Item("U_BPP_BPAP").Value = cliente.U_BPP_BPAP ?? "";
            bp.UserFields.Fields.Item("U_BPP_BPAM").Value = cliente.U_BPP_BPAM ?? "";
            bp.UserFields.Fields.Item("U_BPP_BPNO").Value = cliente.U_BPP_BPNO ?? "";
            bp.UserFields.Fields.Item("U_VS_EXCCOD").Value = cliente.U_VS_EXCCOD ?? "";

            // Direcciones
            if (direcciones != null)
            {
                foreach (var direccion in direcciones)
                {
                    bp.Addresses.AddressName = direccion.AddressName;
                    bp.Addresses.Street = direccion.Street;
                    bp.Addresses.ZipCode = direccion.ZipCode;
                    bp.Addresses.AddressName2 = direccion.AddressName2;

                    if (!string.IsNullOrWhiteSpace(direccion.Country))
                        bp.Addresses.Country = direccion.Country;

                    if (!string.IsNullOrWhiteSpace(direccion.Stat))
                        bp.Addresses.State = direccion.Stat;

                    if (!string.IsNullOrWhiteSpace(direccion.City))
                        bp.Addresses.City = direccion.City;

                    bp.Addresses.AddressType =
                        direccion.AddressType == "S"
                            ? BoAddressType.bo_ShipTo
                            : BoAddressType.bo_BillTo;

                    bp.Addresses.Add();
                }
            }

            int resultado = bp.Add();
            if (resultado != 0)
            {
                throw new Exception("Error creando cliente SAP: " + _company.GetLastErrorDescription());
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
