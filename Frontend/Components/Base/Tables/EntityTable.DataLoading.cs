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
        args = SetLoadData(args);
        
        if (isLoading)
        {
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
                
                
                bool hasSearch = !string.IsNullOrWhiteSpace(searchTerm);
                bool hasFilters = args.Filters != null && args.Filters.Any();
                
                // Si tiene ApiEndpoint personalizado, usar llamada directa a API
                if (!string.IsNullOrEmpty(ApiEndpoint))
                {
                    var API = ServiceProvider.GetRequiredService<Frontend.Services.API>();
                    
                    // Construir query básico con paginación y ordenamiento
                    var queryRequest = new QueryRequest
                    {
                        Skip = args.Skip ?? 0,
                        Take = args.Top ?? PageSize,
                        OrderBy = args.OrderBy
                    };
                    
                    // Agregar filtros de columna si existen
                    var allFilters = new List<string>();
                    
                    // Si tiene BaseQuery, incluir sus filtros como string
                    if (queryWithFilters != null)
                    {
                        var baseQueryRequest = queryWithFilters.ToQueryRequest();
                        if (!string.IsNullOrEmpty(baseQueryRequest.Filter))
                        {
                            allFilters.Add($"({baseQueryRequest.Filter})");
                        }
                    }
                    
                    // Agregar filtros de columna (args.Filters)
                    if (args.Filters != null && args.Filters.Any())
                    {
                        var columnFilters = args.Filters.Select(ConvertRadzenFilterToString).Where(f => !string.IsNullOrEmpty(f));
                        foreach (var filter in columnFilters)
                        {
                            allFilters.Add($"({filter})");
                        }
                    }
                    
                    // Combinar todos los filtros
                    if (allFilters.Any())
                    {
                        queryRequest.Filter = string.Join(" and ", allFilters);
                    }
                    
                    var response = await API.PostAsync<Shared.Models.Responses.PagedResponse<T>>(ApiEndpoint, queryRequest);
                    
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
                else if (hasFilters && !hasSearch)
                {
                    
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
                else if (hasSearch)
                {
                    
                    var searchRequest = new SearchRequest
                    {
                        SearchTerm = searchTerm,
                        Skip = args.Skip ?? 0,
                        Take = args.Top ?? PageSize,
                        OrderBy = args.OrderBy,
                        SearchFields = GetEffectiveSearchFields().ToArray(),
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
                }
            }
            catch (Exception ex)
            {
                // Silently skip conversion errors
            }
        }
        
        return result;
    }

    private T? CreatePartialEntity(JsonElement jsonElement)
    {
        try
        {
            var entity = Activator.CreateInstance<T>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);


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
                        }
                    }
                    catch (Exception ex)
                    {
                        // Silently skip property setting errors
                    }
                }
            }

            return entity;
        }
        catch (Exception ex)
        {
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