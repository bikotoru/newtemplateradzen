using Microsoft.AspNetCore.Components;
using Frontend.Components.Base.Tables;
using Shared.Models.Entities;
using Frontend.Services;
using Radzen;
using CategoriaEntity = Shared.Models.Entities.Categoria;

namespace Frontend.Modules.Categoria;

public partial class CategoriaList : ComponentBase
{
    [Inject] private CategoriaService CategoriaService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private QueryService QueryService { get; set; } = null!;

    private EntityTable<CategoriaEntity>? entityTable;
    private CategoriaViewManager viewManager = null!;
    private ViewConfiguration<CategoriaEntity> currentView = null!;
    
    // Propiedad para EntityTable - ahora es type-safe
    private List<IViewConfiguration<CategoriaEntity>>? ViewConfigurationsTyped => viewManager?.ViewConfigurations?.Cast<IViewConfiguration<CategoriaEntity>>().ToList();
    
    protected override void OnInitialized()
    {
        Console.WriteLine("[CategoriaList] OnInitialized iniciado");
        
        viewManager = new CategoriaViewManager(QueryService);
        currentView = viewManager.GetDefaultView();
        
        Console.WriteLine($"[CategoriaList] ViewManager creado con {viewManager.ViewConfigurations.Count} vistas:");
        foreach (var view in viewManager.ViewConfigurations)
        {
            Console.WriteLine($"[CategoriaList]   Vista: {view.DisplayName} - {view.ColumnConfigs?.Count ?? 0} columnas");
        }
        
        Console.WriteLine($"[CategoriaList] Vista por defecto: {currentView?.DisplayName}");
        Console.WriteLine($"[CategoriaList] Vista por defecto tiene {currentView?.ColumnConfigs?.Count ?? 0} columnas:");
        
        if (currentView?.ColumnConfigs != null)
        {
            foreach (var col in currentView.ColumnConfigs)
            {
                Console.WriteLine($"[CategoriaList]   - {col.Property} ({col.Title}) - Visible: {col.Visible}, Order: {col.Order}");
            }
        }
        
        base.OnInitialized();
    }


    private async Task HandleEdit(CategoriaEntity categoria)
    {
        Navigation.NavigateTo($"/categoria/formulario/{categoria.Id}");
    }

    private async Task HandleDelete(CategoriaEntity categoria)
    {
        var response = await CategoriaService.DeleteAsync(categoria.Id);
        
        if (!response.Success)
        {
            await DialogService.Alert(
                response.Message ?? "Error al eliminar categoría", 
                "Error"
            );
        }
    }
    
    private async Task OnViewChanged(IViewConfiguration<CategoriaEntity> selectedView)
    {
        Console.WriteLine($"[CategoriaList] OnViewChanged llamado con vista: {selectedView?.DisplayName}");
        Console.WriteLine($"[CategoriaList] Vista anterior: {currentView?.DisplayName}");
        
        if (selectedView is ViewConfiguration<CategoriaEntity> viewConfig)
        {
            var previousView = currentView?.DisplayName;
            currentView = viewConfig;
            
            Console.WriteLine($"[CategoriaList] Vista cambiada de '{previousView}' a '{currentView.DisplayName}'");
            Console.WriteLine($"[CategoriaList] Nueva vista tiene {currentView.ColumnConfigs?.Count ?? 0} columnas:");
            
            if (currentView.ColumnConfigs != null)
            {
                foreach (var col in currentView.ColumnConfigs)
                {
                    Console.WriteLine($"[CategoriaList]   - {col.Property} ({col.Title}) - Visible: {col.Visible}, Order: {col.Order}");
                }
            }
            
            // Forzar reconstrucción completa del grid
            await InvokeAsync(StateHasChanged);
            Console.WriteLine("[CategoriaList] StateHasChanged ejecutado");
        }
        else
        {
            Console.WriteLine("[CategoriaList] ERROR: selectedView no es ViewConfiguration<CategoriaEntity>");
        }
    }
    
    /// <summary>
    /// Genera una clave única para forzar la reconstrucción del grid cuando cambie la vista
    /// </summary>
    private string GetGridKey()
    {
        // Clave única basada SOLO en la vista actual - no cambiar en cada render
        return $"grid_{currentView?.DisplayName ?? "default"}";
    }
}