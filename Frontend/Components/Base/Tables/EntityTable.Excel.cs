using Shared.Models.Export;

namespace Frontend.Components.Base.Tables;

public partial class EntityTable<T>
{
    #region Excel Export Methods

    private async Task ExportToExcel()
    {
        try
        {
            if (OnCustomExcelExport.HasDelegate)
            {
                var context = new ExcelExportContext<T>
                {
                    LastLoadDataArgs = lastLoadDataArgs,
                    VisibleColumns = GetVisibleColumns(),
                    CurrentEntities = entities?.ToList() ?? new List<T>(),
                    TotalCount = totalCount,
                    SearchTerm = searchTerm,
                    ApiService = apiService,
                    FileDownloadService = FileDownloadService,
                    DefaultFileName = string.IsNullOrEmpty(ExcelFileName) 
                        ? $"{typeof(T).Name}_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                        : ExcelFileName
                };
                
                await OnCustomExcelExport.InvokeAsync(context);
            }
            else if (OnExcelExport.HasDelegate)
            {
                await OnExcelExport.InvokeAsync();
            }
            else if (apiService != null && lastLoadDataArgs != null)
            {
                var columns = ExcelColumns ?? GetVisibleColumns();
                
                var serializableColumns = columns.Select(c => new ExcelColumnConfig
                {
                    PropertyPath = c.PropertyPath,
                    DisplayName = c.DisplayName,
                    Format = c.Format,
                    CustomFormat = c.CustomFormat,
                    Width = c.Width,
                    Bold = c.Bold,
                    Alignment = c.Alignment,
                    WrapText = c.WrapText,
                    BackgroundColor = c.BackgroundColor,
                    TextColor = c.TextColor,
                    Order = c.Order,
                    Visible = c.Visible,
                    Comment = c.Comment
                }).ToList();
                
                var fileName = string.IsNullOrEmpty(ExcelFileName) 
                    ? $"{typeof(T).Name}_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    : ExcelFileName;

                await apiService.DownloadExcelAsync(
                    lastLoadDataArgs, 
                    FileDownloadService, 
                    serializableColumns, 
                    fileName
                );
            }
            else
            {
                await DialogService.Alert("No hay datos para exportar", "Información");
            }
        }
        catch (Exception ex)
        {
            await DialogService.Alert($"Error al exportar: {ex.Message}", "Error");
        }
    }

    #endregion
}