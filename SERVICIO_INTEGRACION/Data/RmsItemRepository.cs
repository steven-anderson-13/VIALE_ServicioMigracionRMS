using SERVICIO_INTEGRACION.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SERVICIO_INTEGRACION.Data
{
    public class RmsItemRepository
    {
        private readonly string _connectionString;

        public RmsItemRepository()
        {
            _connectionString = ConfigurationManager.AppSettings["Rms.ConnectionString"];
        }

        // =====================================================
        // OBTENER ARTICULOS POR SKU
        // Devuelve todos los hijos del estilo/color.
        // =====================================================

        public List<RmsItemData> ObtenerArticulosPorSKU(string sku)
        {
            List<RmsItemData> articulos = new List<RmsItemData>();

            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("SP_RMS_ObtenerArticulosPorSKU", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@SKU", SqlDbType.VarChar, 50).Value = sku;

                cn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        articulos.Add(MapearArticulo(reader));
                    }
                }

                cn.Close();
            }

            return articulos;
        }

        // =====================================================
        // MAPEO COMPLETO DEL RESULTADO RMS
        // =====================================================

        private RmsItemData MapearArticulo(SqlDataReader r)
        {
            return new RmsItemData
            {
                // DATOS PRINCIPALES
                ItemCode = GetString(r, "ItemCode"),
                ItemName = GetString(r, "ItemName"),
                U_VS_MODA_DSCP = GetString(r, "U_VS_MODA_DSCP"),
                ItemsGroupCode = GetString(r, "ItemsGroupCode"),

                // PROPIEDADES SAP
                CostAccountingMethod = GetString(r, "CostAccountingMethod"),
                ForceSelectionOfSerialNumber = GetString(r, "ForceSelectionOfSerialNumber"),
                Frozen = GetString(r, "Frozen"),
                GLMethod = GetString(r, "GLMethod"),
                InventoryItem = GetString(r, "InventoryItem"),
                InventoryUOM = GetString(r, "InventoryUOM"),
                IssueMethod = GetString(r, "IssueMethod"),
                ItemType = GetString(r, "ItemType"),
                ManageBatchNumbers = GetString(r, "ManageBatchNumbers"),
                ManageSerialNumbersOnReleaseOnly = GetString(r, "ManageSerialNumbersOnReleaseOnly"),
                ManageSerialNumbers = GetString(r, "ManageSerialNumbers"),
                ManageStockByWarehouse = GetString(r, "ManageStockByWarehouse"),
                PlanningSystem = GetString(r, "PlanningSystem"),
                ProcurementMethod = GetString(r, "ProcurementMethod"),
                PurchaseItem = GetString(r, "PurchaseItem"),
                PurchaseUnit = GetString(r, "PurchaseUnit"),
                PurchaseItemsPerUnit = GetDecimal(r, "PurchaseItemsPerUnit"),
                SRIAndBatchManageMethod = GetString(r, "SRIAndBatchManageMethod"),
                SalesItem = GetString(r, "SalesItem"),
                SalesUnit = GetString(r, "SalesUnit"),
                SalesItemsPerUnit = GetDecimal(r, "SalesItemsPerUnit"),
                TaxType = GetString(r, "TaxType"),
                VatLiable = GetString(r, "VatLiable"),
                WTLiable = GetString(r, "WTLiable"),
                AssetItem = GetString(r, "AssetItem"),

                // UDF GENERALES
                U_BPP_TIPEXIST = GetString(r, "U_BPP_TIPEXIST"),
                U_VS_COD_N2 = GetString(r, "U_VS_COD_N2"),
                U_BPP_TIPUNMED = GetString(r, "U_BPP_TIPUNMED"),
                U_VS_AFPRCP = GetString(r, "U_VS_AFPRCP"),
                BarCode = GetString(r, "BarCode"),
                U_ARGNS_ACT = GetString(r, "U_ARGNS_ACT"),
                U_VS_MODA_SUNAT = GetString(r, "U_VS_MODA_SUNAT"),
                U_RMS = GetString(r, "U_RMS"),
                U_VS_MODA_SKU = GetString(r, "U_VS_MODA_SKU"),

                // UDF MODA
                U_VS_MODA_ESTILO = GetString(r, "U_VS_MODA_ESTILO"),
                U_VS_MODA_DEPARTMENT = GetString(r, "U_VS_MODA_DEPARTMENT"),
                U_VS_MODA_MARCA = GetString(r, "U_VS_MODA_MARCA"),
                U_VS_MODA_TIPO_MARCA = GetString(r, "U_VS_MODA_TIPO_MARCA"),
                U_VS_MODA_CLASE = GetString(r, "U_VS_MODA_CLASE"),
                U_VS_MODA_DESIGN = GetString(r, "U_VS_MODA_DESIGN"),
                U_VS_MODA_CAT_EXH = GetString(r, "U_VS_MODA_CAT_EXH"),
                U_VS_MODA_GENERO = GetString(r, "U_VS_MODA_GENERO"),
                U_VS_MODA_GEN_RESUMEN = GetString(r, "U_VS_MODA_GEN_RESUMEN"),
                U_VS_MODA_SERIE = GetString(r, "U_VS_MODA_SERIE"),
                U_VS_MODA_TALLA = GetString(r, "U_VS_MODA_TALLA"),
                U_VS_MODA_CAPELLADA = GetString(r, "U_VS_MODA_CAPELLADA"),
                U_VS_MODA_SUBCAMPAIGN = GetString(r, "U_VS_MODA_SUBCAMPAIGN"),
                U_VS_MODA_LOOK = GetString(r, "U_VS_MODA_LOOK"),
                U_VS_MODA_LINEA = GetString(r, "U_VS_MODA_LINEA"),
                U_VS_MODA_CPLL_TXT1 = GetString(r, "U_VS_MODA_CPLL_TXT1"),
                U_VS_MODA_FORRO = GetString(r, "U_VS_MODA_FORRO"),
                U_VS_MODA_CLR_FORRO = GetString(r, "U_VS_MODA_CLR_FORRO"),
                U_VS_MODA_TIPO_PLTLL = GetString(r, "U_VS_MODA_TIPO_PLTLL"),
                U_VS_MODA_PLANTILLA = GetString(r, "U_VS_MODA_PLANTILLA"),
                U_VS_MODA_CLR_PLNTLL = GetString(r, "U_VS_MODA_CLR_PLNTLL"),
                U_VS_MODA_PLANTA = GetString(r, "U_VS_MODA_PLANTA"),
                U_VS_MODA_MATERIAL_PLANTA = GetString(r, "U_VS_MODA_MATERIAL_PLANTA"),
                U_VS_MODA_ACABADO_PLT1 = GetString(r, "U_VS_MODA_ACABADO_PLT1"),

                // UDF ARGNS
                U_ARGNS_TipodePlanta = GetString(r, "U_ARGNS_TipodePlanta"),
                U_ARGNS_TPLF = GetString(r, "U_ARGNS_TPLF"),
                U_ARGNS_FB = GetString(r, "U_ARGNS_FB"),
                U_VS_MODA_ALT_TACO = GetString(r, "U_VS_MODA_ALT_TACO"),
                U_VS_MODA_ALT_PLATAFORMA = GetString(r, "U_VS_MODA_ALT_PLATAFORMA"),
                U_VS_MODA_ALT_EFECTIVA = GetString(r, "U_VS_MODA_ALT_EFECTIVA"),
                U_ARGNS_RDT = GetString(r, "U_ARGNS_RDT"),
                U_VS_MODA_H_CALZADA = GetString(r, "U_VS_MODA_H_CALZADA"),
                U_VS_MODA_TAMANO_CAJA = GetString(r, "U_VS_MODA_TAMANO_CAJA"),

                // PESOS
                PurchaseWeightUnit = GetString(r, "PurchaseWeightUnit"),
                InventoryWeight = GetDecimal(r, "InventoryWeight"),
                SalesUnitWeight = GetDecimal(r, "SalesUnitWeight"),

                // SUNAT / VIGENCIA / TIPO
                U_VS_MODA_T_TAX = GetString(r, "U_VS_MODA_T_TAX"),
                U_VS_MODA_VIGENCIA = GetString(r, "U_VS_MODA_VIGENCIA"),
                U_VS_MODA_T_ITEM = GetString(r, "U_VS_MODA_T_ITEM"),

                // COLOR / TALLA
                U_ARGNS_COL = GetString(r, "U_ARGNS_COL"),
                U_ARGNS_TALLA = GetString(r, "U_ARGNS_TALLA"),

                // CONTROL INVENTARIO
                U_VS_NO_DESCON = GetString(r, "U_VS_NO_DESCON"),
                U_VS_NO_INV = GetString(r, "U_VS_NO_INV"),
                U_VS_TIPO_CAPELLADA = GetString(r, "U_VS_TIPO_CAPELLADA"),
                U_VS_COLOR_PLANTA = GetString(r, "U_VS_COLOR_PLANTA"),

                // GENERO RESUMEN
                GENERO_RESUMEN = GetString(r, "GENERO_RESUMEN")
            };
        }

        // =====================================================
        // HELPERS PARA NULL / DBNull
        // =====================================================

        private string GetString(SqlDataReader reader, string column)
        {
            int index = reader.GetOrdinal(column);

            if (reader.IsDBNull(index))
                return string.Empty;

            return Convert.ToString(reader.GetValue(index))
                .Trim();
        }

        private decimal GetDecimal(SqlDataReader reader, string column)
        {
            int index = reader.GetOrdinal(column);

            if (reader.IsDBNull(index))
                return 0m;

            return Convert.ToDecimal(
                reader.GetValue(index));
        }

        public void RegistrarResultadoArticulo(string sku, string itemCode, string itemCodePadre, string estado, string mensaje)
        {
            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("SP_RMS_RegistrarResultadoArticulo", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@SKU", SqlDbType.VarChar, 50).Value = sku;
                cmd.Parameters.Add("@ItemCode", SqlDbType.VarChar, 50).Value = (object)itemCode ?? DBNull.Value;
                cmd.Parameters.Add("@ItemCodePadre", SqlDbType.VarChar, 50).Value = (object)itemCodePadre ?? DBNull.Value;
                cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 20).Value = estado;
                cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, -1).Value = (object)mensaje ?? DBNull.Value;

                cn.Open();
                cmd.ExecuteNonQuery();
                cn.Close();
            }

        }

    }
}
