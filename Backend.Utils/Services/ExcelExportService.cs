using System.Reflection;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Shared.Models.Export;
using Shared.Models.DTOs.Auth;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;

namespace Backend.Utils.Services
{
    /// <summary>
    /// Servicio para exportación de datos a Excel usando ClosedXML
    /// </summary>
    public class ExcelExportService<T> where T : class
    {
        private readonly BaseQueryService<T> _queryService;
        private readonly ILogger<ExcelExportService<T>> _logger;

        public ExcelExportService(BaseQueryService<T> queryService, ILogger<ExcelExportService<T>> logger)
        {
            _queryService = queryService;
            _logger = logger;
        }

        /// <summary>
        /// Exporta datos a Excel basado en ExcelExportRequest
        /// </summary>
        public async Task<byte[]> ExportToExcelAsync(ExcelExportRequest request, SessionDataDto sessionData)
        {
            try
            {
                _logger.LogInformation($"Starting Excel export for {typeof(T).Name}");

                // Ejecutar query para obtener datos
                var data = await ExecuteQueryAsync(request, sessionData);
                
                // Crear libro de Excel
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(request.SheetName);

                // Configurar documento
                ConfigureWorkbookProperties(workbook, request);

                // Escribir contenido
                var currentRow = 1;
                
                // Título y subtítulo
                if (!string.IsNullOrEmpty(request.Title))
                {
                    currentRow = WriteTitle(worksheet, request.Title, request.Subtitle, currentRow, request.Columns.Count);
                }

                // Encabezados
                if (request.IncludeHeaders)
                {
                    currentRow = WriteHeaders(worksheet, request.Columns, currentRow);
                }

                // Datos
                currentRow = WriteData(worksheet, data, request.Columns, currentRow);

                // Fila de totales
                if (request.IncludeTotalsRow && request.ColumnTotals?.Any() == true)
                {
                    WriteTotalsRow(worksheet, request.ColumnTotals, request.Columns, currentRow, data.Count);
                }

                // Aplicar formato final
                ApplyFinalFormatting(worksheet, request, currentRow - 1);

                // Convertir a bytes
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                
                _logger.LogInformation($"Excel export completed. Exported {data.Count} rows");
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error exporting to Excel for {typeof(T).Name}");
                throw new InvalidOperationException($"Error exporting to Excel: {ex.Message}", ex);
            }
        }

        #region Private Methods

        /// <summary>
        /// Ejecuta la query para obtener los datos
        /// </summary>
        private async Task<List<object>> ExecuteQueryAsync(ExcelExportRequest request, SessionDataDto sessionData)
        {
            // Si hay límite máximo, aplicarlo
            if (request.MaxRows > 0)
            {
                request.Query.Take = Math.Min(request.Query.Take ?? request.MaxRows, request.MaxRows);
            }

            // Ejecutar query usando el servicio base
            var queryResult = await _queryService.QuerySelectAsync(request.Query, sessionData);
            
            if (queryResult == null)
            {
                throw new InvalidOperationException("Query failed: No data returned");
            }

            return queryResult;
        }

        /// <summary>
        /// Configura las propiedades del libro de Excel
        /// </summary>
        private void ConfigureWorkbookProperties(XLWorkbook workbook, ExcelExportRequest request)
        {
            if (request.DocumentProperties != null)
            {
                var props = request.DocumentProperties;
                
                if (!string.IsNullOrEmpty(props.Title))
                    workbook.Properties.Title = props.Title;
                if (!string.IsNullOrEmpty(props.Subject))
                    workbook.Properties.Subject = props.Subject;
                if (!string.IsNullOrEmpty(props.Author))
                    workbook.Properties.Author = props.Author;
                if (!string.IsNullOrEmpty(props.Manager))
                    workbook.Properties.Manager = props.Manager;
                if (!string.IsNullOrEmpty(props.Company))
                    workbook.Properties.Company = props.Company;
                if (!string.IsNullOrEmpty(props.Category))
                    workbook.Properties.Category = props.Category;
                if (!string.IsNullOrEmpty(props.Keywords))
                    workbook.Properties.Keywords = props.Keywords;
                if (!string.IsNullOrEmpty(props.Comments))
                    workbook.Properties.Comments = props.Comments;
                
                workbook.Properties.Created = props.CreatedDate ?? DateTime.Now;
                workbook.Properties.Modified = props.ModifiedDate ?? DateTime.Now;
            }
        }

        /// <summary>
        /// Escribe título y subtítulo
        /// </summary>
        private int WriteTitle(IXLWorksheet worksheet, string title, string? subtitle, int startRow, int columnCount)
        {
            var currentRow = startRow;
            
            // Título
            var titleCell = worksheet.Cell(currentRow, 1);
            titleCell.Value = title;
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 16;
            if (columnCount > 1)
            {
                worksheet.Range(currentRow, 1, currentRow, columnCount).Merge();
            }
            currentRow++;

            // Subtítulo
            if (!string.IsNullOrEmpty(subtitle))
            {
                var subtitleCell = worksheet.Cell(currentRow, 1);
                subtitleCell.Value = subtitle;
                subtitleCell.Style.Font.FontSize = 12;
                if (columnCount > 1)
                {
                    worksheet.Range(currentRow, 1, currentRow, columnCount).Merge();
                }
                currentRow++;
            }

            // Línea en blanco
            currentRow++;
            
            return currentRow;
        }

