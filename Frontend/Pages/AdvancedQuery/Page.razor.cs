using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using System.Text.Json;
using Frontend.Services;
using Frontend.Pages.AdvancedQuery.Components;
using Frontend.Pages.AdvancedQuery.Components.Modals;

namespace Frontend.Pages.AdvancedQuery;

public partial class Page : ComponentBase
{
    [Inject] private AdvancedQueryService AdvancedQueryService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

    // State variables
    private List<AvailableEntityDto> availableEntities = new();
    private string? selectedEntityName;
    private AvailableEntityDto? selectedEntity;
    private List<EntityFieldDefinition> entityFields = new();
    private AdvancedQueryResult<object>? queryResults;
    private bool isLoading = false;

    // Component references
    private FilterConfiguration? filterConfigurationRef;

    // Filter configuration
    private LogicalFilterOperator logicalOperator = LogicalFilterOperator.And;
    private FilterCaseSensitivity filterCaseSensitivity = FilterCaseSensitivity.CaseInsensitive;
    private int takeLimit = 50;

    // Variables para selección de campos
    private List<EntityFieldDefinition> selectedFields = new();
    private bool showFieldSelectionDialog = false;
    private int selectedFieldsKey = 0; // Clave para forzar re-render

    protected override async Task OnInitializedAsync()
    {
        await LoadAvailableEntities();
    }

