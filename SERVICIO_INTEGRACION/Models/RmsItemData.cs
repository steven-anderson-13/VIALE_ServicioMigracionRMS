using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SERVICIO_INTEGRACION.Models
{
    public class RmsItemData
    {
        // =====================================================
        // CAMPOS PRINCIPALES DEL ARTICULO
        // =====================================================

        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string U_VS_MODA_DSCP { get; set; }
        public string ItemsGroupCode { get; set; }

        // =====================================================
        // PROPIEDADES ESTANDAR SAP B1
        // =====================================================

        public string CostAccountingMethod { get; set; }
        public string ForceSelectionOfSerialNumber { get; set; }
        public string Frozen { get; set; }
        public string GLMethod { get; set; }
        public string InventoryItem { get; set; }
        public string InventoryUOM { get; set; }
        public string IssueMethod { get; set; }
        public string ItemType { get; set; }
        public string ManageBatchNumbers { get; set; }
        public string ManageSerialNumbersOnReleaseOnly { get; set; }
        public string ManageSerialNumbers { get; set; }
        public string ManageStockByWarehouse { get; set; }
        public string PlanningSystem { get; set; }
        public string ProcurementMethod { get; set; }
        public string PurchaseItem { get; set; }
        public string PurchaseUnit { get; set; }
        public decimal PurchaseItemsPerUnit { get; set; }
        public string SRIAndBatchManageMethod { get; set; }
        public string SalesItem { get; set; }
        public string SalesUnit { get; set; }
        public decimal SalesItemsPerUnit { get; set; }
        public string TaxType { get; set; }
        public string VatLiable { get; set; }
        public string WTLiable { get; set; }
        public string AssetItem { get; set; }

        // =====================================================
        // UDF - DATOS GENERALES
        // =====================================================

        public string U_BPP_TIPEXIST { get; set; }
        public string U_VS_COD_N2 { get; set; }
        public string U_BPP_TIPUNMED { get; set; }
        public string U_VS_AFPRCP { get; set; }

        public string BarCode { get; set; }

        public string U_ARGNS_ACT { get; set; }
        public string U_VS_MODA_SUNAT { get; set; }
        public string U_RMS { get; set; }
        public string U_VS_MODA_SKU { get; set; }

        // =====================================================
        // UDF - MODA / ESTILO
        // =====================================================

        public string U_VS_MODA_ESTILO { get; set; }
        public string U_VS_MODA_DEPARTMENT { get; set; }
        public string U_VS_MODA_MARCA { get; set; }
        public string U_VS_MODA_TIPO_MARCA { get; set; }
        public string U_VS_MODA_CLASE { get; set; }
        public string U_VS_MODA_DESIGN { get; set; }
        public string U_VS_MODA_CAT_EXH { get; set; }
        public string U_VS_MODA_GENERO { get; set; }
        public string U_VS_MODA_GEN_RESUMEN { get; set; }
        public string U_VS_MODA_SERIE { get; set; }
        public string U_VS_MODA_TALLA { get; set; }
        public string U_VS_MODA_CAPELLADA { get; set; }
        public string U_VS_MODA_SUBCAMPAIGN { get; set; }
        public string U_VS_MODA_LOOK { get; set; }
        public string U_VS_MODA_LINEA { get; set; }
        public string U_VS_MODA_CPLL_TXT1 { get; set; }
        public string U_VS_MODA_FORRO { get; set; }
        public string U_VS_MODA_CLR_FORRO { get; set; }
        public string U_VS_MODA_TIPO_PLTLL { get; set; }
        public string U_VS_MODA_PLANTILLA { get; set; }
        public string U_VS_MODA_CLR_PLNTLL { get; set; }
        public string U_VS_MODA_PLANTA { get; set; }
        public string U_VS_MODA_MATERIAL_PLANTA { get; set; }
        public string U_VS_MODA_ACABADO_PLT1 { get; set; }

        // =====================================================
        // UDF - ARGNS / CARACTERISTICAS
        // =====================================================

        public string U_ARGNS_TipodePlanta { get; set; }
        public string U_ARGNS_TPLF { get; set; }
        public string U_ARGNS_FB { get; set; }

        public string U_VS_MODA_ALT_TACO { get; set; }
        public string U_VS_MODA_ALT_PLATAFORMA { get; set; }
        public string U_VS_MODA_ALT_EFECTIVA { get; set; }
        public string U_ARGNS_RDT { get; set; }

        public string U_VS_MODA_H_CALZADA { get; set; }
        public string U_VS_MODA_TAMANO_CAJA { get; set; }

        // =====================================================
        // PESOS
        // =====================================================

        public string PurchaseWeightUnit { get; set; }
        public decimal InventoryWeight { get; set; }
        public decimal SalesUnitWeight { get; set; }

        // =====================================================
        // UDF - DATOS SUNAT / VIGENCIA / TIPO
        // =====================================================

        public string U_VS_MODA_T_TAX { get; set; }
        public string U_VS_MODA_VIGENCIA { get; set; }
        public string U_VS_MODA_T_ITEM { get; set; }

        // =====================================================
        // UDF - COLOR / TALLA
        // =====================================================

        public string U_ARGNS_COL { get; set; }
        public string U_ARGNS_TALLA { get; set; }


        public string U_VS_NO_DESCON { get; set; }
        public string U_VS_NO_INV { get; set; }
        public string U_VS_TIPO_CAPELLADA { get; set; }
        public string U_VS_COLOR_PLANTA { get; set; }
        public string GENERO_RESUMEN { get; set; }

        // ==========================
    }
}
