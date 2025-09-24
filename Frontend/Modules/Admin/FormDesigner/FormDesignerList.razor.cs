using Microsoft.AspNetCore.Components;
using Frontend.Components.Base.Tables;
using Shared.Models.Entities.SystemEntities;
using Frontend.Services;
using Radzen;
using Frontend.Components.Auth;

namespace Frontend.Modules.Admin.FormDesigner;

public partial class FormDesignerList : AuthorizedPageBase
{
    [Inject] private SystemFormEntitiesService SystemFormEntitiesService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private QueryService QueryService { get; set; } = null!;

    private EntityTable<SystemFormEntities>? entityTable;
    private SystemFormEntitiesViewManager? viewManager;
    private ViewConfiguration<SystemFormEntities>? currentView;

    private List<IViewConfiguration<SystemFormEntities>>? ViewConfigurationsTyped =>
        viewManager?.ViewConfigurations?.Cast<IViewConfiguration<SystemFormEntities>>().ToList();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync(); // ¡IMPORTANTE! Siempre llamar primero al base para verificar permisos

        if (HasRequiredPermissions)
        {
            viewManager = new SystemFormEntitiesViewManager(QueryService);
            currentView = viewManager.GetDefaultView();
        }
    }

    private async Task HandleEdit(SystemFormEntities entity)
    {
        // Navegar al diseñador de formularios para esta entidad
        Navigation.NavigateTo($"/admin/form-designer/formulario/{entity.EntityName}");
    }

    private async Task OnViewChanged(IViewConfiguration<SystemFormEntities> selectedView)
    {
        if (selectedView is ViewConfiguration<SystemFormEntities> viewConfig)
        {
            currentView = viewConfig;
            await InvokeAsync(StateHasChanged);
        }
    }

    private string GetGridKey()
    {
        return $"grid_{currentView?.DisplayName ?? "default"}";
    }

    // HandleEdit ya implementa la navegación al diseñador
    // No necesitamos CustomActionButtons, usamos la acción edit estándar
}