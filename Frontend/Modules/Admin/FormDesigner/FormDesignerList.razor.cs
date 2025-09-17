using Microsoft.AspNetCore.Components;
using Frontend.Components.Base.Tables;
using Shared.Models.Entities.SystemEntities;
using Frontend.Services;
using Radzen;
using Frontend.Components.Auth;

namespace Frontend.Modules.Admin.FormDesigner;

public partial class FormDesignerList : AuthorizedPageBase
{
    [Inject] private SystemFormEntityService SystemFormEntityService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private QueryService QueryService { get; set; } = null!;

    private EntityTable<SystemFormEntity>? entityTable;
    private SystemFormEntityViewManager? viewManager;
    private ViewConfiguration<SystemFormEntity>? currentView;

    private List<IViewConfiguration<SystemFormEntity>>? ViewConfigurationsTyped =>
        viewManager?.ViewConfigurations?.Cast<IViewConfiguration<SystemFormEntity>>().ToList();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync(); // ¡IMPORTANTE! Siempre llamar primero al base para verificar permisos

        if (HasRequiredPermissions)
        {
            viewManager = new SystemFormEntityViewManager(QueryService);
            currentView = viewManager.GetDefaultView();
        }
    }

    private async Task HandleEdit(SystemFormEntity entity)
    {
        // Navegar al diseñador de formularios para esta entidad
        Navigation.NavigateTo($"/admin/form-designer/{entity.EntityName}");
    }

    private async Task OnViewChanged(IViewConfiguration<SystemFormEntity> selectedView)
    {
        if (selectedView is ViewConfiguration<SystemFormEntity> viewConfig)
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