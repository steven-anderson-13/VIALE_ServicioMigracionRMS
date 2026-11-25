using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VIALE_ServicioMigracionRMS.Models;

namespace VIALE_ServicioMigracionRMS.Repositories
{
    public class RMSDownPaymentRepository
    {
        private readonly string _connectionString;

        public RMSDownPaymentRepository()
        {
            _connectionString = ConfigurationManager.AppSettings["Rms.ConnectionString"];
        }
        public List<RMSDownPaymentData> ObtenerAnticiposPendientes(DateTime fechaDesde, DateTime fechaHasta)
        {
            var lista = new List<RMSDownPaymentData>();

            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("SP_RMS_ObtenerAnticiposPendientes", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@FechaDesde", SqlDbType.Date).Value = fechaDesde.Date;
                cmd.Parameters.Add("@FechaHasta", SqlDbType.Date).Value = fechaHasta.Date;
                cn.Open();

                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new RMSDownPaymentData
                        {
                            StoreNo = Convert.ToInt32(dr["StoreNo"]),
                            ReceiptNo = Convert.ToInt32(dr["ReceiptNo"]),
                            ReceiptId = dr["ReceiptId"].ToString(),
                            CustomerNo = Convert.ToInt32(dr["CustomerNo"]),
                            //SalesDate = Convert.ToDateTime(dr["SalesDate"]),
                            //CurrencyId = Convert.ToInt32(dr["CurrencyId"]),
                            //DocNumber = dr["DocNumber"].ToString(),
                            //Flag1 = Convert.ToInt32(dr["Flag1"]),
                            //Notes = dr["Notes"] == DBNull.Value
                            //    ? ""
                            //    : dr["Notes"].ToString(),
                            PayTotal = dr["PayTotal"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(dr["PayTotal"]),
                            //FCPayTotal = dr["FCPayTotal"] == DBNull.Value
                            //    ? 0
                            //    : Convert.ToDecimal(dr["FCPayTotal"])
                        });
                    }
                }

                cn.Close();
            }

