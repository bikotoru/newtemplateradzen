using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using System.Text.Json;
using Frontend.Services;
using Frontend.Pages.AdvancedQuery.Components;
using Frontend.Pages.AdvancedQuery.Components.Modals;
using Shared.Models.Requests;

namespace Frontend.Pages.AdvancedQuery;

// Clase simple para serializar filtros sin problemas de tipo Type
public class SerializableFilter
{
    public string PropertyName { get; set; } = "";
    public string Operator { get; set; } = "";
    public object? Value { get; set; }
    public string LogicalOperator { get; set; } = "And";
}

public partial class Page : ComponentBase
{
    [Inject] private AdvancedQueryService AdvancedQueryService { get; set; } = null!;
    [Inject] private SavedQueryService SavedQueryService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

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

    // Variables para PageWithCommandBar y SavedQueries
    private bool CanSave => !string.IsNullOrEmpty(selectedEntityName) && 
                           !string.IsNullOrWhiteSpace(queryName) &&
                           filterConfigurationRef?.DataFilter != null &&
                           HasConfiguredFilters();
    
    // SavedQueries state
    [Parameter, SupplyParameterFromQuery] public string? LoadQuery { get; set; }
    private SavedQueryDto? currentSavedQuery;
    private string queryName = "";

    protected override async Task OnInitializedAsync()
    {
        await LoadAvailableEntities();
        
        // Cargar búsqueda guardada si se especifica en la URL
        if (!string.IsNullOrEmpty(LoadQuery) && Guid.TryParse(LoadQuery, out var queryId))
        {
            await LoadSavedQuery(queryId);
        }
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
        if (!CanSave)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "No se puede guardar",
                Detail = "Debe seleccionar una entidad, ingresar un nombre y configurar al menos un filtro",
                Duration = 3000
            });
            return;
        }


        try
        {
            var savedQuery = new SavedQueryDto
            {
                Id = currentSavedQuery?.Id ?? Guid.NewGuid(),
                Name = queryName.Trim(),
                Description = $"Búsqueda en {selectedEntity?.DisplayName}",
                EntityName = selectedEntityName!,
                SelectedFields = JsonSerializer.Serialize(GetDisplayFields().Select(f => f.PropertyName)),
                FilterConfiguration = SerializeFilters(),
                LogicalOperator = (byte)logicalOperator,
                TakeLimit = takeLimit,
                IsPublic = false,
                IsTemplate = false,
                Active = true,
                FechaCreacion = currentSavedQuery?.FechaCreacion ?? DateTime.Now,
                FechaModificacion = DateTime.Now
            };

            if (currentSavedQuery != null)
            {
                // Actualizar búsqueda existente
                var updateRequest = new UpdateRequest<SavedQueryDto> { Entity = savedQuery };
                var response = await SavedQueryService.UpdateAsync(updateRequest, BackendType.FormBackend);
                
                if (response.Success)
                {
                    currentSavedQuery = response.Data;
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Búsqueda Actualizada",
                        Detail = $"La búsqueda '{queryName.Trim()}' ha sido actualizada",
                        Duration = 4000
                    });
                }
                else
                {
                    throw new Exception(response.Message ?? "Error al actualizar la búsqueda");
                }
            }
            else
            {
                // Crear nueva búsqueda
                var createRequest = new CreateRequest<SavedQueryDto> { Entity = savedQuery };
                var response = await SavedQueryService.CreateAsync(createRequest, BackendType.FormBackend);
                
                if (response.Success)
                {
                    currentSavedQuery = response.Data;
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Búsqueda Guardada",
                        Detail = $"La búsqueda '{queryName.Trim()}' ha sido guardada exitosamente",
                        Duration = 4000
                    });
                }
                else
                {
                    throw new Exception(response.Message ?? "Error al guardar la búsqueda");
                }
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error al guardar la búsqueda: {ex.Message}",
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

    /// <summary>
    /// Cargar una búsqueda guardada por ID
    /// </summary>
    private async Task LoadSavedQuery(Guid queryId)
    {
        try
        {
            isLoading = true;
            var response = await SavedQueryService.GetByIdAsync(queryId);
            
            if (!response.Success || response.Data == null)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Búsqueda no encontrada",
                    Detail = "La búsqueda guardada solicitada no se pudo cargar",
                    Duration = 4000
                });
                return;
            }

            currentSavedQuery = response.Data;
            
            // Cargar nombre de la búsqueda
            queryName = currentSavedQuery.Name;
            
            // Cargar la entidad
            selectedEntityName = currentSavedQuery.EntityName;
            await OnEntitySelected();
            
            // Esperar a que se cargue la entidad
            await Task.Delay(100);
            
            // Cargar configuración de campos
            if (!string.IsNullOrEmpty(currentSavedQuery.SelectedFields))
            {
                try
                {
                    var fieldNames = JsonSerializer.Deserialize<List<string>>(currentSavedQuery.SelectedFields);
                    if (fieldNames != null)
                    {
                        selectedFields = entityFields
                            .Where(f => fieldNames.Contains(f.PropertyName))
                            .OrderBy(f => fieldNames.IndexOf(f.PropertyName))
                            .ToList();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializing selected fields: {ex.Message}");
                }
            }
            
            // Cargar configuración de filtros
            if (!string.IsNullOrEmpty(currentSavedQuery.FilterConfiguration))
            {
                try
                {
                    var filters = DeserializeFilters(currentSavedQuery.FilterConfiguration);
                    if (filters.Any() && filterConfigurationRef?.DataFilter != null)
                    {
                        await Task.Delay(200); // Dar tiempo para que se inicialice el componente
                        // TODO: Implementar carga de filtros desde SerializableFilter - por ahora solo notificar
                        Console.WriteLine($"Filters to load: {filters.Count}");
                        foreach (var filter in filters)
                        {
                            Console.WriteLine($"Filter: {filter.PropertyName} {filter.Operator} {filter.Value}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading filters: {ex.Message}");
                }
            }
            
            // Cargar configuración de operador lógico y límite
            logicalOperator = (LogicalFilterOperator)currentSavedQuery.LogicalOperator;
            takeLimit = currentSavedQuery.TakeLimit;
            
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Búsqueda Cargada",
                Detail = $"Se ha cargado la búsqueda '{currentSavedQuery.Name}'",
                Duration = 4000
            });
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error al cargar la búsqueda: {ex.Message}",
                Duration = 5000
            });
        }
        finally
        {
            isLoading = false;
        }
    }

    /// <summary>
    /// Verificar si hay filtros configurados
    /// </summary>
    private bool HasConfiguredFilters()
    {
        try
        {
            return filterConfigurationRef?.DataFilter?.Filters?.Any() == true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Navegar a la página de administración de búsquedas guardadas
    /// </summary>
    private void NavigateToSavedQueries()
    {
        Navigation.NavigateTo("/advanced-query/saved-queries/list");
    }

    /// <summary>
    /// Serializa los filtros actuales de manera segura
    /// </summary>
    private string SerializeFilters()
    {
        try
        {
            var filters = filterConfigurationRef?.DataFilter?.Filters;
            if (filters == null || !filters.Any())
            {
                return JsonSerializer.Serialize(new List<SerializableFilter>());
            }

            var serializableFilters = filters.Select(f => new SerializableFilter
            {
                PropertyName = f.Property?.ToString() ?? "",
                Operator = f.FilterOperator.ToString(),
                Value = f.FilterValue,
                LogicalOperator = f.LogicalFilterOperator.ToString()
            }).ToList();

            return JsonSerializer.Serialize(serializableFilters);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error serializing filters: {ex.Message}");
            return JsonSerializer.Serialize(new List<SerializableFilter>());
        }
    }

    /// <summary>
    /// Deserializa los filtros guardados
    /// </summary>
    private List<SerializableFilter> DeserializeFilters(string? filterConfiguration)
    {
        try
        {
            if (string.IsNullOrEmpty(filterConfiguration))
            {
                return new List<SerializableFilter>();
            }

            var filters = JsonSerializer.Deserialize<List<SerializableFilter>>(filterConfiguration);
            return filters ?? new List<SerializableFilter>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deserializing filters: {ex.Message}");
            return new List<SerializableFilter>();
        }
    }
}