        /// <summary>
        /// Escribe los encabezados de las columnas
        /// </summary>
        private int WriteHeaders(IXLWorksheet worksheet, List<ExcelColumnConfig> columns, int row)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                if (!column.Visible) continue;
                
                var cell = worksheet.Cell(row, i + 1);
                cell.Value = column.DisplayName;
                
                // Estilo del encabezado
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                
                // Comentario si existe
                if (!string.IsNullOrEmpty(column.Comment))
                {
                    cell.CreateComment().AddText(column.Comment);
                }
            }
            
            return row + 1;
        }

        /// <summary>
        /// Escribe los datos
        /// </summary>
        private int WriteData(IXLWorksheet worksheet, List<object> data, List<ExcelColumnConfig> columns, int startRow)
        {
            var currentRow = startRow;
            
            foreach (var item in data)
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    var column = columns[i];
                    if (!column.Visible) continue;
                    
                    var cell = worksheet.Cell(currentRow, i + 1);
                    var value = GetPropertyValue(item, column.PropertyPath);
                    
                    // Aplicar transformación personalizada si existe
                    if (column.ValueTransform != null)
                    {
                        cell.Value = column.ValueTransform(value);
                    }
                    else
                    {
                        // Formatear valor según el tipo
                        var formattedValue = FormatValue(value, column);
                        cell.Value = XLCellValue.FromObject(formattedValue);
                    }
                    
                    // Aplicar formato de celda
                    ApplyColumnFormatting(cell, column);
                }
                currentRow++;
            }
            
            return currentRow;
        }

        /// <summary>
        /// Escribe la fila de totales
        /// </summary>
        private void WriteTotalsRow(IXLWorksheet worksheet, Dictionary<string, ExcelTotalFunction> columnTotals, 
            List<ExcelColumnConfig> columns, int row, int dataRowCount)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                if (!column.Visible) continue;
                
                var cell = worksheet.Cell(row, i + 1);
                
                if (columnTotals.TryGetValue(column.PropertyPath, out var totalFunction) && totalFunction != ExcelTotalFunction.None)
                {
                    // Crear fórmula para el total
                    var dataRange = worksheet.Range(row - dataRowCount, i + 1, row - 1, i + 1);
                    var formula = GetTotalFormula(totalFunction, dataRange.RangeAddress.ToString());
                    cell.FormulaA1 = formula;
                }
                else if (i == 0)
                {
                    // Primera columna: etiqueta "Total"
                    cell.Value = "Total";
                }
                
                // Estilo de fila de totales
                cell.Style.Font.Bold = true;
                cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            }
        }

        /// <summary>
        /// Aplica el formato final a la hoja
        /// </summary>
        private void ApplyFinalFormatting(IXLWorksheet worksheet, ExcelExportRequest request, int lastRow)
        {
            if (lastRow <= 1) return;
            
            // Ajustar ancho de columnas
            if (request.AutoFitColumns)
            {
                worksheet.ColumnsUsed().AdjustToContents();
            }
            else
            {
                // Aplicar anchos personalizados
                for (int i = 0; i < request.Columns.Count; i++)
                {
                    var column = request.Columns[i];
                    if (column.Width.HasValue)
                    {
                        worksheet.Column(i + 1).Width = column.Width.Value;
                    }
                }
            }
            
            // Congelar encabezados
            if (request.FreezeHeaders && request.IncludeHeaders)
            {
                var headerRow = request.Title != null ? (request.Subtitle != null ? 4 : 3) : 1;
                worksheet.SheetView.FreezeRows(headerRow);
            }
            
            // Formato de tabla (incluye automáticamente autofilter)
            if (request.FormatAsTable && request.IncludeHeaders && lastRow > 1)
            {
                var headerRow = request.Title != null ? (request.Subtitle != null ? 4 : 3) : 1;
                var tableRange = worksheet.Range(headerRow, 1, lastRow, request.Columns.Count);
                var table = tableRange.CreateTable();
                
                if (!string.IsNullOrEmpty(request.TableStyleName))
                {
                    // Aplicar estilo personalizado si está disponible
                    try
                    {
                        table.Theme = (XLTableTheme)Enum.Parse(typeof(XLTableTheme), request.TableStyleName);
                    }
                    catch
                    {
                        // Si el estilo no existe, usar el por defecto
                        table.Theme = XLTableTheme.TableStyleMedium2;
                    }
                }
            }
            // Solo aplicar AutoFilter si NO se está usando FormatAsTable
            else if (request.AutoFilter && request.IncludeHeaders)
            {
                var headerRow = request.Title != null ? (request.Subtitle != null ? 4 : 3) : 1;
                worksheet.Range(headerRow, 1, lastRow, request.Columns.Count).SetAutoFilter();
            }
        }

        /// <summary>
        /// Obtiene el valor de una propiedad usando reflexión
        /// </summary>
        private object? GetPropertyValue(object obj, string propertyPath)
        {
            try
            {
                var properties = propertyPath.Split('.');
                object? current = obj;
                
                foreach (var property in properties)
                {
                    if (current == null) return null;
                    
                    var propInfo = current.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
                    if (propInfo == null) return null;
                    
                    current = propInfo.GetValue(current);
                }
                
                return current;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Formatea un valor según la configuración de columna
        /// </summary>
        private object FormatValue(object? value, ExcelColumnConfig column)
        {
            if (value == null) return "";
            
            // Formateo especial para booleanos
            if (value is bool boolValue && column.Format.HasValue)
            {
                return column.Format.Value switch
                {
                    ExcelFormat.YesNo => boolValue ? "Sí" : "No",
                    ExcelFormat.TrueFalse => boolValue ? "Verdadero" : "Falso",
                    ExcelFormat.ActiveInactive => boolValue ? "Activo" : "Inactivo",
                    ExcelFormat.EnabledDisabled => boolValue ? "Habilitado" : "Deshabilitado",
                    ExcelFormat.OnOff => boolValue ? "Encendido" : "Apagado",
                    _ => value
                };
            }
            
            return value;
        }

        /// <summary>
        /// Aplica formato a una celda según la configuración de columna
        /// </summary>
        private void ApplyColumnFormatting(IXLCell cell, ExcelColumnConfig column)
        {
            // Formato de número
            if (!string.IsNullOrEmpty(column.CustomFormat))
            {
                cell.Style.NumberFormat.Format = column.CustomFormat;
            }
            else if (column.Format.HasValue)
            {
                cell.Style.NumberFormat.Format = GetExcelFormatString(column.Format.Value);
            }
            
            // Alineación
            if (column.Alignment.HasValue)
            {
                cell.Style.Alignment.Horizontal = column.Alignment.Value switch
                {
                    ExcelAlignment.Left => XLAlignmentHorizontalValues.Left,
                    ExcelAlignment.Center => XLAlignmentHorizontalValues.Center,
                    ExcelAlignment.Right => XLAlignmentHorizontalValues.Right,
                    ExcelAlignment.Justify => XLAlignmentHorizontalValues.Justify,
                    _ => XLAlignmentHorizontalValues.General
                };
            }
            
            // Estilo de fuente
            if (column.Bold)
            {
                cell.Style.Font.Bold = true;
            }
            
            // Ajuste de texto
            if (column.WrapText)
            {
                cell.Style.Alignment.WrapText = true;
            }
            
            // Colores
            if (!string.IsNullOrEmpty(column.BackgroundColor))
            {
                try
                {
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml(column.BackgroundColor);
                }
                catch { /* Color inválido, ignorar */ }
            }
            
            if (!string.IsNullOrEmpty(column.TextColor))
            {
                try
                {
                    cell.Style.Font.FontColor = XLColor.FromHtml(column.TextColor);
                }
                catch { /* Color inválido, ignorar */ }
            }
        }

        /// <summary>
        /// Convierte ExcelFormat a string de formato de Excel
        /// </summary>
        private string GetExcelFormatString(ExcelFormat format)
        {
            return format switch
            {
                ExcelFormat.Date => "dd/MM/yyyy",
                ExcelFormat.DateTime => "dd/MM/yyyy HH:mm:ss",
                ExcelFormat.DateTimeShort => "dd/MM/yyyy HH:mm",
                ExcelFormat.DateOnly => "dd/MM/yyyy",
                ExcelFormat.TimeOnly => "HH:mm:ss",
                ExcelFormat.Integer => "#,##0",
                ExcelFormat.Decimal2 => "#,##0.00",
                ExcelFormat.Decimal4 => "#,##0.0000",
                ExcelFormat.Currency => "\"$\"#,##0.00",
                ExcelFormat.CurrencyNoSymbol => "#,##0.00",
                ExcelFormat.Percentage => "0.00%",
                ExcelFormat.Scientific => "0.00E+00",
                ExcelFormat.Text => "@",
                ExcelFormat.TextWrap => "@",
                ExcelFormat.Phone => "@",
                ExcelFormat.Email => "@",
                ExcelFormat.Url => "@",
                ExcelFormat.Guid => "@",
                ExcelFormat.Code => "@",
                _ => "General"
            };
        }

        /// <summary>
        /// Genera fórmula para funciones de total
        /// </summary>
        private string GetTotalFormula(ExcelTotalFunction function, string range)
        {
            return function switch
            {
                ExcelTotalFunction.Sum => $"SUM({range})",
                ExcelTotalFunction.Average => $"AVERAGE({range})",
                ExcelTotalFunction.Count => $"COUNTA({range})",
                ExcelTotalFunction.CountNumbers => $"COUNT({range})",
                ExcelTotalFunction.Max => $"MAX({range})",
                ExcelTotalFunction.Min => $"MIN({range})",
                ExcelTotalFunction.StdDev => $"STDEV({range})",
                ExcelTotalFunction.Var => $"VAR({range})",
                _ => ""
            };
        }

        #endregion
    }
}