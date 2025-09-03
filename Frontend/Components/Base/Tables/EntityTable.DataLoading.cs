using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Models.QueryModels;
using Shared.Models.Requests;
using System.Text.Json;
using System.Reflection;

namespace Frontend.Components.Base.Tables;

public partial class EntityTable<T>
{
    #region Data Loading Methods

    private async Task LoadData(LoadDataArgs args)
    {
        var callId = Guid.NewGuid().ToString()[..8];
        Console.WriteLine($"[DEBUG] ==> LoadData INICIO #{callId} con searchTerm: '{searchTerm}'");
        Console.WriteLine($"[DEBUG] ==> #{callId} ColumnConfigs Count: {ColumnConfigs?.Count ?? 0}");
        Console.WriteLine($"[DEBUG] ==> #{callId} isLoading: {isLoading}");
        
        if (ColumnConfigs != null)
        {
            Console.WriteLine($"[DEBUG] ==> #{callId} ColumnConfigs en LoadData:");
            foreach (var col in ColumnConfigs.OrderBy(c => c.Order ?? 999))
            {
                Console.WriteLine($"[DEBUG] ==> #{callId}   - {col.Property} ({col.Title}) - Visible: {col.Visible}, Order: {col.Order}");
            }
        }
        
        if (args.Filters != null && args.Filters.Any())
        {
            Console.WriteLine($"[DEBUG] ==> #{callId} FILTROS DE RADZEN DETECTADOS:");
            foreach (var filter in args.Filters)
            {
                Console.WriteLine($"[DEBUG] ==> #{callId}   - {filter.Property}: {filter.FilterValue} ({filter.FilterOperator})");
            }
        }
        else
        {
            Console.WriteLine($"[DEBUG] ==> #{callId} No hay filtros de RadzenDataGrid");
        }
        
        if (isLoading)
        {
            Console.WriteLine($"[DEBUG] ==> #{callId} YA ESTÁ CARGANDO, SALTANDO...");
            return;
        }
        
        try
        {
            isLoading = true;
            lastLoadDataArgs = args;

            if (OnLoadData.HasDelegate)
            {
                await OnLoadData.InvokeAsync(args);
            }
            else if (apiService != null)
            {
                var queryWithFilters = BaseQuery;
                
                Console.WriteLine($"[DEBUG] args.Filter = '{args.Filter ?? "null"}'");
                
                if (args.Filters != null && args.Filters.Any())
                {
                    Console.WriteLine($"[FILTROS] RadzenDataGrid enviará {args.Filters.Count()} filtros al backend:");
                    
                    foreach (var filter in args.Filters)
                    {
                        Console.WriteLine($"[FILTROS]   ✓ {filter.Property}: {filter.FilterValue} ({filter.FilterOperator})");
                    }
                }
                else
                {
                    Console.WriteLine("[FILTROS] No hay filtros de RadzenDataGrid");
                }
                
                bool hasSearch = !string.IsNullOrWhiteSpace(searchTerm);
                bool hasFilters = args.Filters != null && args.Filters.Any();
                
                if (hasFilters && !hasSearch)
                {
                    Console.WriteLine($"[DEBUG] ==> Ejecutando LoadData con filtros de RadzenDataGrid aplicados");
                    
                    var response = await apiService.LoadDataAsync(args);
                    
                    if (response.Success && response.Data != null)
                    {
                        entities = response.Data.Data;
                        totalCount = response.Data.TotalCount;
                    }
                    else
                    {
                        entities = new List<T>();
                        totalCount = 0;
                    }
                }
                else if (hasSearch)
                {
                    Console.WriteLine($"[DEBUG] ==> Ejecutando SearchRequest con término de búsqueda");
                    
                    var searchRequest = new SearchRequest
                    {
                        SearchTerm = searchTerm,
                        Skip = args.Skip ?? 0,
                        Take = args.Top ?? PageSize,
                        OrderBy = args.OrderBy,
                        SearchFields = effectiveSearchFields?.ToArray(),
                        BaseQuery = queryWithFilters?.ToQueryRequest()
                    };
                    
                    var searchResponse = await apiService.SearchPagedAsync(searchRequest);
                    
                    if (searchResponse.Success && searchResponse.Data != null)
                    {
                        entities = searchResponse.Data.Data;
                        totalCount = searchResponse.Data.TotalCount;
                    }
                    else
                    {
                        entities = new List<T>();
                        totalCount = 0;
                    }
                }
                else
                {
                    Console.WriteLine($"[DEBUG] ==> Ejecutando LoadData estándar");
                    
                    var response = queryWithFilters != null 
                        ? await apiService.LoadDataAsync(args, queryWithFilters)
                        : await apiService.LoadDataAsync(args);
                    
                    if (response.Success && response.Data != null)
                    {
                        entities = response.Data.Data;
                        totalCount = response.Data.TotalCount;
                    }
                    else
                    {
                        entities = new List<T>();
                        totalCount = 0;
                    }
                }
            }
            else
            {
                entities = new List<T>();
                totalCount = 0;
            }

            if (OnAfterLoadData.HasDelegate)
            {
                await OnAfterLoadData.InvokeAsync(args);
            }

            var test = grid.ColumnsCollection;
        }
        catch (Exception ex)
        {
            entities = new List<T>();
            totalCount = 0;
            await DialogService.Alert($"Error al cargar datos: {ex.Message}", "Error");
        }
        finally
        {
            Console.WriteLine($"[DEBUG] ==> LoadData TERMINADO #{callId}");
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadDataWithSelectOptimization(LoadDataArgs args)
    {
        try
        {
            var selectString = BuildSelectString();
            
            var query = BaseQuery != null
                ? apiService!.Query().And(BaseQuery)
                : apiService!.Query();

            var queryRequest = ConvertLoadDataArgsToQueryRequestWithSelect(args, selectString);
            
            var response = await apiService.QuerySelectPagedAsync(queryRequest);
            
            if (response.Success && response.Data != null)
            {
                entities = ConvertSelectResultsToEntityType(response.Data.Data);
                totalCount = response.Data.TotalCount;
            }
            else
            {
                entities = new List<T>();
                totalCount = 0;
            }
        }
        catch (Exception)
        {
            var response = BaseQuery != null 
                ? await apiService!.LoadDataAsync(args, BaseQuery)
                : await apiService!.LoadDataAsync(args);
            
            if (response.Success && response.Data != null)
            {
                entities = response.Data.Data;
                totalCount = response.Data.TotalCount;
            }
            else
            {
                entities = new List<T>();
                totalCount = 0;
            }
        }
    }

    private async Task LoadDataWithSearch(LoadDataArgs args)
    {
        try
        {
            var searchFields = GetEffectiveSearchFields();
            var searchRequest = BuildSearchRequest(args, searchFields);
            
            if (ShouldUseSelectOptimization())
            {
                await LoadDataWithSearchAndSelectOptimization(args, searchRequest);
            }
            else
            {
                var response = await apiService!.SearchPagedAsync(searchRequest);
                
                if (response.Success && response.Data != null)
                {
                    entities = response.Data.Data;
                    totalCount = response.Data.TotalCount;
                }
                else
                {
                    entities = new List<T>();
                    totalCount = 0;
                }
            }
        }
        catch (Exception)
        {
            var response = BaseQuery != null 
                ? await apiService!.LoadDataAsync(args, BaseQuery)
                : await apiService!.LoadDataAsync(args);
            
            if (response.Success && response.Data != null)
            {
                entities = response.Data.Data;
                totalCount = response.Data.TotalCount;
            }
            else
            {
                entities = new List<T>();
                totalCount = 0;
            }
        }
    }

    private async Task LoadDataWithSearchAndSelectOptimization(LoadDataArgs args, SearchRequest searchRequest)
    {
        try
        {
            var selectString = BuildSelectString();
            
            if (searchRequest.BaseQuery != null)
            {
                searchRequest.BaseQuery.Select = selectString;
            }
            else
            {
                searchRequest.BaseQuery = new QueryRequest { Select = selectString };
            }
            
            var response = await apiService!.SearchSelectPagedAsync(searchRequest);
            
            if (response.Success && response.Data != null)
            {
                entities = ConvertSelectResultsToEntityType(response.Data.Data);
                totalCount = response.Data.TotalCount;
            }
            else
            {
                entities = new List<T>();
                totalCount = 0;
            }
        }
        catch (Exception)
        {
            var response = await apiService!.SearchPagedAsync(searchRequest);
            
            if (response.Success && response.Data != null)
            {
                entities = response.Data.Data;
                totalCount = response.Data.TotalCount;
            }
            else
            {
                entities = new List<T>();
                totalCount = 0;
            }
        }
    }

    private List<T> ConvertSelectResultsToEntityType(List<object> selectResults)
    {
        var result = new List<T>();
        
        Console.WriteLine($"[DEBUG] Convirtiendo {selectResults.Count} elementos de select results");
        
        foreach (var item in selectResults)
        {
            try
            {
                T? entity = default;
                
                if (item is System.Text.Json.JsonElement jsonElement)
                {
                    entity = CreatePartialEntity(jsonElement);
                }
                else if (item is string jsonString)
                {
                    var jsonElem = JsonSerializer.Deserialize<JsonElement>(jsonString);
                    entity = CreatePartialEntity(jsonElem);
                }
                else
                {
                    var json = JsonSerializer.Serialize(item);
                    var jsonElem = JsonSerializer.Deserialize<JsonElement>(json);
                    entity = CreatePartialEntity(jsonElem);
                }

                if (entity != null)
                {
                    result.Add(entity);
                    Console.WriteLine($"[DEBUG] Entidad convertida exitosamente");
                }
                else
                {
                    Console.WriteLine($"[DEBUG] No se pudo convertir entidad");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Error convirtiendo elemento: {ex.Message}");
            }
        }
        
        Console.WriteLine($"[DEBUG] Resultado final: {result.Count} entidades convertidas");
        return result;
    }

    private T? CreatePartialEntity(JsonElement jsonElement)
    {
        try
        {
            var entity = Activator.CreateInstance<T>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            Console.WriteLine($"[DEBUG] Creando entidad parcial para tipo {typeof(T).Name}");

            foreach (var property in properties)
            {
                var propertyFound = false;
                JsonElement propertyValue = default;

                if (jsonElement.TryGetProperty(property.Name, out propertyValue))
                {
                    propertyFound = true;
                }
                else if (jsonElement.TryGetProperty(char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1), out propertyValue))
                {
                    propertyFound = true;
                }

                if (propertyFound)
                {
                    try
                    {
                        object? value = null;

                        if (property.PropertyType == typeof(string))
                        {
                            value = propertyValue.GetString();
                        }
                        else if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?))
                        {
                            if (propertyValue.ValueKind == JsonValueKind.String && Guid.TryParse(propertyValue.GetString(), out var guid))
                            {
                                value = guid;
                            }
                        }
                        else if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
                        {
                            if (propertyValue.ValueKind == JsonValueKind.True || propertyValue.ValueKind == JsonValueKind.False)
                            {
                                value = propertyValue.GetBoolean();
                            }
                        }
                        else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                        {
                            if (propertyValue.ValueKind == JsonValueKind.String && DateTime.TryParse(propertyValue.GetString(), out var date))
                            {
                                value = date;
                            }
                        }
                        else
                        {
                            value = JsonSerializer.Deserialize(propertyValue.GetRawText(), property.PropertyType);
                        }

                        if (value != null)
                        {
                            property.SetValue(entity, value);
                            Console.WriteLine($"[DEBUG] Propiedad {property.Name} establecida con valor {value}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEBUG] Error estableciendo propiedad {property.Name}: {ex.Message}");
                    }
                }
            }

            return entity;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Error creando entidad parcial: {ex.Message}");
            return default(T);
        }
    }

    private QueryRequest ConvertLoadDataArgsToQueryRequestWithSelect(LoadDataArgs args, string selectString)
    {
        var queryRequest = new QueryRequest
        {
            Select = selectString,
            Skip = args.Skip,
            Take = args.Top
        };

        if (args.Filters != null && args.Filters.Any())
        {
            var filters = args.Filters.Select(ConvertRadzenFilterToString).Where(f => !string.IsNullOrEmpty(f));
            if (filters.Any())
            {
                queryRequest.Filter = string.Join(" && ", filters);
            }
        }

        if (args.Sorts != null && args.Sorts.Any())
        {
            var sorts = args.Sorts.Select(ConvertRadzenSortToString).Where(s => !string.IsNullOrEmpty(s));
            if (sorts.Any())
            {
                queryRequest.OrderBy = string.Join(", ", sorts);
            }
        }

        return queryRequest;
    }

    #endregion
}