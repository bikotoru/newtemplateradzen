using Microsoft.Extensions.Logging;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.QueryModels;
using System.Linq.Expressions;
using Radzen;

namespace Frontend.Services
{
    public abstract class BaseApiService<T> where T : class
    {
        protected readonly API _api;
        protected readonly ILogger<BaseApiService<T>> _logger;
        protected readonly string _baseUrl;
        private readonly QueryService _queryService;

        protected BaseApiService(API api, ILogger<BaseApiService<T>> logger, string baseUrl)
        {
            _api = api;
            _logger = logger;
            _baseUrl = baseUrl.TrimEnd('/');
            _queryService = new QueryService(_api);
        }

        #region Individual Operations

        /// <summary>
        /// Crear una entidad individual
        /// </summary>
        public virtual async Task<ApiResponse<T>> CreateAsync(CreateRequest<T> request)
        {
            _logger.LogInformation($"Creating {typeof(T).Name}");
            return await _api.PostAsync<T>($"{_baseUrl}/create", request);
        }

        /// <summary>
        /// Actualizar una entidad individual
        /// </summary>
        public virtual async Task<ApiResponse<T>> UpdateAsync(UpdateRequest<T> request)
        {
            _logger.LogInformation($"Updating {typeof(T).Name}");
            return await _api.PutAsync<T>($"{_baseUrl}/update", request);
        }

        /// <summary>
        /// Obtener todos los registros (paginado por defecto)
        /// </summary>
        public virtual async Task<ApiResponse<PagedResponse<T>>> GetAllPagedAsync(int page = 1, int pageSize = 10)
        {
            _logger.LogInformation($"Getting paged {typeof(T).Name} - Page: {page}, PageSize: {pageSize}");
            return await _api.GetAsync<PagedResponse<T>>($"{_baseUrl}/all?page={page}&pageSize={pageSize}&all=false");
        }

        /// <summary>
        /// Obtener todos los registros sin paginación
        /// </summary>
        public virtual async Task<ApiResponse<List<T>>> GetAllUnpagedAsync()
        {
            _logger.LogInformation($"Getting all {typeof(T).Name} (unpaged)");
            return await _api.GetAsync<List<T>>($"{_baseUrl}/all?all=true");
        }

        /// <summary>
        /// Obtener por ID
        /// </summary>
        public virtual async Task<ApiResponse<T>> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Getting {typeof(T).Name} by ID: {id}");
            return await _api.GetAsync<T>($"{_baseUrl}/{id}");
        }