            return lista;
        }
        public RMSDownPaymentData ObtenerDocumento(int receiptNo, string receiptId)
        {
            RMSDownPaymentData documento = null;

            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("SP_RMS_ObtenerAnticipoDocumento", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@ReceiptNo", SqlDbType.Int).Value = receiptNo;
                cmd.Parameters.Add("@ReceiptId", SqlDbType.VarChar, 100).Value = receiptId;

                cn.Open();

                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        documento = new RMSDownPaymentData
                        {
                            StoreNo = Convert.ToInt32(dr["StoreNo"]),
                            ReceiptId = dr["ReceiptId"].ToString(),
                            ReceiptNo = Convert.ToInt32(dr["ReceiptNo"]),
                            CustomerNo = Convert.ToInt32(dr["CustomerNo"]),

                            NumAtCard = dr["NumAtCard"].ToString(),

                            DocDate = Convert.ToDateTime(dr["DocDate"]),
                            TaxDate = Convert.ToDateTime(dr["TaxDate"]),
                            DocDueDate = Convert.ToDateTime(dr["DocDueDate"]),

                            DocCurrency = dr["DocCurrency"].ToString(),
                            Comments = dr["Comments"].ToString(),

                            DocTypeSUNAT = dr["DocTypeSUNAT"].ToString(),
                            Serie = dr["Serie"].ToString(),
                            Correlativo = dr["Correlativo"].ToString(),

                            U_BPV_SERI = dr["U_BPV_SERI"].ToString(),
                            U_BPV_NCON2 = dr["U_BPV_NCON2"].ToString(),
                            JrnlMemo = dr["JrnlMemo"].ToString(),

                            U_CL_REFPA1 = dr["U_CL_REFPA1"].ToString(),
                            U_RMS_VALIDATX = dr["U_RMS_VALIDATX"].ToString(),

                            PayTotal = Convert.ToDecimal(dr["PayTotal"])
                        };
                    }
                }

                cn.Close();
            }

            if (documento == null)
                throw new Exception($"No se encontró la cabecera RMS, Anticipo ReceiptNo: {receiptNo}");

            return documento;
        }
        public List<RMSDownPaymentLineData> ObtenerDetalle(int receiptNo, string receiptId)
        {
            var lista = new List<RMSDownPaymentLineData>();

            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("SP_RMS_ObtenerAnticipoDetalle", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 120;

                cmd.Parameters.Add("@ReceiptNo", SqlDbType.Int).Value = receiptNo;
                cmd.Parameters.Add("@ReceiptId", SqlDbType.VarChar, 100).Value = receiptId;

                cn.Open();

                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new RMSDownPaymentLineData
                        {
                            ItemCode = dr["ItemCode"].ToString(),
                            LineNum = Convert.ToInt32(dr["LineNum"]),
                            Dscription = dr["Dscription"].ToString(),
                            Quantity = Convert.ToDecimal(dr["Quantity"]),
                            Price = Convert.ToDecimal(dr["Price"]),
                            DiscPrcnt = Convert.ToDecimal(dr["DiscPrcnt"]),
                            TaxCode = "IEX",
                            WhsCode = dr["WhsCode"] == DBNull.Value
                                ? ""
                                : dr["WhsCode"].ToString(),
                            AccountCode = dr["AccountCode"] == DBNull.Value
                                ? ""
                                : dr["AccountCode"].ToString(),
                            CostingCode = dr["CostingCode"] == DBNull.Value
                                ? ""
                                : dr["CostingCode"].ToString(),
                            CostingCode2 = dr["CostingCode2"] == DBNull.Value
                                ? ""
                                : dr["CostingCode2"].ToString(),
                            CostingCode3 = dr["CostingCode3"] == DBNull.Value
                                ? ""
                                : dr["CostingCode3"].ToString(),
                            CostingCode4 = dr["CostingCode4"] == DBNull.Value
                                ? ""
                                : dr["CostingCode4"].ToString(),
                            CostingCode5 = dr["CostingCode5"] == DBNull.Value
                                ? ""
                                : dr["CostingCode5"].ToString()
                        });
                    }
                }

                cn.Close();
            }

            return lista;
        }
        public RMSDownPaymentCustomerData ObtenerCliente(int receiptNo, string receiptId)
        {
            RMSDownPaymentCustomerData cliente = null;

            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("SP_RMS_ObtenerAnticipoCliente", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ReceiptNo", SqlDbType.Int).Value = receiptNo;
                cmd.Parameters.Add("@ReceiptId", SqlDbType.VarChar, 100).Value = receiptId;

                cn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        cliente = new RMSDownPaymentCustomerData
                        {
                            CardCode = dr["CardCode"]?.ToString(),
                            GroupCode = dr["GroupCode"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(dr["GroupCode"]),

                            CompanyName = dr["CompanyName"]?.ToString(),
                            FederalTaxId = dr["FederalTaxId"]?.ToString(),

                            FirstName = dr["CFirstName"]?.ToString(),
                            LastName = dr["CLastName"]?.ToString(),
                            MiddleName = dr["CMiddleName"]?.ToString(),

                            Email = dr["EmailAddress"]?.ToString(),
                            Phone1 = dr["CPhone1"]?.ToString(),
                            Phone2 = dr["CPhone2"]?.ToString(),

                            Address = dr["Street"]?.ToString(),
                            ZipCode = dr["ZipCode"]?.ToString(),

                            U_BPP_BPTD = dr["U_BPP_BPTD"]?.ToString(),
                            U_BPP_BPTP = dr["U_BPP_BPTP"]?.ToString(),

                            U_VS_NOMCOM = dr["U_VS_NOMCOM"]?.ToString(),
                            U_BPP_BPAP = dr["U_BPP_BPAP"]?.ToString(),
                            U_BPP_BPAM = dr["U_BPP_BPAM"]?.ToString(),
                            U_BPP_BPNO = dr["U_BPP_BPNO"]?.ToString(),
                            U_VS_EXCCOD = dr["U_VS_EXCCOD"]?.ToString()
                        };
                    }
                }

                cn.Close();
            }

            if (cliente == null)
                throw new Exception($"No se encontró el cliente RMS, Anticipo receiptNo: {receiptNo}");

            return cliente;
        }
        public List<RMSDownPaymentAddressData> ObtenerDirecciones(int receiptNo, string receiptId)
        {
            var direcciones = new List<RMSDownPaymentAddressData>();

            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("SP_RMS_ObtenerAnticipoDireccion", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ReceiptNo", SqlDbType.Int).Value = receiptNo;
                cmd.Parameters.Add("@ReceiptId", SqlDbType.VarChar, 100).Value = receiptId;
                cn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        direcciones.Add(new RMSDownPaymentAddressData
                        {
                            AddressName = dr["AddressName"]?.ToString(),
                            Giro = dr["Giro"]?.ToString(),
                            CCalle = dr["CCalle"]?.ToString(),
                            CCuadra = dr["CCuadra"]?.ToString(),

                            ZipCode = dr["ZipCode"]?.ToString(),
                            CRegion = dr["cRegion"]?.ToString(),
                            CCodPais = dr["CCodPais"]?.ToString(),

                            Street = dr["Street"]?.ToString(),
                            AddressName2 = dr["AddressName2"]?.ToString(),
                            AddressName3 = dr["AddressName3"]?.ToString(),

                            EmailAddress = dr["EmailAddress"]?.ToString(),
                            BilltoDefault = dr["BilltoDefault"]?.ToString(),
                            FederalTaxId = dr["FederalTaxId"]?.ToString(),
                            ShipToDefault = dr["ShipToDefault"]?.ToString(),

                            City = dr["City"]?.ToString(),
                            Country = dr["Country"]?.ToString(),
                            County = dr["County"]?.ToString(),
                            Stat = dr["stat"]?.ToString(),
                            TaxCode = dr["TaxCode"]?.ToString(),

                            AddressType = dr["AddressType"]?.ToString()
                        });
                    }
                }

                cn.Close();
            }

            return direcciones;
        }
        public List<RMSDownPaymentPaymentData> ObtenerPagos(int receiptNo, string receiptId)
        {
            var lista = new List<RMSDownPaymentPaymentData>();

            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("SP_RMS_ObtenerAnticipoPagos_nbk1", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@ReceiptNo", SqlDbType.Int).Value = "0";
                cmd.Parameters.Add("@ReceiptId", SqlDbType.VarChar, 100).Value = receiptId;

                cn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new RMSDownPaymentPaymentData
                        {
                            AccountCode = dr["cuenta"]?.ToString(),
                            Currency = dr["Currency"]?.ToString(),

                            TaxDate = Convert.ToDateTime(dr["TaxDate"]),
                            DocDate = Convert.ToDateTime(dr["DocDate"]),
                            DueDate = Convert.ToDateTime(dr["FecVence"]),

                            TenderId = Convert.ToInt32(dr["TenderId"]),

                            Voucher = dr["Vaucher"]?.ToString(),
                            CrTypeCode = dr["CrTypeCode"]?.ToString(),
                            CardNumber = dr["cardNumber"]?.ToString(),

                            PayTotal = dr["PayTotal"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(dr["PayTotal"]),

                            PayTotalFC = dr["PayTotalFC"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(dr["PayTotalFC"]),

                            TransferRef = dr["TransferRef"]?.ToString(),

                            CardExpDate = dr["CardExpDate"]?.ToString(),
                            CardAuthorization = dr["CardAuthorization"]?.ToString(),
                            CardHolderName = dr["CardHolderName"]?.ToString(),

                            Quotas = dr["Quotas"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(dr["Quotas"]),

                            CodigoAut = dr["CodigoAut"]?.ToString(),
                            CodigoBanco = dr["CodigoBanco"]?.ToString(),

                            RutCheque = dr["RutCheque"]?.ToString(),
                            NumCheque = dr["NumCheque"]?.ToString(),
                            CtaCte = dr["CtaCte"]?.ToString(),
                            SucBco = dr["SucBco"]?.ToString(),

                            CountryCode = dr["CountryCode"]?.ToString(),

                            MPPG = dr["MPPG"]?.ToString(),
                            TPOOPERI = dr["TPOOPERI"]?.ToString(),
                            NCUserType = dr["NCUserType"]?.ToString(),

                            CreditCardStoreNo = dr["CreditCardStoreNo"]?.ToString(),

                            U_BPP_EFEC = dr["U_BPP_EFEC"]?.ToString(),
                            U_BPP_TARJ = dr["U_BPP_TARJ"]?.ToString(),
                            U_BPP_TRAN = dr["U_BPP_TRAN"]?.ToString(),
                            U_BPP_CHEQ = dr["U_BPP_CHEQ"]?.ToString(),
                            U_BPP_LETR = dr["U_BPP_LETR"]?.ToString(),

                            U_VS_PRECIO_DOLARES = dr["U_VS_PRECIO_DOLARES"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(dr["U_VS_PRECIO_DOLARES"]),

                            U_CL_NROVAL = dr["U_CL_NROVAL"]?.ToString(),
                            ForeignPay = dr["ForeignPay"]?.ToString(),
                            JournalRemarks = dr["JournalRemarks"]?.ToString()
                        });
                    }
                }

                cn.Close();

            }

            if (lista.Count == 0)
                throw new Exception(
                    $"No se encontraron medios de pago RMS, Anticipo ReceiptNo: {receiptNo}");

            return lista;
        }


    }
}
