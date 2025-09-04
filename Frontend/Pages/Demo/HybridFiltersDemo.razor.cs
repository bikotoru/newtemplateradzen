using Microsoft.AspNetCore.Components;
using Frontend.Components.Base.Tables;
using Frontend.Modules.Categoria;
using Shared.Models.Entities;
using Radzen;

namespace Frontend.Pages.Demo;

public partial class HybridFiltersDemo : ComponentBase
{
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    
    private EntityTable<Categoria>? entityTable;
    private bool useHybridFilters = true;
    
    /// <summary>
    /// Obtiene las configuraciones de columnas según el modo actual
    /// </summary>
    private List<ColumnConfig<Categoria>> GetCurrentColumnConfigs()
    {
        return useHybridFilters 
            ? CategoriaConfig.HybridFilters.GetHybridFilterColumns()
            : CategoriaConfig.HybridFilters.GetTraditionalColumns();
    }
    
    /// <summary>
    /// Genera clave única para forzar recreación de tabla al cambiar modo
    /// </summary>
    private string GetTableKey()
    {
        return $"hybrid-demo-{useHybridFilters}";
    }
    
    /// <summary>
    /// Handler personalizado para demostración de filtros
    /// </summary>
    private async Task HandleFilterData(DataGridLoadColumnFilterDataEventArgs<Categoria> args)
    {
        var propertyName = args.Column?.GetFilterProperty();
        
        Console.WriteLine($"[HybridFiltersDemo] Loading filter data for: {propertyName}");
        Console.WriteLine($"[HybridFiltersDemo] Filter: '{args.Filter}', Skip: {args.Skip}, Top: {args.Top}");
        
        // Ejemplo de personalización para demostrar capacidades
        switch (propertyName)
        {
            case nameof(Categoria.Active):
                // Personalizar valores booleanos con texto más descriptivo
                var boolValues = new[]
                {
                    new { Value = true, Text = "✅ Categorías Activas" },
                    new { Value = false, Text = "❌ Categorías Inactivas" }
                };
                
                // Aplicar filtro de búsqueda si existe
                if (!string.IsNullOrEmpty(args.Filter))
                {
                    var filtered = boolValues.Where(v => 
                        v.Text.Contains(args.Filter, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    args.Data = filtered;
                    args.Count = filtered.Length;
                }
                else
                {
                    args.Data = boolValues;
                    args.Count = boolValues.Length;
                }
                
                Console.WriteLine($"[HybridFiltersDemo] Custom boolean filter loaded: {args.Count} items");
                break;
                
            case nameof(Categoria.Nombre):
                // Para nombres, agregar estadística de uso (simulado)
                Console.WriteLine("[HybridFiltersDemo] Loading nombres with usage stats...");
                
                // En un caso real, podrías cargar estadísticas de uso desde la BD
                // Por ahora, usar la lógica automática de EntityTable
                return;
                
            default:
                // Para otros campos, usar auto-generación de EntityTable
                return;
        }
    }
    
    /// <summary>
    /// Limpia el cache de filtros
    /// </summary>
    private async Task ClearCache()
    {
        if (entityTable != null)
        {
            await entityTable.ClearFilterCache();
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Cache Limpiado",
                Detail = "El cache de filtros ha sido limpiado exitosamente.",
                Duration = 3000
            });
        }
    }
    
    /// <summary>
    /// Muestra estadísticas del cache
    /// </summary>
    private async Task ShowCacheStats()
    {
        if (entityTable != null)
        {
            var stats = await entityTable.GetFilterCacheStats();
            
            var message = $@"
📊 Estadísticas del Cache de Filtros:

• Total de entradas: {stats.totalEntries}
• Entradas expiradas: {stats.expiredEntries}  
• Entrada más antigua: {stats.oldestEntry.TotalMinutes:F1} minutos

💡 El cache mejora el rendimiento al evitar consultas repetidas a la base de datos.";

            await DialogService.Alert(message, "Estadísticas de Cache");
        }
    }
    
    /// <summary>
    /// Se ejecuta cuando cambia el toggle de modo híbrido
    /// </summary>
    protected override void OnParametersSet()
    {
        // Forzar actualización cuando cambie el modo
        StateHasChanged();
    }
}