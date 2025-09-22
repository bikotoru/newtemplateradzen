using Microsoft.AspNetCore.Components;
using Frontend.Components.Base.Tables;
using Frontend.Pages.AdvancedQuery;
using Frontend.Services;
using Radzen;
using Frontend.Components.Auth;

namespace Frontend.Pages.AdvancedQuery;

// Alias for SavedQuery entity to avoid naming conflicts
using SavedQueryEntity = Frontend.Pages.AdvancedQuery.SavedQueryDto;

public partial class List : AuthorizedPageBase
{
    [Inject] private SavedQueryService SavedQueryService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private QueryService QueryService { get; set; } = null!;

    private EntityTable<SavedQueryDto>? entityTable;
    private SavedQueryViewManager? viewManager;
    private ViewConfiguration<SavedQueryDto>? currentView;
    
    // Modal states
    private bool showShareModal = false;
    private bool showDuplicateModal = false;
    private SavedQueryDto? selectedQuery;
    
    private List<IViewConfiguration<SavedQueryDto>>? ViewConfigurationsTyped => 
        viewManager?.ViewConfigurations?.Cast<IViewConfiguration<SavedQueryDto>>().ToList();

    // Permissions
    private bool CanCreate => AuthService.HasPermission("SAVEDQUERIES.CREATE");
    private bool CanView => AuthService.HasPermission("SAVEDQUERIES.VIEW");
    
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync(); // ¡IMPORTANTE! Siempre llamar primero al base para verificar permisos
        
        if (HasRequiredPermissions)
        {
            viewManager = new SavedQueryViewManager(QueryService);
            currentView = viewManager.GetDefaultView();
        }
    }

    private async Task HandleEdit(SavedQueryDto savedQuery)
    {
        Navigation.NavigateTo($"/advanced-query/saved-queries/formulario/{savedQuery.Id}");
    }

    private async Task HandleDelete(SavedQueryDto savedQuery)
    {
        try
        {
            var confirmed = await DialogService.Confirm(
                $"¿Estás seguro de que deseas eliminar la búsqueda guardada '{savedQuery.Name}'?",
                "Confirmar Eliminación",
                new ConfirmOptions { OkButtonText = "Sí, Eliminar", CancelButtonText = "Cancelar" }
            );

            if (confirmed == true)
            {
                await DialogService.OpenLoadingAsync("Eliminando...");
                var response = await SavedQueryService.DeleteAsync(savedQuery.Id);
                DialogService.Close();
                
                if (!response.Success)
                {
                    await DialogService.Alert(
                        response.Message ?? "Error al eliminar la búsqueda guardada", 
                        "Error"
                    );
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "¡Éxito!",
                        Detail = "Búsqueda guardada eliminada exitosamente",
                        Duration = 4000
                    });
                    
                    // Refrescar la tabla después de eliminar
                    if (entityTable != null)
                    {
                        await entityTable.Reload();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DialogService.Close();
            await DialogService.Alert(
                $"Error inesperado al eliminar: {ex.Message}", 
                "Error"
            );
        }
    }

    private async Task HandleDuplicate(SavedQueryDto savedQuery)
    {
        selectedQuery = savedQuery;
        showDuplicateModal = true;
        StateHasChanged();
    }

    private async Task HandleShare(SavedQueryDto savedQuery)
    {
        selectedQuery = savedQuery;
        showShareModal = true;
        StateHasChanged();
    }

    private async Task HandleExecute(SavedQueryDto savedQuery)
    {
        try
        {
            // Navigate to advanced query page with the saved query loaded
            Navigation.NavigateTo($"/advanced-query?loadQuery={savedQuery.Id}");
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error al ejecutar la búsqueda: {ex.Message}",
                Duration = 5000
            });
        }
    }
    
    private async Task OnViewChanged(IViewConfiguration<SavedQueryDto> selectedView)
    {
        if (selectedView is ViewConfiguration<SavedQueryDto> viewConfig)
        {
            currentView = viewConfig;
            await InvokeAsync(StateHasChanged);
        }
    }
    
    private string GetGridKey()
    {
        return $"savedqueries_grid_{currentView?.DisplayName ?? "default"}";
    }

    private async Task OnQueryShared()
    {
        showShareModal = false;
        selectedQuery = null;
        
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Success,
            Summary = "¡Éxito!",
            Detail = "Búsqueda guardada compartida exitosamente",
            Duration = 4000
        });

        // Refrescar la tabla para mostrar cambios en SharedCount
        if (entityTable != null)
        {
            await entityTable.Reload();
        }
        
        StateHasChanged();
    }

    private async Task OnQueryDuplicated()
    {
        showDuplicateModal = false;
        selectedQuery = null;
        
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Success,
            Summary = "¡Éxito!",
            Detail = "Búsqueda guardada duplicada exitosamente",
            Duration = 4000
        });

        // Refrescar la tabla para mostrar la nueva búsqueda
        if (entityTable != null)
        {
            await entityTable.Reload();
        }
        
        StateHasChanged();
    }
}