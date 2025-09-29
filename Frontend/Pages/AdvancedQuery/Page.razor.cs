using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using System.Text.Json;
using Frontend.Services;
using Frontend.Pages.AdvancedQuery.Components;
using Frontend.Pages.AdvancedQuery.Components.Modals;
using Frontend.Components.CustomRadzen.QueryBuilder;
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
    
    // Filtros pendientes para cargar cuando DataFilter esté listo
    private List<SerializableFilter>? pendingFilters;
    
    // Modal states for sharing
    private bool showShareModal = false;

    // Variables para PageWithCommandBar y SavedQueries
    private bool CanSave => !string.IsNullOrEmpty(selectedEntityName) && 
                           !string.IsNullOrWhiteSpace(queryName) &&
                           filterConfigurationRef?.DataFilter != null &&
                           HasConfiguredFilters();
    
    // SavedQueries state
    [Parameter, SupplyParameterFromQuery] public string? LoadQuery { get; set; }
    [Parameter, SupplyParameterFromQuery] public string? Mode { get; set; }
    private SavedQueryDto? currentSavedQuery;
    private string queryName = "";
    
    // Play mode properties
    private bool IsPlayMode => !string.IsNullOrEmpty(Mode) && Mode.ToLower() == "play";
    private bool IsReadOnlyMode => IsPlayMode;
    private List<SerializableFilter> readOnlyFilters = new();

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
        // En modo play, permitir ejecutar sin filtros si no hay filtros configurados
        var hasFilters = IsPlayMode ? 
            (readOnlyFilters?.Any() == true) : 
            (filterConfigurationRef?.DataFilter?.Filters?.Any() == true);
            
        if (!IsPlayMode && !HasAnyFilters())
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

            // En modo play, convertir filtros serializados a CompositeFilterDescriptor
            CompositeFilterDescriptor[] filters;
            if (IsPlayMode && readOnlyFilters?.Any() == true)
            {
                var compositeFilters = new List<CompositeFilterDescriptor>();
                foreach (var filter in readOnlyFilters)
                {
                    if (Enum.TryParse<FilterOperator>(filter.Operator, out var filterOperator) &&
                        Enum.TryParse<LogicalFilterOperator>(filter.LogicalOperator, out var logicalOperator))
                    {
                        compositeFilters.Add(new CompositeFilterDescriptor
                        {
                            Property = filter.PropertyName,
                            FilterOperator = filterOperator,
                            FilterValue = filter.Value,
                            LogicalFilterOperator = logicalOperator
                        });
                    }
                }
                filters = compositeFilters.ToArray();
            }
            else
            {
                filters = filterConfigurationRef?.DataFilter?.Filters?.ToArray() ?? Array.Empty<CompositeFilterDescriptor>();
            }

            var request = new AdvancedQueryRequest
            {
                Filters = filters,
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
            await ClearAllFilters();
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
            Console.WriteLine($"🔄 LoadSavedQuery started for ID: {queryId}");
            isLoading = true;
            
            Console.WriteLine("📡 Calling SavedQueryService.GetByIdAsync...");
            var response = await SavedQueryService.GetByIdAsync(queryId);
            
            if (!response.Success || response.Data == null)
            {
                Console.WriteLine($"❌ Failed to load saved query: {response.Message}");
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Búsqueda no encontrada",
                    Detail = "La búsqueda guardada solicitada no se pudo cargar",
                    Duration = 4000
                });
                return;
            }

            Console.WriteLine($"✅ Saved query loaded successfully: {response.Data.Name}");
            currentSavedQuery = response.Data;
            
            // Cargar nombre de la búsqueda
            queryName = currentSavedQuery.Name;
            Console.WriteLine($"📝 Query name set: {queryName}");
            
            // Cargar la entidad
            selectedEntityName = currentSavedQuery.EntityName;
            Console.WriteLine($"🏷️ Entity name set: {selectedEntityName}");
            
            Console.WriteLine("🔄 Calling OnEntitySelected...");
            await OnEntitySelected();
            
            // Esperar a que se cargue la entidad
            Console.WriteLine("⏳ Waiting for entity to load...");
            await Task.Delay(100);
            
            Console.WriteLine($"📊 Entity fields loaded: {entityFields.Count} fields");
            foreach (var field in entityFields)
            {
                Console.WriteLine($"  - {field.PropertyName} ({field.PropertyType?.Name})");
            }
            
            // Cargar configuración de campos
            if (!string.IsNullOrEmpty(currentSavedQuery.SelectedFields))
            {
                try
                {
                    Console.WriteLine($"🔧 Loading selected fields: {currentSavedQuery.SelectedFields}");
                    var fieldNames = JsonSerializer.Deserialize<List<string>>(currentSavedQuery.SelectedFields);
                    if (fieldNames != null)
                    {
                        selectedFields = entityFields
                            .Where(f => fieldNames.Contains(f.PropertyName))
                            .OrderBy(f => fieldNames.IndexOf(f.PropertyName))
                            .ToList();
                        Console.WriteLine($"✅ Selected fields loaded: {selectedFields.Count} fields");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error deserializing selected fields: {ex.Message}");
                }
            }
            
            // Cargar configuración de filtros
            Console.WriteLine($"🔍 Checking filter configuration...");
            Console.WriteLine($"FilterConfiguration: {currentSavedQuery.FilterConfiguration}");
            
            if (!string.IsNullOrEmpty(currentSavedQuery.FilterConfiguration))
            {
                try
                {
                    Console.WriteLine("📋 Deserializing filters...");
                    var filters = DeserializeFilters(currentSavedQuery.FilterConfiguration);
                    Console.WriteLine($"✅ Deserialized {filters.Count} filters");
                    
                    // Almacenar filtros para modo play
                    readOnlyFilters = filters;
                    
                    foreach (var filter in filters)
                    {
                        Console.WriteLine($"  - Filter: {filter.PropertyName} {filter.Operator} {filter.Value}");
                    }
                    
                    if (filters.Any())
                    {
                        Console.WriteLine("🔄 Checking filterConfigurationRef...");
                        if (filterConfigurationRef == null)
                        {
                            Console.WriteLine("❌ filterConfigurationRef is null!");
                        }
                        else
                        {
                            Console.WriteLine("✅ filterConfigurationRef found");
                            
                            // Intentar múltiples veces hasta que DataFilter esté disponible
                            var maxAttempts = 10;
                            var attempt = 0;
                            var delayMs = 200;
                            
                            while (!HasDataFilter() && attempt < maxAttempts)
                            {
                                attempt++;
                                Console.WriteLine($"⏳ Attempt {attempt}/{maxAttempts}: DataFilter is null, waiting {delayMs}ms...");
                                await Task.Delay(delayMs);
                                
                                // Forzar re-render para asegurar que el componente se inicialice
                                StateHasChanged();
                                await Task.Delay(100);
                                
                                // Aumentar el delay gradualmente
                                delayMs = Math.Min(delayMs + 100, 1000);
                            }
                            
                            if (HasDataFilter())
                            {
                                Console.WriteLine($"✅ DataFilter found after {attempt} attempts!");
                                Console.WriteLine("🚀 Starting LoadFiltersIntoDataFilter...");
                                await LoadFiltersIntoDataFilter(filters);
                            }
                            else
                            {
                                Console.WriteLine($"❌ DataFilter is still null after {maxAttempts} attempts");
                                Console.WriteLine("💾 Storing filters to load when DataFilter becomes ready...");
                                pendingFilters = filters;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ No filters to load");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error loading filters: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Filtros no cargados",
                        Detail = "No se pudieron cargar los filtros guardados. Puede configurarlos manualmente.",
                        Duration = 4000
                    });
                }
            }
            else
            {
                Console.WriteLine("⚠️ FilterConfiguration is empty");
            }
            
            // Cargar configuración de operador lógico y límite
            logicalOperator = (LogicalFilterOperator)currentSavedQuery.LogicalOperator;
            takeLimit = currentSavedQuery.TakeLimit;
            Console.WriteLine($"⚙️ LogicalOperator: {logicalOperator}, TakeLimit: {takeLimit}");
            
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Búsqueda Cargada",
                Detail = $"Se ha cargado la búsqueda '{currentSavedQuery.Name}'",
                Duration = 4000
            });
            
            Console.WriteLine("🔄 Calling StateHasChanged...");
            StateHasChanged();
            Console.WriteLine("✅ LoadSavedQuery completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ LoadSavedQuery failed: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
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
            Console.WriteLine("🏁 LoadSavedQuery finished");
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
    /// Cambiar del modo play al modo edit
    /// </summary>
    private void SwitchToEditMode()
    {
        if (currentSavedQuery != null)
        {
            // Navegar a la misma página pero sin el parámetro mode=play
            Navigation.NavigateTo($"/advanced-query?loadQuery={currentSavedQuery.Id}");
        }
    }
    
    /// <summary>
    /// Abrir modal de compartir
    /// </summary>
    private async Task OpenShareModal()
    {
        if (currentSavedQuery != null)
        {
            showShareModal = true;
            StateHasChanged();
        }
    }
    
    /// <summary>
    /// Cerrar modal de compartir
    /// </summary>
    private async Task OnShareModalClosed()
    {
        showShareModal = false;
        StateHasChanged();
    }
    
    /// <summary>
    /// Callback cuando se comparte la búsqueda
    /// </summary>
    private async Task OnQueryShared()
    {
        showShareModal = false;
        
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Success,
            Summary = "¡Éxito!",
            Detail = "Búsqueda guardada compartida exitosamente",
            Duration = 4000
        });
        
        StateHasChanged();
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

    /// <summary>
    /// Carga los filtros deserializados en el componente RadzenDataFilter
    /// </summary>
    private async Task LoadFiltersIntoDataFilter(List<SerializableFilter> serializableFilters)
    {
        try
        {
            Console.WriteLine($"LoadFiltersIntoDataFilter called with {serializableFilters.Count} filters");
            
            if (filterConfigurationRef == null)
            {
                Console.WriteLine("filterConfigurationRef is null");
                return;
            }

            if (!HasDataFilter())
            {
                Console.WriteLine("DataFilter is null, waiting a bit more...");
                await Task.Delay(1000); // Esperar más tiempo

                if (!HasDataFilter())
                {
                    Console.WriteLine("DataFilter is still null after waiting");
                    return;
                }
            }

            Console.WriteLine("DataFilter found, clearing existing filters");
            
            // Limpiar filtros existentes
            await ClearAllFilters();

            Console.WriteLine("Starting to convert and add filters");

            // Convertir SerializableFilter a CompositeFilterDescriptor y agregar uno por uno
            foreach (var serializableFilter in serializableFilters)
            {
                Console.WriteLine($"Processing filter: {serializableFilter.PropertyName} {serializableFilter.Operator} {serializableFilter.Value}");
                
                var compositeFilter = ConvertToCompositeFilterDescriptor(serializableFilter);
                if (compositeFilter != null)
                {
                    Console.WriteLine($"Filter converted successfully, adding to DataFilter");
                    
                    // Agregar filtro al DataFilter usando diferentes enfoques
                    try
                    {
                        // Método 1: Usar helper method
                        await AddFilterToDataFilter(compositeFilter);
                    }
                    catch (Exception addEx)
                    {
                        Console.WriteLine($"✗ Failed to add filter directly: {addEx.Message}");

                        // Método 2: Usar helper method con lista
                        try
                        {
                            await AddFilterToDataFilter(compositeFilter, useListMethod: true);
                        }
                        catch (Exception listEx)
                        {
                            Console.WriteLine($"✗ Failed to add filter using list method: {listEx.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"✗ Failed to convert filter: {serializableFilter.PropertyName}");
                }
            }

            Console.WriteLine("Refreshing filter state and forcing re-render");
            
            // Forzar actualización del componente con múltiples enfoques
            filterConfigurationRef.RefreshFilterState();
            await InvokeAsync(StateHasChanged);
            
            // Esperar un poco y volver a actualizar
            await Task.Delay(100);
            await InvokeAsync(StateHasChanged);
            
            // Verificar si los filtros se agregaron correctamente
            var filterCount = GetCurrentFilterCount();
            Console.WriteLine($"Current filter count in DataFilter: {filterCount}");

            Console.WriteLine($"✓ Successfully loaded {serializableFilters.Count} filters into DataFilter");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error loading filters into DataFilter: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Convierte un SerializableFilter a CompositeFilterDescriptor
    /// </summary>
    private CompositeFilterDescriptor? ConvertToCompositeFilterDescriptor(SerializableFilter serializableFilter)
    {
        try
        {
            Console.WriteLine($"Converting filter - Property: {serializableFilter.PropertyName}, Operator: {serializableFilter.Operator}");
            
            // Buscar el campo correspondiente en entityFields
            var field = entityFields.FirstOrDefault(f => f.PropertyName == serializableFilter.PropertyName);
            if (field == null)
            {
                Console.WriteLine($"✗ Field not found: {serializableFilter.PropertyName}");
                Console.WriteLine($"Available fields: {string.Join(", ", entityFields.Select(f => f.PropertyName))}");
                return null;
            }

            Console.WriteLine($"✓ Field found: {field.PropertyName} (Type: {field.PropertyType?.Name})");

            // Convertir el operador string a FilterOperator enum
            if (!Enum.TryParse<FilterOperator>(serializableFilter.Operator, out var filterOperator))
            {
                Console.WriteLine($"✗ Invalid filter operator: {serializableFilter.Operator}");
                Console.WriteLine($"Available operators: {string.Join(", ", Enum.GetNames<FilterOperator>())}");
                return null;
            }

            Console.WriteLine($"✓ Filter operator parsed: {filterOperator}");

            // Convertir el operador lógico string a LogicalFilterOperator enum
            if (!Enum.TryParse<LogicalFilterOperator>(serializableFilter.LogicalOperator, out var logicalOperator))
            {
                logicalOperator = LogicalFilterOperator.And; // Default
                Console.WriteLine($"Using default logical operator: {logicalOperator}");
            }
            else
            {
                Console.WriteLine($"✓ Logical operator parsed: {logicalOperator}");
            }

            // Convertir el valor
            var convertedValue = ConvertFilterValue(serializableFilter.Value, field.PropertyType?.Name ?? "string");
            Console.WriteLine($"✓ Filter value converted: {convertedValue} (original: {serializableFilter.Value})");

            // Crear el CompositeFilterDescriptor
            var compositeFilter = new CompositeFilterDescriptor
            {
                Property = serializableFilter.PropertyName,
                FilterOperator = filterOperator,
                FilterValue = convertedValue,
                LogicalFilterOperator = logicalOperator
            };

            Console.WriteLine($"✓ CompositeFilterDescriptor created successfully");
            return compositeFilter;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error converting SerializableFilter to CompositeFilterDescriptor: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// Se ejecuta cuando el DataFilter del componente FilterConfiguration está listo
    /// </summary>
    private async Task OnDataFilterReady()
    {
        Console.WriteLine("🎯 OnDataFilterReady called");
        
        if (pendingFilters != null && pendingFilters.Any())
        {
            Console.WriteLine($"🔄 Loading {pendingFilters.Count} pending filters...");
            await LoadFiltersIntoDataFilter(pendingFilters);
            pendingFilters = null; // Limpiar filtros pendientes
        }
        else
        {
            Console.WriteLine("📭 No pending filters to load");
        }
    }

    /// <summary>
    /// Convierte el valor del filtro al tipo correcto basado en el tipo de propiedad
    /// </summary>
    private object? ConvertFilterValue(object? value, string propertyType)
    {
        try
        {
            if (value == null) return null;

            var valueString = value.ToString();
            if (string.IsNullOrEmpty(valueString)) return null;

            return propertyType.ToLower() switch
            {
                "int" or "int32" => int.TryParse(valueString, out var intVal) ? intVal : null,
                "long" or "int64" => long.TryParse(valueString, out var longVal) ? longVal : null,
                "decimal" => decimal.TryParse(valueString, out var decVal) ? decVal : null,
                "double" => double.TryParse(valueString, out var doubleVal) ? doubleVal : null,
                "float" => float.TryParse(valueString, out var floatVal) ? floatVal : null,
                "bool" or "boolean" => bool.TryParse(valueString, out var boolVal) ? boolVal : null,
                "datetime" => DateTime.TryParse(valueString, out var dateVal) ? dateVal : null,
                "guid" => Guid.TryParse(valueString, out var guidVal) ? guidVal : null,
                _ => valueString // Default to string
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting filter value: {ex.Message}");
            return value; // Return original value if conversion fails
        }
    }

    #region Helper Methods for CustomDataFilter Integration

    /// <summary>
    /// Verifica si hay algún DataFilter disponible (Radzen o Custom)
    /// </summary>
    private bool HasDataFilter()
    {
        return filterConfigurationRef?.DataFilter != null || filterConfigurationRef?.CustomDataFilter != null;
    }

    /// <summary>
    /// Verifica si hay filtros en cualquier DataFilter
    /// </summary>
    private bool HasAnyFilters()
    {
        var radzenHasFilters = filterConfigurationRef?.DataFilter?.Filters?.Any() == true;
        var customHasFilters = filterConfigurationRef?.CustomDataFilter?.Filters?.Any() == true;
        return radzenHasFilters || customHasFilters;
    }

    /// <summary>
    /// Limpia todos los filtros de los DataFilters disponibles
    /// </summary>
    private async Task ClearAllFilters()
    {
        if (filterConfigurationRef?.DataFilter != null)
        {
            await ClearAllFilters();
        }
        if (filterConfigurationRef?.CustomDataFilter != null)
        {
            await filterConfigurationRef.CustomDataFilter.ClearFilters();
        }
    }

    /// <summary>
    /// Agrega un filtro al DataFilter disponible
    /// </summary>
    private async Task AddFilterToDataFilter(CompositeFilterDescriptor compositeFilter, bool useListMethod = false)
    {
        if (filterConfigurationRef?.CustomDataFilter != null)
        {
            // Usar CustomDataFilter preferentemente
            await filterConfigurationRef.CustomDataFilter.AddFilter(compositeFilter);
            Console.WriteLine($"✓ Added filter to CustomDataFilter: {compositeFilter.Property} {compositeFilter.FilterOperator} {compositeFilter.FilterValue}");
        }
        else if (filterConfigurationRef?.DataFilter != null)
        {
            // Fallback a RadzenDataFilter
            if (!useListMethod)
            {
                try
                {
                    ((IList<CompositeFilterDescriptor>)filterConfigurationRef.DataFilter.Filters).Add(compositeFilter);
                    Console.WriteLine($"✓ Added filter to RadzenDataFilter (direct): {compositeFilter.Property} {compositeFilter.FilterOperator} {compositeFilter.FilterValue}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Failed to add filter directly to RadzenDataFilter: {ex.Message}");
                    throw;
                }
            }
            else
            {
                try
                {
                    var currentFilters = filterConfigurationRef.DataFilter.Filters?.ToList() ?? new List<CompositeFilterDescriptor>();
                    currentFilters.Add(compositeFilter);
                    filterConfigurationRef.DataFilter.Filters = currentFilters;
                    Console.WriteLine($"✓ Added filter to RadzenDataFilter (list method): {compositeFilter.Property}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Failed to add filter using list method to RadzenDataFilter: {ex.Message}");
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Obtiene el número actual de filtros
    /// </summary>
    private int GetCurrentFilterCount()
    {
        var radzenCount = filterConfigurationRef?.DataFilter?.Filters?.Count() ?? 0;
        var customCount = filterConfigurationRef?.CustomDataFilter?.Filters?.Count() ?? 0;
        return Math.Max(radzenCount, customCount);
    }

    #endregion
}