    private async Task LoadAvailableEntities()
    {
        try
        {
            isLoading = true;
            availableEntities = await AdvancedQueryService.GetAdvancedQueryEntitiesAsync();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error cargando entidades: {ex.Message}",
                Duration = 5000
            });
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OnEntitySelected()
    {
        if (string.IsNullOrEmpty(selectedEntityName))
        {
            selectedEntity = null;
            entityFields.Clear();
            selectedFields.Clear(); // Limpiar selección de campos
            queryResults = null;
            return;
        }

        selectedEntity = availableEntities.FirstOrDefault(e => e.EntityName == selectedEntityName);

        try
        {
            isLoading = true;
            entityFields = await AdvancedQueryService.GetEntityFieldDefinitionsAsync(selectedEntityName);

            // Limpiar resultados anteriores y selección de campos
            queryResults = null;
            selectedFields.Clear();

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Entidad Cargada",
                Detail = $"Se cargaron {entityFields.Count} campos para {selectedEntity?.DisplayName}",
                Duration = 3000
            });
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error cargando campos: {ex.Message}",
                Duration = 5000
            });
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task ExecuteQuery()
    {
        if (filterConfigurationRef?.DataFilter?.Filters == null || !filterConfigurationRef.DataFilter.Filters.Any())
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Sin Filtros",
                Detail = "Debe configurar al menos un filtro para ejecutar la consulta",
                Duration = 3000
            });
            return;
        }

        try
        {
            isLoading = true;

            var request = new AdvancedQueryRequest
            {
                Filters = filterConfigurationRef?.DataFilter?.Filters?.ToArray() ?? Array.Empty<CompositeFilterDescriptor>(),
                LogicalOperator = logicalOperator,
                FilterCaseSensitivity = filterCaseSensitivity,
                Take = takeLimit
            };

            queryResults = await AdvancedQueryService.ExecuteAdvancedQueryAsync(selectedEntityName!, request, selectedEntity?.BackendApi);

            if (queryResults.Success)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Consulta Exitosa",
                    Detail = $"Se encontraron {queryResults.TotalCount} registros",
                    Duration = 3000
                });
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error en Consulta",
                    Detail = queryResults.Message,
                    Duration = 5000
                });
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error ejecutando consulta: {ex.Message}",
                Duration = 5000
            });
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task ClearFilters()
    {
        if (filterConfigurationRef?.DataFilter != null)
        {
            await filterConfigurationRef.DataFilter.ClearFilters();
            queryResults = null;
        }
    }

    private async Task ShowGeneratedQuery()
    {
        if (filterConfigurationRef?.DataFilter?.Filters != null && filterConfigurationRef.DataFilter.Filters.Any())
        {
            try
            {
                // Generar la query string usando el servicio
                var request = new AdvancedQueryRequest
                {
                    Filters = filterConfigurationRef?.DataFilter?.Filters?.ToArray() ?? Array.Empty<CompositeFilterDescriptor>(),
                    LogicalOperator = logicalOperator,
                    FilterCaseSensitivity = filterCaseSensitivity
                };

                var filterString = "Generada por AdvancedQueryService.ConvertFiltersToLinqString()";

                var queryInfo = $@"
**Filtros Configurados:** {filterConfigurationRef?.DataFilter.Filters.Count()}
**Operador Lógico:** {logicalOperator}
**Sensibilidad:** {filterCaseSensitivity}
**Límite:** {takeLimit}

**Query LINQ Generada:**
```
{filterString}
```

**Endpoint que se llamaría:**
POST /api/{selectedEntityName}/paged

**Payload JSON:**
```json
{{
  ""filter"": ""{filterString}"",
  ""take"": {takeLimit}
}}
```";

                var dialogParameters = new Dictionary<string, object>
                {
                    ["QueryInfo"] = queryInfo,
                    ["FilterString"] = filterString,
                    ["OnCopyQuery"] = EventCallback.Factory.Create<string>(this, CopyToClipboard),
                    ["OnClose"] = EventCallback.Factory.Create(this, () => DialogService.Close())
                };

                await DialogService.OpenAsync<QueryDetailsModal>("Query Generada", dialogParameters);
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = $"Error generando query: {ex.Message}",
                    Duration = 5000
                });
            }
        }
    }

    private async Task CopyToClipboard(string text)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Copiado",
                Detail = "Query copiada al portapapeles",
                Duration = 2000
            });
        }
        catch
        {
            // Fallback para navegadores que no soportan clipboard API
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Información",
                Detail = "No se pudo copiar automáticamente. Selecciona y copia manualmente.",
                Duration = 3000
            });
        }
    }

    private async Task SaveConfiguration()
    {
        if (filterConfigurationRef?.DataFilter?.Filters == null || !filterConfigurationRef.DataFilter.Filters.Any())
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Sin Filtros",
                Detail = "No hay filtros configurados para guardar",
                Duration = 3000
            });
            return;
        }

        var saveDialogParameters = new Dictionary<string, object>
        {
            ["EntityName"] = selectedEntityName ?? "",
            ["EntityDisplayName"] = selectedEntity?.DisplayName ?? "",
            ["Filters"] = filterConfigurationRef?.DataFilter?.Filters?.ToArray() ?? Array.Empty<CompositeFilterDescriptor>(),
            ["LogicalOperator"] = logicalOperator,
            ["FilterCaseSensitivity"] = filterCaseSensitivity,
            ["Take"] = takeLimit
        };

        var result = await DialogService.OpenAsync<Frontend.Components.AdvancedQuery.SaveConfigurationDialog>("Guardar Configuración", saveDialogParameters);

        if (result is bool success && success)
        {
            // Configuración guardada exitosamente
            StateHasChanged();
        }
    }

    private async Task LoadConfiguration()
    {
        if (string.IsNullOrEmpty(selectedEntityName))
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Seleccionar Entidad",
                Detail = "Primero debes seleccionar una entidad",
                Duration = 3000
            });
            return;
        }

        var loadDialogParameters = new Dictionary<string, object>
        {
            ["EntityName"] = selectedEntityName ?? ""
        };

        var result = await DialogService.OpenAsync<Frontend.Components.AdvancedQuery.LoadConfigurationDialog>("Cargar Configuración", loadDialogParameters);

        if (result is SavedQueryConfiguration config)
        {
            await ApplyConfiguration(config);
        }
    }

    private async Task ApplyConfiguration(SavedQueryConfiguration config)
    {
        try
        {
            // Aplicar configuración cargada
            logicalOperator = config.LogicalOperator;
            filterCaseSensitivity = config.FilterCaseSensitivity;
            if (config.Take.HasValue)
            {
                takeLimit = config.Take.Value;
            }

            // Limpiar filtros actuales
            if (filterConfigurationRef?.DataFilter != null)
            {
                await filterConfigurationRef.DataFilter.ClearFilters();

                // Aplicar filtros de la configuración
                var filters = config.GetFilters();
                foreach (var filter in filters)
                {
                    await filterConfigurationRef.DataFilter.AddFilter(filter);
                }

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Configuración Aplicada",
                    Detail = $"Se aplicó la configuración '{config.Name}' con {filters.Length} filtros",
                    Duration = 3000
                });
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error aplicando configuración: {ex.Message}",
                Duration = 5000
            });
        }
    }

    private async Task ViewDetails(object item)
    {
        var dialogParameters = new Dictionary<string, object>
        {
            ["Item"] = item,
            ["OnClose"] = EventCallback.Factory.Create(this, () => DialogService.Close())
        };

        await DialogService.OpenAsync<ViewDetailsModal>("Detalles del Registro", dialogParameters);
    }

    /// <summary>
    /// Abrir diálogo de selección de campos
    /// </summary>
    private async Task OpenFieldSelectionDialog()
    {
        if (!entityFields.Any()) return;

        // Configurar campos seleccionados basado en IsSelectedByDefault
        if (!selectedFields.Any())
        {
            var defaultFields = entityFields.Where(f => f.IsVisible && f.IsSelectedByDefault).ToList();
            Console.WriteLine($"Default fields found: {defaultFields.Count}");
            foreach (var field in defaultFields)
            {
                Console.WriteLine($"Default field: {field.PropertyName} (IsSelectedByDefault: {field.IsSelectedByDefault})");
                // Crear copia para evitar modificar el original
                var fieldCopy = new EntityFieldDefinition
                {
                    PropertyName = field.PropertyName,
                    DisplayName = field.DisplayName,
                    PropertyType = field.PropertyType,
                    IsNullable = field.IsNullable,
                    IsSearchable = field.IsSearchable,
                    FieldCategory = field.FieldCategory,
                    Description = field.Description,
                    IsVisible = field.IsVisible,
                    IsSelectedByDefault = field.IsSelectedByDefault,
                    SortOrder = field.SortOrder
                };
                selectedFields.Add(fieldCopy);
            }
            Console.WriteLine($"Selected fields initialized with {selectedFields.Count} fields");
        }

        var dialogParameters = new Dictionary<string, object>
        {
            ["AvailableFields"] = entityFields,
            ["InitialSelectedFields"] = selectedFields.ToList(), // Crear copia para evitar modificaciones
            ["OnApply"] = EventCallback.Factory.Create<List<EntityFieldDefinition>>(this, ApplyFieldSelection),
            ["OnCancel"] = EventCallback.Factory.Create(this, CancelFieldSelection)
        };

        var result = await DialogService.OpenAsync<Frontend.Components.AdvancedQuery.FieldSelectionDialog>("Configurar Campos Visibles",
            dialogParameters,
            new DialogOptions
            {
                Width = "900px",
                Height = "600px",
                Resizable = true,
                Draggable = true
            });

        // Si el usuario aplicó los cambios, mostrar notificación
        if (result is bool success && success)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Campos Configurados",
                Detail = $"Se configuraron {selectedFields.Count} campos para mostrar",
                Duration = 3000
            });
        }
    }

    private async Task ApplyFieldSelection(List<EntityFieldDefinition> newSelectedFields)
    {
        selectedFields = newSelectedFields;
        StateHasChanged();
        DialogService.Close(true);
    }

    private async Task CancelFieldSelection()
    {
        DialogService.Close(false);
    }

    /// <summary>
    /// Obtener campos para mostrar en la tabla de resultados
    /// </summary>
    private List<EntityFieldDefinition> GetDisplayFields()
    {
        // Si hay campos seleccionados, usar esos en el orden especificado
        if (selectedFields.Any())
        {
            return selectedFields;
        }

        // Si no hay selección, usar los campos por defecto (solo los seleccionados por defecto)
        var defaultFields = entityFields.Where(f => f.IsVisible && f.IsSelectedByDefault).ToList();
        if (defaultFields.Any())
        {
            return defaultFields;
        }

        // Fallback: mostrar máximo 6 campos visibles
        return entityFields.Where(f => f.IsVisible).Take(6).ToList();
    }
}