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
    private int takeLimit = 50;

    // Variables para selección de campos
    private List<EntityFieldDefinition> selectedFields = new();
    private bool showFieldSelectionDialog = false;
    private int selectedFieldsKey = 0; // Clave para forzar re-render

    // Variable para almacenar el último request ejecutado (para exportación)
    private AdvancedQueryRequest? lastExecutedRequest;

    // Variables para PageWithCommandBar
    private bool CanSave => false; // Por ahora no hay funcionalidad de guardado

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
            lastExecutedRequest = null;

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

            // Construir Select string con los campos seleccionados
            var selectFields = GetSelectedFieldsForQuery();

            var request = new AdvancedQueryRequest
            {
                Filters = filterConfigurationRef?.DataFilter?.Filters?.ToArray() ?? Array.Empty<CompositeFilterDescriptor>(),
                LogicalOperator = logicalOperator,
                FilterCaseSensitivity = FilterCaseSensitivity.CaseInsensitive,
                Take = takeLimit,
                Select = selectFields
            };

            // Almacenar el request para uso en exportación
            lastExecutedRequest = request;

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
            lastExecutedRequest = null;
            // Refrescar el estado del componente de filtros
            filterConfigurationRef.RefreshFilterState();
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

    private async Task SaveForm()
    {
        // Por ahora no hay funcionalidad de guardado específica
        // Se puede implementar más adelante si se necesita guardar configuraciones de consulta
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Info,
            Summary = "Información",
            Detail = "Funcionalidad de guardado no implementada",
            Duration = 3000
        });
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

    /// <summary>
    /// Obtener string de campos seleccionados para el Select de la query
    /// </summary>
    private string? GetSelectedFieldsForQuery()
    {
        var fieldsToSelect = GetDisplayFields();

        if (!fieldsToSelect.Any())
        {
            // Si no hay campos seleccionados, no usar Select (traer todo)
            return null;
        }

        // Construir string con sintaxis para System.Linq.Dynamic.Core
        // Formato: "new (Id, Nombre)" en lugar de "Id, Nombre"
        var fieldNames = string.Join(", ", fieldsToSelect.Select(f => f.PropertyName));
        var selectString = $"new ({fieldNames})";

        Console.WriteLine($"Generated Select string: {selectString}");
        return selectString;
    }
}