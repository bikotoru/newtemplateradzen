using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Frontend.Pages.AdvancedQuery;
using Frontend.Services.Validation;
using Frontend.Components.Validation;
using Frontend.Components.Auth;
using Frontend.Components.Forms;
using Radzen;
using System.Linq.Expressions;
using Shared.Models.Requests;

namespace Frontend.Pages.AdvancedQuery;

// Alias for SavedQuery entity to avoid naming conflicts
using SavedQueryEntity = Frontend.Pages.AdvancedQuery.SavedQueryDto;

public partial class Formulario : AuthorizedPageBase
{
    [Inject] private SavedQueryService SavedQueryService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = null!;
    
    [Parameter] public Guid? Id { get; set; }

    private SavedQueryDto? entity;
    private bool isLoading = false;
    private bool isEditMode => Id.HasValue;
    private bool isFormValid = false;
    private bool isNewlyCreated = false;

    // Available data for dropdowns
    private List<AvailableEntity> availableEntities = new();
    private List<LogicalOperatorOption> logicalOperators = new();

    // Propiedades de permisos
    private bool CanView => AuthService.HasPermission("SAVEDQUERIES.VIEW");
    private bool CanCreate => AuthService.HasPermission("SAVEDQUERIES.CREATE");
    private bool CanEdit => isEditMode ? (entity?.CanEdit ?? false) : AuthService.HasPermission("SAVEDQUERIES.CREATE");
    private bool CanSave => CanEdit;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync(); // ¡IMPORTANTE! Siempre llamar primero al base para verificar permisos
        
        if (HasRequiredPermissions)
        {
            await OnPermissionsVerifiedAsync();
        }
    }

    protected override async Task OnPermissionsVerifiedAsync()
    {
        await InitializeDropdownData();

        if (isEditMode && Id.HasValue)
        {
            await LoadEntity();
            
            if (Navigation.Uri.Contains("created=true"))
            {
                isNewlyCreated = true;
                
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "¡Éxito!",
                    Detail = "Búsqueda guardada creada exitosamente. Ahora puedes editarla.",
                    Duration = 5000
                });
                
                Navigation.NavigateTo($"/advanced-query/saved-queries/formulario/{Id}", replace: true);
            }
        }
        else
        {
            entity = new SavedQueryDto 
            { 
                Id = Guid.NewGuid(),
                Active = true,
                FechaCreacion = DateTime.Now,
                FechaModificacion = DateTime.Now,
                TakeLimit = 50,
                LogicalOperator = 0, // AND
                IsPublic = false,
                IsTemplate = false,
                CanEdit = true,
                CanShare = true,
                SharedCount = 0,
                SelectedFields = "[]",
                FilterConfiguration = "{}"
            };
        }
        
        StateHasChanged();
    }

    private async Task InitializeDropdownData()
    {
        try
        {
            // Initialize available entities (this would typically come from an API)
            availableEntities = new List<AvailableEntity>
            {
                new() { Name = "Region", DisplayName = "Regiones" },
                new() { Name = "SystemUsers", DisplayName = "Usuarios del Sistema" },
                new() { Name = "SystemRoles", DisplayName = "Roles del Sistema" },
                new() { Name = "SystemPermissions", DisplayName = "Permisos del Sistema" }
            };

            // Initialize logical operators
            logicalOperators = new List<LogicalOperatorOption>
            {
                new() { Value = 0, Text = "Y (AND)" },
                new() { Value = 1, Text = "O (OR)" }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SavedQueryFormulario] Error initializing dropdown data: {ex.Message}");
        }
    }

    private async Task LoadEntity()
    {
        try
        {
            isLoading = true;
            await DialogService.OpenLoadingAsync("Obteniendo datos...");
            var response = await SavedQueryService.GetByIdAsync(Id!.Value);
            
            if (response.Success && response.Data != null)
            {
                entity = response.Data;
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = "No se pudo cargar la búsqueda guardada",
                    Duration = 5000
                });
                Navigation.NavigateTo("/advanced-query/saved-queries/list");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error cargando búsqueda guardada: {ex.Message}",
                Duration = 5000
            });
        }
        finally
        {
            isLoading = false;
            DialogService.Close();
            StateHasChanged();
        }
    }

    private FormValidationRules GetValidationRules()
    {
        return FormValidationRulesBuilder
            .Create()
            .Field("Name", field => field
                .Required("El nombre es obligatorio")
                .Length(3, 100, "El nombre debe tener entre 3 y 100 caracteres"))
            .Field("EntityName", field => field
                .Required("La entidad es obligatoria"))
            .Field("TakeLimit", field => field
                .Required("El límite de registros es obligatorio")
                .Range(1, 10000, "El límite debe estar entre 1 y 10,000 registros"))
            .Build();
    }
    
    private async Task SaveForm()
    {
        try
        {
            // Verificar permisos antes de continuar
            if (!CanSave)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Permisos Insuficientes",
                    Detail = isEditMode ? 
                        "No tienes permisos para editar este registro" : 
                        "No tienes permisos para crear nuevos registros",
                    Duration = 4000
                });
                return;
            }

            if (entity == null)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = "Error interno: entidad no inicializada",
                    Duration = 4000
                });
                return;
            }

            isLoading = true;

            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validación",
                    Detail = "El nombre es obligatorio",
                    Duration = 4000
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(entity.EntityName))
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validación",
                    Detail = "La entidad es obligatoria",
                    Duration = 4000
                });
                return;
            }

            if (entity.TakeLimit < 1 || entity.TakeLimit > 10000)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validación",
                    Detail = "El límite debe estar entre 1 y 10,000 registros",
                    Duration = 4000
                });
                return;
            }

            if (isEditMode)
            {
                var updateRequest = new UpdateRequest<SavedQueryDto>
                {
                    Entity = entity
                };

                await DialogService.OpenLoadingAsync("Actualizando...");
                var response = await SavedQueryService.UpdateAsync(updateRequest);
                DialogService.Close();

                if (response.Success)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "¡Éxito!",
                        Detail = "Búsqueda guardada actualizada exitosamente",
                        Duration = 4000
                    });
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = response.Message ?? "Error al actualizar búsqueda guardada",
                        Duration = 5000
                    });
                }
            }
            else
            {
                var createRequest = new CreateRequest<SavedQueryDto>
                {
                    Entity = entity
                };

                await DialogService.OpenLoadingAsync("Creando...");
                var response = await SavedQueryService.CreateAsync(createRequest);
                DialogService.Close();

                if (response.Success && response.Data != null)
                {
                    entity = response.Data;
                    Navigation.NavigateTo($"/advanced-query/saved-queries/formulario/{entity.Id}?created=true");
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = response.Message ?? "Error al crear búsqueda guardada",
                        Duration = 5000
                    });
                }
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error Inesperado",
                Detail = ex.Message,
                Duration = 6000
            });
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    // Helper classes for dropdown data
    private class AvailableEntity
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }

    private class LogicalOperatorOption
    {
        public int Value { get; set; }
        public string Text { get; set; } = "";
    }
}