        /// <summary>
        /// Eliminar por ID
        /// </summary>
        public virtual async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting {typeof(T).Name} with ID: {id}");
            return await _api.DeleteAsync<bool>($"{_baseUrl}/{id}");
        }

        #endregion

        #region Batch Operations

        /// <summary>
        /// Crear múltiples entidades
        /// </summary>
        public virtual async Task<ApiResponse<BatchResponse<T>>> CreateBatchAsync(CreateBatchRequest<T> batchRequest)
        {
            _logger.LogInformation($"Creating batch of {batchRequest.Requests?.Count ?? 0} {typeof(T).Name}");
            return await _api.PostAsync<BatchResponse<T>>($"{_baseUrl}/create-batch", batchRequest);
        }

        /// <summary>
        /// Actualizar múltiples entidades
        /// </summary>
        public virtual async Task<ApiResponse<BatchResponse<T>>> UpdateBatchAsync(UpdateBatchRequest<T> batchRequest)
        {
            _logger.LogInformation($"Updating batch of {batchRequest.Requests?.Count ?? 0} {typeof(T).Name}");
            return await _api.PutAsync<BatchResponse<T>>($"{_baseUrl}/update-batch", batchRequest);
        }

        #endregion

        #region Health Check

        /// <summary>
        /// Health check endpoint
        /// </summary>
        public virtual async Task<ApiResponse<object>> HealthCheckAsync()
        {
            _logger.LogInformation($"Performing health check for {typeof(T).Name}");
            return await _api.GetAsync<object>($"{_baseUrl}/health");
        }

        #endregion

        #region Query Operations (Dynamic Queries)

        public virtual async Task<ApiResponse<List<T>>> QueryAsync(QueryRequest queryRequest)
        {
            try
            {
                _logger.LogInformation($"Executing query for {typeof(T).Name}");
                return await _api.PostAsync<List<T>>($"{_baseUrl}/query", queryRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception executing query for {typeof(T).Name}");
                return ApiResponse<List<T>>.ErrorResponse($"Exception executing query: {ex.Message}");
            }
        }

        public virtual async Task<ApiResponse<PagedResult<T>>> QueryPagedAsync(QueryRequest queryRequest)
        {
            try
            {
                _logger.LogInformation($"Executing paged query for {typeof(T).Name}");
                return await _api.PostAsync<PagedResult<T>>($"{_baseUrl}/paged", queryRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception executing paged query for {typeof(T).Name}");
                return ApiResponse<PagedResult<T>>.ErrorResponse($"Exception executing paged query: {ex.Message}");
            }
        }

        public virtual async Task<ApiResponse<List<object>>> QuerySelectAsync(QueryRequest queryRequest)
        {
            try
            {
                _logger.LogInformation($"Executing select query for {typeof(T).Name}");
                return await _api.PostAsync<List<object>>($"{_baseUrl}/select", queryRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception executing select query for {typeof(T).Name}");
                return ApiResponse<List<object>>.ErrorResponse($"Exception executing select query: {ex.Message}");
            }
        }

        public virtual async Task<ApiResponse<PagedResult<object>>> QuerySelectPagedAsync(QueryRequest queryRequest)
        {
            try
            {
                _logger.LogInformation($"Executing paged select query for {typeof(T).Name}");
                return await _api.PostAsync<PagedResult<object>>($"{_baseUrl}/select-paged", queryRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception executing paged select query for {typeof(T).Name}");
                return ApiResponse<PagedResult<object>>.ErrorResponse($"Exception executing paged select query: {ex.Message}");
            }
        }

        #endregion

        #region Search Operations (Intelligent Search)

        public virtual async Task<ApiResponse<List<T>>> SearchAsync(SearchRequest searchRequest)
        {
            try
            {
                _logger.LogInformation($"Executing search for {typeof(T).Name} with term: {searchRequest.SearchTerm}");
                return await _api.PostAsync<List<T>>($"{_baseUrl}/search", searchRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception executing search for {typeof(T).Name}");
                return ApiResponse<List<T>>.ErrorResponse($"Exception executing search: {ex.Message}");
            }
        }

        public virtual async Task<ApiResponse<PagedResult<T>>> SearchPagedAsync(SearchRequest searchRequest)
        {
            try
            {
                _logger.LogInformation($"Executing paged search for {typeof(T).Name} with term: {searchRequest.SearchTerm}");
                return await _api.PostAsync<PagedResult<T>>($"{_baseUrl}/search-paged", searchRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception executing paged search for {typeof(T).Name}");
                return ApiResponse<PagedResult<T>>.ErrorResponse($"Exception executing paged search: {ex.Message}");
            }
        }

        public virtual async Task<ApiResponse<List<object>>> SearchSelectAsync(SearchRequest searchRequest)
        {
            try
            {
                _logger.LogInformation($"Executing search select for {typeof(T).Name} with term: {searchRequest.SearchTerm}");
                return await _api.PostAsync<List<object>>($"{_baseUrl}/search-select", searchRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception executing search select for {typeof(T).Name}");
                return ApiResponse<List<object>>.ErrorResponse($"Exception executing search select: {ex.Message}");
            }
        }

        public virtual async Task<ApiResponse<PagedResult<object>>> SearchSelectPagedAsync(SearchRequest searchRequest)
        {
            try
            {
                _logger.LogInformation($"Executing paged search select for {typeof(T).Name} with term: {searchRequest.SearchTerm}");
                return await _api.PostAsync<PagedResult<object>>($"{_baseUrl}/search-select-paged", searchRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception executing paged search select for {typeof(T).Name}");
                return ApiResponse<PagedResult<object>>.ErrorResponse($"Exception executing paged search select: {ex.Message}");
            }
        }

        public virtual async Task<ApiResponse<List<T>>> SearchAsync(string searchTerm, string[]? searchFields = null, QueryRequest? baseQuery = null)
        {
            var searchRequest = new SearchRequest
            {
                SearchTerm = searchTerm,
                SearchFields = searchFields ?? Array.Empty<string>(),
                BaseQuery = baseQuery
            };

            return await SearchAsync(searchRequest);
        }

        public virtual async Task<ApiResponse<PagedResult<T>>> SearchPagedAsync(string searchTerm, int page = 1, int pageSize = 10, string[]? searchFields = null, QueryRequest? baseQuery = null)
        {
            var searchRequest = new SearchRequest
            {
                SearchTerm = searchTerm,
                SearchFields = searchFields ?? Array.Empty<string>(),
                BaseQuery = baseQuery,
                Skip = (page - 1) * pageSize,
                Take = pageSize
            };

            return await SearchPagedAsync(searchRequest);
        }

        #endregion

        #region Strongly Typed Query Operations

        public virtual QueryBuilder<T> Query()
        {
            return _queryService.For<T>(_baseUrl);
        }

        public virtual QueryBuilder<T> QueryAsync()
        {
            return Query();
        }

        #endregion

        #region Radzen Integration (LoadDataArgs)

        public virtual async Task<ApiResponse<PagedResult<T>>> LoadDataAsync(LoadDataArgs args)
        {
            try
            {
                _logger.LogInformation($"LoadDataAsync basic for {typeof(T).Name} - Skip: {args.Skip}, Top: {args.Top}");
                
                var queryRequest = ConvertLoadDataArgsToQueryRequest(args, null, null);
                _logger.LogInformation($"Using QueryPagedAsync with Filter: '{queryRequest.Filter ?? "null"}'");
                return await QueryPagedAsync(queryRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync for {typeof(T).Name}");
                return ApiResponse<PagedResult<T>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        public virtual async Task<ApiResponse<PagedResult<T>>> LoadDataAsync(
            LoadDataArgs args,
            params Expression<Func<T, object>>[] searchFields)
        {
            try
            {
                var query = ConvertLoadDataArgsToQuery(args, null, searchFields);
                var result = await query.ToPagedResultAsync();
                return ApiResponse<PagedResult<T>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync with typed search fields for {typeof(T).Name}");
                return ApiResponse<PagedResult<T>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        public virtual async Task<ApiResponse<PagedResult<T>>> LoadDataAsync(
            LoadDataArgs args,
            List<string> searchFields)
        {
            try
            {
                var query = ConvertLoadDataArgsToQuery(args, null, null, searchFields);
                var result = await query.ToPagedResultAsync();
                return ApiResponse<PagedResult<T>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync with string search fields for {typeof(T).Name}");
                return ApiResponse<PagedResult<T>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        public virtual async Task<ApiResponse<PagedResult<T>>> LoadDataAsync(
            LoadDataArgs args,
            QueryBuilder<T>? baseQuery = null,
            params Expression<Func<T, object>>[] searchFields)
        {
            try
            {
                var query = ConvertLoadDataArgsToQuery(args, baseQuery, searchFields);
                var result = await query.ToPagedResultAsync();
                return ApiResponse<PagedResult<T>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync with base query and typed search fields for {typeof(T).Name}");
                return ApiResponse<PagedResult<T>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        public virtual async Task<ApiResponse<PagedResult<T>>> LoadDataAsync(
            LoadDataArgs args,
            QueryBuilder<T>? baseQuery,
            List<string> searchFields)
        {
            try
            {
                var query = ConvertLoadDataArgsToQuery(args, baseQuery, null, searchFields);
                var result = await query.ToPagedResultAsync();
                return ApiResponse<PagedResult<T>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync with base query and string search fields for {typeof(T).Name}");
                return ApiResponse<PagedResult<T>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        public virtual async Task<ApiResponse<PagedResult<TResult>>> LoadDataAsync<TResult>(
            LoadDataArgs args,
            Expression<Func<T, TResult>> selector,
            QueryBuilder<T>? baseQuery = null,
            params Expression<Func<T, object>>[] searchFields)
        {
            try
            {
                var query = ConvertLoadDataArgsToQuery(args, baseQuery, searchFields);
                var result = await query.Select(selector).ToPagedResultAsync();
                return ApiResponse<PagedResult<TResult>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync with select and typed search fields for {typeof(T).Name}");
                return ApiResponse<PagedResult<TResult>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        public virtual async Task<ApiResponse<PagedResult<TResult>>> LoadDataAsync<TResult>(
            LoadDataArgs args,
            Expression<Func<T, TResult>> selector,
            QueryBuilder<T>? baseQuery,
            List<string> searchFields)
        {
            try
            {
                var query = ConvertLoadDataArgsToQuery(args, baseQuery, null, searchFields);
                var result = await query.Select(selector).ToPagedResultAsync();
                return ApiResponse<PagedResult<TResult>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync with select and string search fields for {typeof(T).Name}");
                return ApiResponse<PagedResult<TResult>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        public virtual async Task<ApiResponse<PagedResult<dynamic>>> LoadDataAsync(
            LoadDataArgs args,
            List<string> selectFields,
            QueryBuilder<T>? baseQuery = null,
            List<string>? searchFields = null)
        {
            try
            {
                var queryRequest = ConvertLoadDataArgsToQueryRequest(args, baseQuery, searchFields);
                queryRequest.Select = string.Join(", ", selectFields);
                
                var result = await QuerySelectPagedAsync(queryRequest);
                if (result.Success && result.Data != null)
                {
                    var pagedResult = new PagedResult<dynamic>
                    {
                        Data = result.Data.Data?.Cast<dynamic>().ToList() ?? new List<dynamic>(),
                        TotalCount = result.Data.TotalCount,
                        Page = result.Data.Page,
                        PageSize = result.Data.PageSize
                    };
                    return ApiResponse<PagedResult<dynamic>>.SuccessResponse(pagedResult);
                }
                return ApiResponse<PagedResult<dynamic>>.ErrorResponse(result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync with string select fields for {typeof(T).Name}");
                return ApiResponse<PagedResult<dynamic>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        #endregion

        #region Private Helper Methods for LoadData

        private QueryBuilder<T> ConvertLoadDataArgsToQuery(
            LoadDataArgs args, 
            QueryBuilder<T>? baseQuery = null,
            Expression<Func<T, object>>[]? typedSearchFields = null,
            List<string>? stringSearchFields = null)
        {
            var query = baseQuery ?? Query();
            query._baseUrl = _baseUrl;
            if (args.Filters != null && args.Filters.Any())
            {
                foreach (var filter in args.Filters)
                {
                    var filterString = ConvertRadzenFilterToString(filter);
                    if (!string.IsNullOrEmpty(filterString))
                    {
                        _logger.LogWarning($"Filter conversion not fully implemented: {filterString}");
                    }
                }
            }

            if (args.Sorts != null && args.Sorts.Any())
            {
                foreach (var sort in args.Sorts)
                {
                    if (!string.IsNullOrEmpty(sort.Property))
                    {
                        var propertyInfo = typeof(T).GetProperty(sort.Property);
                        if (propertyInfo != null)
                        {
                            var parameter = Expression.Parameter(typeof(T), "x");
                            var property = Expression.Property(parameter, propertyInfo);
                            var lambda = Expression.Lambda(property, parameter);
                            
                            try
                            {
                                var isDescending = sort.SortOrder == SortOrder.Descending;
                                var orderByMethod = typeof(QueryBuilder<T>).GetMethods()
                                    .FirstOrDefault(m => m.Name == "OrderBy" && 
                                                   m.IsGenericMethodDefinition && 
                                                   m.GetParameters().Length == 2 &&
                                                   m.GetParameters()[1].ParameterType == typeof(bool));
                                
                                if (orderByMethod != null)
                                {
                                    var genericMethod = orderByMethod.MakeGenericMethod(propertyInfo.PropertyType);
                                    query = (QueryBuilder<T>)genericMethod.Invoke(query, new object[] { lambda, isDescending })!;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error applying sort to {sort.Property}: {ex.Message}");
                            }
                        }
                    }
                }
            }

            // Only use args.Filter as search if there are no column filters (args.Filters)
            // RadzenDataGrid puts LINQ expressions in args.Filter when column filters are used
            bool hasColumnFilters = args.Filters != null && args.Filters.Any();
            if (!string.IsNullOrEmpty(args.Filter) && !hasColumnFilters)
            {
                query = query.Search(args.Filter);
                
                if (typedSearchFields != null && typedSearchFields.Any())
                {
                    query = query.InFields(typedSearchFields);
                }
                else if (stringSearchFields != null && stringSearchFields.Any())
                {
                    _logger.LogInformation($"String search fields will be applied via SearchRequest: {string.Join(", ", stringSearchFields)}");
                }
            }
            else if (!string.IsNullOrEmpty(args.Filter) && hasColumnFilters)
            {
                _logger.LogDebug($"Ignoring args.Filter (LINQ expression) because column filters are present: {args.Filter}");
            }

            if (args.Skip.HasValue)
            {
                query = query.Skip(args.Skip.Value);
            }

            if (args.Top.HasValue)
            {
                query = query.Take(args.Top.Value);
            }

            return query;
        }

        private QueryRequest ConvertLoadDataArgsToQueryRequest(
            LoadDataArgs args,
            QueryBuilder<T>? baseQuery = null,
            List<string>? searchFields = null)
        {
            var queryRequest = new QueryRequest();

            if (baseQuery != null)
            {
                _logger.LogWarning("Base query extraction not fully implemented for QueryRequest conversion");
            }

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
                    _logger.LogDebug($"OrderBy applied to QueryRequest: {queryRequest.OrderBy}");
                }
            }

            queryRequest.Skip = args.Skip;
            queryRequest.Take = args.Top;

            return queryRequest;
        }

        private string ConvertRadzenFilterToString(FilterDescriptor filter)
        {
            if (filter == null || string.IsNullOrEmpty(filter.Property)) 
                return string.Empty;

            var property = filter.Property;
            var value = filter.FilterValue?.ToString() ?? "";
            
            // Para strings, usar comparaciones case-insensitive con null check
            // Para números y booleans, usar comparación directa
            var propertyType = typeof(T).GetProperty(property)?.PropertyType;
            bool isString = propertyType == typeof(string);
            bool isNumeric = IsNumericType(propertyType);
            
            return filter.FilterOperator switch
            {
                FilterOperator.Equals when isString => $"({property} != null && {property}.ToLower() == \"{value.ToLower()}\")",
                FilterOperator.Equals => $"{property} == {(isNumeric ? value : $"\"{value}\"")}",
                
                FilterOperator.NotEquals when isString => $"({property} == null || {property}.ToLower() != \"{value.ToLower()}\")",
                FilterOperator.NotEquals => $"{property} != {(isNumeric ? value : $"\"{value}\"")}",
                
                FilterOperator.Contains => $"({property} != null && {property}.ToLower().Contains(\"{value.ToLower()}\"))",
                FilterOperator.DoesNotContain => $"({property} == null || !{property}.ToLower().Contains(\"{value.ToLower()}\"))",
                FilterOperator.StartsWith => $"({property} != null && {property}.ToLower().StartsWith(\"{value.ToLower()}\"))",
                FilterOperator.EndsWith => $"({property} != null && {property}.ToLower().EndsWith(\"{value.ToLower()}\"))",
                
                FilterOperator.GreaterThan => $"{property} > {(isNumeric ? value : $"\"{value}\"")}",
                FilterOperator.GreaterThanOrEquals => $"{property} >= {(isNumeric ? value : $"\"{value}\"")}",
                FilterOperator.LessThan => $"{property} < {(isNumeric ? value : $"\"{value}\"")}",
                FilterOperator.LessThanOrEquals => $"{property} <= {(isNumeric ? value : $"\"{value}\"")}",
                
                FilterOperator.IsNull => $"{property} == null",
                FilterOperator.IsNotNull => $"{property} != null",
                FilterOperator.IsEmpty => $"string.IsNullOrEmpty({property})",
                FilterOperator.IsNotEmpty => $"!string.IsNullOrEmpty({property})",
                _ => string.Empty
            };
        }
        
        private bool IsNumericType(Type? type)
        {
            if (type == null) return false;
            return type == typeof(int) || type == typeof(int?) ||
                   type == typeof(long) || type == typeof(long?) ||
                   type == typeof(decimal) || type == typeof(decimal?) ||
                   type == typeof(double) || type == typeof(double?) ||
                   type == typeof(float) || type == typeof(float?);
        }

        private Expression<Func<T, bool>> CreateFilterExpression(FilterDescriptor filter)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, filter.Property);
            
            var propertyType = typeof(T).GetProperty(filter.Property)?.PropertyType;
            bool isString = propertyType == typeof(string);
            bool isNumeric = IsNumericType(propertyType);
            
            var value = filter.FilterValue?.ToString() ?? "";
            var constantValue = Expression.Constant(isNumeric ? 
                (object?)Convert.ChangeType(value, propertyType!) : value);

            Expression comparison = filter.FilterOperator switch
            {
                FilterOperator.Equals when isString => CreateStringEqualsExpression(property, value),
                FilterOperator.Equals => Expression.Equal(property, constantValue),
                
                FilterOperator.NotEquals when isString => CreateStringNotEqualsExpression(property, value),
                FilterOperator.NotEquals => Expression.NotEqual(property, constantValue),
                
                FilterOperator.Contains => CreateStringContainsExpression(property, value),
                FilterOperator.DoesNotContain => CreateStringNotContainsExpression(property, value),
                FilterOperator.StartsWith => CreateStringStartsWithExpression(property, value),
                FilterOperator.EndsWith => CreateStringEndsWithExpression(property, value),
                
                FilterOperator.GreaterThan => Expression.GreaterThan(property, constantValue),
                FilterOperator.GreaterThanOrEquals => Expression.GreaterThanOrEqual(property, constantValue),
                FilterOperator.LessThan => Expression.LessThan(property, constantValue),
                FilterOperator.LessThanOrEquals => Expression.LessThanOrEqual(property, constantValue),
                
                FilterOperator.IsNull => Expression.Equal(property, Expression.Constant(null)),
                FilterOperator.IsNotNull => Expression.NotEqual(property, Expression.Constant(null)),
                FilterOperator.IsEmpty => Expression.Call(null, typeof(string).GetMethod("IsNullOrEmpty")!, property),
                FilterOperator.IsNotEmpty => Expression.Not(Expression.Call(null, typeof(string).GetMethod("IsNullOrEmpty")!, property)),
                
                _ => Expression.Equal(property, constantValue)
            };

            return Expression.Lambda<Func<T, bool>>(comparison, parameter);
        }

        private Expression CreateStringEqualsExpression(Expression property, string value)
        {
            var nullCheck = Expression.NotEqual(property, Expression.Constant(null));
            var toLower = Expression.Call(property, "ToLower", null);
            var equals = Expression.Equal(toLower, Expression.Constant(value.ToLower()));
            return Expression.AndAlso(nullCheck, equals);
        }

        private Expression CreateStringNotEqualsExpression(Expression property, string value)
        {
            var isNull = Expression.Equal(property, Expression.Constant(null));
            var toLower = Expression.Call(property, "ToLower", null);
            var notEquals = Expression.NotEqual(toLower, Expression.Constant(value.ToLower()));
            return Expression.OrElse(isNull, notEquals);
        }

        private Expression CreateStringContainsExpression(Expression property, string value)
        {
            var nullCheck = Expression.NotEqual(property, Expression.Constant(null));
            var toLower = Expression.Call(property, "ToLower", null);
            var contains = Expression.Call(toLower, "Contains", null, Expression.Constant(value.ToLower()));
            return Expression.AndAlso(nullCheck, contains);
        }

        private Expression CreateStringNotContainsExpression(Expression property, string value)
        {
            var isNull = Expression.Equal(property, Expression.Constant(null));
            var toLower = Expression.Call(property, "ToLower", null);
            var notContains = Expression.Not(Expression.Call(toLower, "Contains", null, Expression.Constant(value.ToLower())));
            return Expression.OrElse(isNull, notContains);
        }

        private Expression CreateStringStartsWithExpression(Expression property, string value)
        {
            var nullCheck = Expression.NotEqual(property, Expression.Constant(null));
            var toLower = Expression.Call(property, "ToLower", null);
            var startsWith = Expression.Call(toLower, "StartsWith", null, Expression.Constant(value.ToLower()));
            return Expression.AndAlso(nullCheck, startsWith);
        }

        private Expression CreateStringEndsWithExpression(Expression property, string value)
        {
            var nullCheck = Expression.NotEqual(property, Expression.Constant(null));
            var toLower = Expression.Call(property, "ToLower", null);
            var endsWith = Expression.Call(toLower, "EndsWith", null, Expression.Constant(value.ToLower()));
            return Expression.AndAlso(nullCheck, endsWith);
        }

        private string ConvertRadzenSortToString(SortDescriptor sort)
        {
            if (sort == null || string.IsNullOrEmpty(sort.Property))
                return string.Empty;

            var direction = sort.SortOrder == SortOrder.Descending ? " desc" : " asc";
            return $"{sort.Property}{direction}";
        }

        #endregion

        #region Excel Export Operations

        public virtual async Task<byte[]> ExportToExcelAsync(Shared.Models.Export.ExcelExportRequest exportRequest)
        {
            try
            {
                _logger.LogInformation($"Starting Excel export for {typeof(T).Name}");
                var excelBytes = await _api.PostFileAsync($"{_baseUrl}/export/excel", exportRequest);
                
                _logger.LogInformation($"Excel export completed for {typeof(T).Name}. Downloaded {excelBytes.Length} bytes");
                return excelBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception exporting to Excel for {typeof(T).Name}");
                throw new InvalidOperationException($"Exception exporting to Excel: {ex.Message}", ex);
            }
        }

        public virtual async Task DownloadExcelAsync(Shared.Models.Export.ExcelExportRequest exportRequest, FileDownloadService fileDownloadService, string? fileName = null)
        {
            try
            {
                _logger.LogInformation($"Starting Excel download for {typeof(T).Name}");

                var excelBytes = await ExportToExcelAsync(exportRequest);
                var finalFileName = fileName ?? $"{typeof(T).Name}_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                
                await fileDownloadService.DownloadExcelAsync(excelBytes, finalFileName);
                
                _logger.LogInformation($"Excel file downloaded successfully: {finalFileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception downloading Excel for {typeof(T).Name}");
                throw new InvalidOperationException($"Error descargando Excel: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exporta a Excel usando LoadDataArgs (convierte automáticamente)
        /// </summary>
        public virtual async Task<byte[]> ExportToExcelAsync(LoadDataArgs args, List<Shared.Models.Export.ExcelColumnConfig>? columns = null, string? fileName = null)
        {
            try
            {
                _logger.LogInformation($"Converting LoadDataArgs to ExcelExportRequest for {typeof(T).Name}");
                
                // Convertir LoadDataArgs a QueryRequest (sin paginación para exportar todo)
                var queryRequest = ConvertLoadDataArgsToQueryRequest(args);
                queryRequest.Skip = null; // Quitar paginación
                queryRequest.Take = null; // Quitar paginación
                
                // Crear ExcelExportRequest
                var exportRequest = new Shared.Models.Export.ExcelExportRequest
                {
                    Query = queryRequest,
                    Columns = columns ?? new List<Shared.Models.Export.ExcelColumnConfig>(),
                    SheetName = typeof(T).Name,
                    Title = $"Exportación de {typeof(T).Name}",
                    Subtitle = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}",
                    IncludeHeaders = true,
                    AutoFilter = true,
                    FreezeHeaders = true,
                    FormatAsTable = true,
                    AutoFitColumns = true
                };
                
                return await ExportToExcelAsync(exportRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception converting LoadDataArgs to Excel export for {typeof(T).Name}");
                throw new InvalidOperationException($"Error exportando con LoadDataArgs: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Descarga Excel usando LoadDataArgs
        /// </summary>
        public virtual async Task DownloadExcelAsync(LoadDataArgs args, FileDownloadService fileDownloadService, List<Shared.Models.Export.ExcelColumnConfig>? columns = null, string? fileName = null)
        {
            try
            {
                var excelBytes = await ExportToExcelAsync(args, columns, fileName);
                var finalFileName = fileName ?? $"{typeof(T).Name}_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                
                await fileDownloadService.DownloadExcelAsync(excelBytes, finalFileName);
                
                _logger.LogInformation($"Excel file downloaded successfully: {finalFileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception downloading Excel with LoadDataArgs for {typeof(T).Name}");
                throw new InvalidOperationException($"Error descargando Excel con LoadDataArgs: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exporta a Excel simple (sin filtros)
        /// </summary>
        public virtual async Task<byte[]> ExportToExcelAsync(List<Shared.Models.Export.ExcelColumnConfig>? columns = null, string? fileName = null)
        {
            try
            {
                _logger.LogInformation($"Starting simple Excel export for {typeof(T).Name}");
                
                // Crear ExcelExportRequest básico
                var exportRequest = new Shared.Models.Export.ExcelExportRequest
                {
                    Query = new QueryRequest(), // Query vacío = todos los datos
                    Columns = columns ?? new List<Shared.Models.Export.ExcelColumnConfig>(),
                    SheetName = typeof(T).Name,
                    Title = $"Exportación de {typeof(T).Name}",
                    Subtitle = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}",
                    IncludeHeaders = true,
                    AutoFilter = true,
                    FreezeHeaders = true,
                    FormatAsTable = true,
                    AutoFitColumns = true
                };
                
                return await ExportToExcelAsync(exportRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in simple Excel export for {typeof(T).Name}");
                throw new InvalidOperationException($"Error en exportación simple: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Descarga Excel simple (sin filtros)
        /// </summary>
        public virtual async Task DownloadExcelAsync(FileDownloadService fileDownloadService, List<Shared.Models.Export.ExcelColumnConfig>? columns = null, string? fileName = null)
        {
            try
            {
                var excelBytes = await ExportToExcelAsync(columns, fileName);
                var finalFileName = fileName ?? $"{typeof(T).Name}_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                
                await fileDownloadService.DownloadExcelAsync(excelBytes, finalFileName);
                
                _logger.LogInformation($"Excel file downloaded successfully: {finalFileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception downloading simple Excel for {typeof(T).Name}");
                throw new InvalidOperationException($"Error descargando Excel simple: {ex.Message}", ex);
            }
        }

        #endregion
    }
}