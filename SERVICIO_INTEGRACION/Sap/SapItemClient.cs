using SAPbobsCOM;
using SERVICIO_INTEGRACION.Data;
using SERVICIO_INTEGRACION.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SERVICIO_INTEGRACION.Sap
{
    public class SapItemClient
    {
        private readonly Company _company;
        private readonly RmsItemRepository _rmsRepository;

        public SapItemClient(Company company)
        {
            _company = company;
            _rmsRepository = new RmsItemRepository();
        }

        // =====================================================
        // 1. BUSCAR ARTICULO SAP POR SKU
        // =====================================================

        public string ObtenerItemCodePorSKU(string sku)
        {
            Recordset rs = null;

            try
            {
                rs = (Recordset)_company.GetBusinessObject(
                    BoObjectTypes.BoRecordset);

                string skuSeguro = sku.Replace("'", "''");

                string sql = @"
                    SELECT ""ItemCode""
                    FROM ""OITM""
                    WHERE ""CodeBars"" = '" + skuSeguro + @"'
                      AND ""InvntItem"" = 'Y'";

                rs.DoQuery(sql);

                if (!rs.EoF)
                {
                    return Convert.ToString(
                        rs.Fields.Item("ItemCode").Value);
                }

                return null;
            }
            finally
            {
                LiberarCOM(rs);
            }
        }

        // =====================================================
        // 2. RESOLVER GRUPO DE ARTICULOS
        // =====================================================

        private int ObtenerItemsGroupCode(string nombreGrupoRMS, string generoResumen)
        {
            Recordset rs = null;

            try
            {
                rs = (Recordset)_company.GetBusinessObject(
                    BoObjectTypes.BoRecordset);

                string grupo = nombreGrupoRMS.Replace("'", "''");
                string genero = generoResumen.Replace("'", "''");

                string sql = $@"
            SELECT T0.""U_ItmsGrpCod""
            FROM ""@VIA_ARTGRUP"" T0
            WHERE T0.""U_NombreGrupoRMS"" = '{grupo}'
              AND (
                    SELECT COUNT(*)
                    FROM ""@VIA_ARTGRUP"" T1
                    WHERE T1.""U_NombreGrupoRMS"" = T0.""U_NombreGrupoRMS""
                  ) = 1
               OR (
                    T0.""U_NombreGrupoRMS"" = '{grupo}'
                    AND UPPER(T0.""U_ItmsGrpNam"") LIKE
                        '%' || UPPER('{genero}') || '%'
                  )";

                rs.DoQuery(sql);

                if (rs.RecordCount == 0)
                {
                    throw new Exception(
                        $"No existe configuración de grupo SAP para RMS: " +
                        $"{nombreGrupoRMS}, género: {generoResumen}");
                }

                return Convert.ToInt32(rs.Fields.Item(0).Value);
            }
            finally
            {
                LiberarCOM(rs);
            }
        }

        // =====================================================
        // 3. PROCESAR SKU SOLICITADO
        // =====================================================

        public string ObtenerOCrearArticulo(string sku)
        {
            string itemCodeSAP = null;
            string itemCodePadre = null;

            try
            {
                // Primero buscamos directamente en SAP.
                //string itemCodeSAP = ObtenerItemCodePorSKU(sku);
                itemCodeSAP = ObtenerItemCodePorSKU(sku);


                if (!string.IsNullOrWhiteSpace(itemCodeSAP))
                {
                    // Artículo ya existente en SAP
                    itemCodePadre = ObtenerItemCodePadre(sku);

                    _rmsRepository.RegistrarResultadoArticulo(sku, itemCodeSAP, itemCodePadre, "OK", "El artículo ya existía en SAP.");

                    return itemCodeSAP;
                }


                // El SKU no existe: obtener estilo/color/tallas RMS.
                List<RmsItemData> articulos = _rmsRepository.ObtenerArticulosPorSKU(sku);

                if (articulos == null || articulos.Count == 0)
                {
                    throw new Exception("No se encontraron datos RMS para el SKU: " + sku);
                }

                // Obtener código del padre
                itemCodePadre = articulos[0].U_VS_MODA_ESTILO;

                // 3. Registrar inicio del procesamiento
                _rmsRepository.RegistrarResultadoArticulo(sku, null, itemCodePadre, "PROCESANDO", "Iniciando creación de artículo.");


                // Crear padre e hijos faltantes.
                CrearArticulosFaltantes(articulos);

                // Volver a consultar el SKU original.
                itemCodeSAP = ObtenerItemCodePorSKU(sku);

                if (string.IsNullOrWhiteSpace(itemCodeSAP))
                {
                    throw new Exception(
                        "No se pudo recuperar el ItemCode SAP " +
                        "del SKU después de crearlo: " + sku);
                }

                //// 6. Registrar resultado exitoso
                //_rmsRepository.RegistrarResultadoArticulo(sku, itemCodeSAP, itemCodePadre, "OK", "Artículo creado correctamente.");

                return itemCodeSAP;
            }
            catch (Exception ex)
            {
                // 7. Registrar error
                try
                {
                    _rmsRepository.RegistrarResultadoArticulo(sku, itemCodeSAP, itemCodePadre, "ERROR", ex.Message);
                }
                catch (Exception exLog)
                {
                    // No ocultar el error original si falla el log
                    throw new Exception(
                        $"Error procesando SKU {sku}: {ex.Message}. " +
                        $"Además, falló el registro del log: {exLog.Message}",
                        ex);
                }

                throw;
            }
        }

        private string ObtenerItemCodePadre(string sku)
        {
            Recordset rs = null;

            try
            {
                // 1. Obtener el estilo asociado al SKU hijo
                string sql = $@"
            SELECT T0.""U_VS_MODA_ESTILO""
            FROM ""OITM"" T0
            WHERE T0.""CodeBars"" = '{sku.Replace("'", "''")}'
              AND T0.""InvntItem"" = 'Y'";

                rs = (Recordset)_company.GetBusinessObject(
                    BoObjectTypes.BoRecordset);

                rs.DoQuery(sql);

                if (rs.RecordCount == 0)
                    return null;

                string itemCodePadre =
                    Convert.ToString(rs.Fields.Item(0).Value);

                return itemCodePadre;
            }
            finally
            {
                LiberarCOM(rs);
            }
        }

        // =====================================================
        // 4. CREAR ARTICULOS FALTANTES
        // =====================================================

        public void CrearArticulosFaltantes(List<RmsItemData> articulos)
        {
            if (articulos == null || articulos.Count == 0)
                return;

            // Obtener serie completa de tallas
            string serieTallas = ObtenerSerieTallas(articulos);

            RmsItemData primerArticulo = articulos[0];

            // El padre corresponde al estilo.
            string itemCodePadre = primerArticulo.U_VS_MODA_ESTILO;

            if (string.IsNullOrWhiteSpace(itemCodePadre))
            {
                throw new Exception(
                    "El estilo del artículo está vacío.");
            }

            // Crear padre solamente si no existe.
            if (!ExisteItemCode(itemCodePadre))
            {
                CrearArticuloPadre(primerArticulo, serieTallas);
            }

            // Crear cada hijo devuelto por RMS.
            foreach (RmsItemData articulo in articulos)
            {
                string existente = ObtenerItemCodePorSKU(articulo.BarCode);

                if (!string.IsNullOrWhiteSpace(existente))
                    continue;

                if (ExisteItemCode(articulo.ItemCode))
                    continue;

                CrearArticuloHijo(articulo, serieTallas);
            }
        }
        private string ObtenerSerieTallas(List<RmsItemData> articulos)
        {
            var tallas = articulos
                .Select(x => x.U_ARGNS_TALLA)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => Convert.ToInt32(x))
                .ToList();

            return string.Join("/", tallas);
        }

        // =====================================================
        // 5. VALIDAR ITEMCODE
        // =====================================================

        private bool ExisteItemCode(string itemCode)
        {
            Items item = null;

            try
            {
                item = (Items)_company.GetBusinessObject(BoObjectTypes.oItems);

                return item.GetByKey(itemCode);
            }
            finally
            {
                LiberarCOM(item);
            }
        }

        // =====================================================
        // 6. CREAR ARTICULO PADRE
        // =====================================================

        private void CrearArticuloPadre(RmsItemData data, string serieTallas)
        {
            Items item = null;

            try
            {
                item = (Items)_company.GetBusinessObject(BoObjectTypes.oItems);
                item.ItemCode = data.U_VS_MODA_ESTILO;
                //item.ItemName = data.ItemName;
                item.ItemName = data.U_VS_MODA_ESTILO;
                item.ItemsGroupCode = ObtenerItemsGroupCode(data.ItemsGroupCode, data.GENERO_RESUMEN);

                // El padre no es un SKU de talla.
                item.BarCode = "";

                // Propiedades generales.
                ConfigurarPropiedades(item, data);

                // Padre.
                SetUDF(item, "U_VS_MODA_SKU", "Y");

                // Serie de tallas existentes
                SetUDF(item, "U_VS_MODA_SERIE", serieTallas);

                // UDF restantes compartidos.
                ConfigurarUDF(item, data);

                // Reforzar valores específicos del padre
                //SetUDF(item, "BarCode", "");
                SetUDF(item, "U_VS_MODA_SKU", "Y");
                SetUDF(item, "U_VS_MODA_SERIE", serieTallas);

                SetUDF(item, "U_ARGNS_COL", "");
                SetUDF(item, "U_ARGNS_TALLA", "");

                int resultado = item.Add();

                if (resultado != 0)
                    LanzarErrorSAP("Crear artículo padre " + data.U_VS_MODA_ESTILO);

                // LOG OK
                _rmsRepository.RegistrarResultadoArticulo(data.U_VS_MODA_ESTILO, data.U_VS_MODA_ESTILO, data.U_VS_MODA_ESTILO, "OK", "Artículo padre creado correctamente.");
            }
            catch (Exception ex)
            {
                // LOG ERROR
                _rmsRepository.RegistrarResultadoArticulo(data.U_VS_MODA_ESTILO, data.U_VS_MODA_ESTILO, data.U_VS_MODA_ESTILO, "ERROR", "Error creando artículo padre: " + ex.Message);
                EnviarAlertaError($"Articulo Padre {data.U_VS_MODA_ESTILO}", $"Generar Articulo padre: {data.ItemName}", ex.Message.ToString());

                throw;
            }
            finally
            {
                LiberarCOM(item);
            }
        }

        // =====================================================
        // 7. CREAR ARTICULO HIJO
        // =====================================================

        private void CrearArticuloHijo(RmsItemData data, string serieTallas)
        {
            Items item = null;

            try
            {
                item = (Items)_company.GetBusinessObject(BoObjectTypes.oItems);

                //item.ItemCode = data.ItemCode;
                item.ItemCode = data.ItemName;
                item.ItemName = data.ItemName;
                item.ItemsGroupCode = ObtenerItemsGroupCode(data.ItemsGroupCode, data.GENERO_RESUMEN);
                item.BarCode = data.BarCode;

                ConfigurarPropiedades(item, data);

                // Hijo.
                SetUDF(item, "U_VS_MODA_SKU", "N");
                // Serie completa de tallas del estilo
                SetUDF(item, "U_VS_MODA_SERIE", serieTallas);

                ConfigurarUDF(item, data);

                SetUDF(item, "U_VS_MODA_SKU", "N");
                SetUDF(item, "U_VS_MODA_SERIE", serieTallas);

                int resultado = item.Add();

                if (resultado != 0)
                    LanzarErrorSAP("Crear artículo hijo " + data.ItemCode);

                // SAP creó correctamente el artículo
                ActualizarArticuloALU(data.BarCode, data.ItemName);

                // LOG OK
                _rmsRepository.RegistrarResultadoArticulo(data.BarCode, data.ItemName, data.U_VS_MODA_ESTILO, "OK", "Artículo hijo creado correctamente.");
            }
            catch (Exception ex)
            {
                // LOG ERROR
                _rmsRepository.RegistrarResultadoArticulo(data.BarCode, data.ItemName, data.U_VS_MODA_ESTILO, "ERROR", "Error creando artículo hijo: " + ex.Message);
                EnviarAlertaError($"SKU {data.BarCode}", $"Generar Articulo hijo: {data.ItemName}", ex.Message.ToString());

                throw;
            }
            finally
            {
                LiberarCOM(item);
            }
        }
        public void ActualizarArticuloALU(string sku, string ALU)
        {
            using (SqlConnection cn = new SqlConnection(ConfigurationManager.AppSettings["Rms.ConnectionString"]))
            using (SqlCommand cmd = new SqlCommand(@"
            UPDATE [RETAILDATA].[DBO].PRODUCT
            SET ALU = @ALU
            WHERE SKU = @SKU", cn))
            {
                cmd.Parameters.Add("@SKU", SqlDbType.VarChar).Value = sku;
                cmd.Parameters.Add("@ALU", SqlDbType.VarChar).Value = ALU;

                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // =====================================================
        // 8. PROPIEDADES ESTANDAR
        // =====================================================

        private void ConfigurarPropiedades(Items item, RmsItemData data)
        {
            item.InventoryItem = BoYesNoEnum.tYES;
            item.PurchaseItem = BoYesNoEnum.tYES;
            item.SalesItem = BoYesNoEnum.tYES;
            item.InventoryUOM = data.InventoryUOM;
            item.PurchaseUnit = data.PurchaseUnit;
            item.SalesUnit = data.SalesUnit;
            item.PurchaseItemsPerUnit = (double)data.PurchaseItemsPerUnit;
            item.SalesItemsPerUnit = (double)data.SalesItemsPerUnit;
            item.InventoryWeight = (double)data.InventoryWeight;
            item.SalesUnitWeight = (double)data.SalesUnitWeight;

            item.PurchaseWeightUnit = int.TryParse(data.PurchaseWeightUnit, out int weightUnit) ? weightUnit : 1;

            item.Frozen =
                data.Frozen == "Y"
                    ? BoYesNoEnum.tYES
                    : BoYesNoEnum.tNO;

            item.ManageBatchNumbers =
                data.ManageBatchNumbers == "Y"
                    ? BoYesNoEnum.tYES
                    : BoYesNoEnum.tNO;

            item.ManageSerialNumbers =
                data.ManageSerialNumbers == "Y"
                    ? BoYesNoEnum.tYES
                    : BoYesNoEnum.tNO;

            item.ManageSerialNumbersOnReleaseOnly =
                data.ManageSerialNumbersOnReleaseOnly == "Y"
                    ? BoYesNoEnum.tYES
                    : BoYesNoEnum.tNO;

            // Forzar selección de número de serie
            item.ForceSelectionOfSerialNumber =
                data.ForceSelectionOfSerialNumber == "Y"
                    ? BoYesNoEnum.tYES
                    : BoYesNoEnum.tNO;

            item.ManageStockByWarehouse =
                data.ManageStockByWarehouse == "Y"
                    ? BoYesNoEnum.tYES
                    : BoYesNoEnum.tNO;

            item.VatLiable =
                data.VatLiable == "Y"
                    ? BoYesNoEnum.tYES
                    : BoYesNoEnum.tNO;

            item.WTLiable =
                data.WTLiable == "Y"
                    ? BoYesNoEnum.tYES
                    : BoYesNoEnum.tNO;

            item.AssetItem =
                data.AssetItem == "Y"
                    ? BoYesNoEnum.tYES
                    : BoYesNoEnum.tNO;
        }

        // =====================================================
        // 9. ASIGNAR TODOS LOS UDF
        // =====================================================

        private void ConfigurarUDF(Items item, RmsItemData d)
        {
            SetUDF(item, "U_VS_MODA_DSCP", d.U_VS_MODA_DSCP);
            SetUDF(item, "U_BPP_TIPEXIST", d.U_BPP_TIPEXIST);
            SetUDF(item, "U_VS_COD_N2", d.U_VS_COD_N2);
            SetUDF(item, "U_BPP_TIPUNMED", d.U_BPP_TIPUNMED);
            SetUDF(item, "U_VS_AFPRCP", d.U_VS_AFPRCP);

            SetUDF(item, "U_ARGNS_ACT", d.U_ARGNS_ACT);
            SetUDF(item, "U_VS_MODA_SUNAT", d.U_VS_MODA_SUNAT);
            SetUDF(item, "U_RMS", d.U_RMS);

            SetUDF(item, "U_VS_MODA_ESTILO", d.U_VS_MODA_ESTILO);
            SetUDF(item, "U_VS_MODA_DEPARTMENT", d.U_VS_MODA_DEPARTMENT);
            SetUDF(item, "U_VS_MODA_MARCA", d.U_VS_MODA_MARCA);
            SetUDF(item, "U_VS_MODA_TIPO_MARCA", d.U_VS_MODA_TIPO_MARCA);
            SetUDF(item, "U_VS_MODA_CLASE", d.U_VS_MODA_CLASE);
            SetUDF(item, "U_VS_MODA_DESIGN", d.U_VS_MODA_DESIGN);
            SetUDF(item, "U_VS_MODA_CAT_EXH", d.U_VS_MODA_CAT_EXH);
            SetUDF(item, "U_VS_MODA_GENERO", d.U_VS_MODA_GENERO);
            SetUDF(item, "U_VS_MODA_GEN_RESUMEN", d.U_VS_MODA_GEN_RESUMEN);
            SetUDF(item, "U_VS_MODA_SERIE", d.U_VS_MODA_SERIE);
            SetUDF(item, "U_VS_MODA_TALLA", d.U_VS_MODA_TALLA);
            SetUDF(item, "U_VS_MODA_CAPELLADA", d.U_VS_MODA_CAPELLADA);
            SetUDF(item, "U_VS_MODA_SUBCAMPAIGN", d.U_VS_MODA_SUBCAMPAIGN);
            SetUDF(item, "U_VS_MODA_LOOK", d.U_VS_MODA_LOOK);
            SetUDF(item, "U_VS_MODA_LINEA", d.U_VS_MODA_LINEA);
            SetUDF(item, "U_VS_MODA_CPLL_TXT1", d.U_VS_MODA_CPLL_TXT1);
            SetUDF(item, "U_VS_MODA_FORRO", d.U_VS_MODA_FORRO);
            SetUDF(item, "U_VS_MODA_CLR_FORRO", d.U_VS_MODA_CLR_FORRO);
            SetUDF(item, "U_VS_MODA_TIPO_PLTLL", d.U_VS_MODA_TIPO_PLTLL);
            SetUDF(item, "U_VS_MODA_PLANTILLA", d.U_VS_MODA_PLANTILLA);
            SetUDF(item, "U_VS_MODA_CLR_PLNTLL", d.U_VS_MODA_CLR_PLNTLL);
            SetUDF(item, "U_VS_MODA_PLANTA", d.U_VS_MODA_PLANTA);
            SetUDF(item, "U_VS_MODA_MATERIAL_PLANTA", d.U_VS_MODA_MATERIAL_PLANTA);
            SetUDF(item, "U_VS_MODA_ACABADO_PLT1", d.U_VS_MODA_ACABADO_PLT1);

            SetUDF(item, "U_ARGNS_TipodePlanta", d.U_ARGNS_TipodePlanta);
            SetUDF(item, "U_ARGNS_TPLF", d.U_ARGNS_TPLF);
            SetUDF(item, "U_ARGNS_FB", d.U_ARGNS_FB);

            SetUDF(item, "U_VS_MODA_ALT_TACO", d.U_VS_MODA_ALT_TACO);
            SetUDF(item, "U_VS_MODA_ALT_PLATAFORMA", d.U_VS_MODA_ALT_PLATAFORMA);
            SetUDF(item, "U_VS_MODA_ALT_EFECTIVA", d.U_VS_MODA_ALT_EFECTIVA);
            SetUDF(item, "U_ARGNS_RDT", d.U_ARGNS_RDT);

            SetUDF(item, "U_VS_MODA_H_CALZADA", d.U_VS_MODA_H_CALZADA);
            SetUDF(item, "U_VS_MODA_TAMANO_CAJA", d.U_VS_MODA_TAMANO_CAJA);

            SetUDF(item, "U_VS_MODA_T_TAX", d.U_VS_MODA_T_TAX);
            SetUDF(item, "U_VS_MODA_VIGENCIA", d.U_VS_MODA_VIGENCIA);
            SetUDF(item, "U_VS_MODA_T_ITEM", d.U_VS_MODA_T_ITEM);

            SetUDF(item, "U_ARGNS_COL", d.U_ARGNS_COL);
            SetUDF(item, "U_ARGNS_TALLA", d.U_ARGNS_TALLA);

            SetUDF(item, "U_VS_NO_DESCON", d.U_VS_NO_DESCON);
            SetUDF(item, "U_VS_NO_INV", d.U_VS_NO_INV);
            SetUDF(item, "U_VS_TIPO_CAPELLADA", d.U_VS_TIPO_CAPELLADA);
            SetUDF(item, "U_VS_COLOR_PLANTA", d.U_VS_COLOR_PLANTA);
        }

        // =====================================================
        // 10. HELPERS
        // =====================================================

        private void SetUDF(Items item, string nombre, object valor)
        {
            if (valor == null)
                valor = string.Empty;

            item.UserFields.Fields.Item(nombre).Value = valor;
        }

        private void LanzarErrorSAP(string operacion)
        {
            int codigo;
            string mensaje;

            _company.GetLastError(out codigo, out mensaje);

            throw new Exception(operacion + " - SAP " + codigo + ": " + mensaje);
        }

        private void LiberarCOM(object objeto)
        {
            if (objeto != null &&
                Marshal.IsComObject(objeto))
            {
                Marshal.ReleaseComObject(objeto);
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
                //string correoCC = ConfigurationManager.AppSettings["Mail.CC"];

                using (MailMessage correo = new MailMessage())
                {
                    correo.From = new MailAddress(smtpUser);
                    correo.To.Add(correoDestino);

                    //if (!string.IsNullOrWhiteSpace(correoCC))
                    //    correo.CC.Add(correoCC);

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
                                                            Aticulo
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
