using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Text.Json;
using Shared.Models.QueryModels;

namespace Shared.Models.Services
{
    public class QueryService
    {
        private readonly HttpClient _http;

        public QueryService(HttpClient http)
        {
            _http = http;
        }

        public QueryBuilder<T> For<T>() where T : class
        {
            return new QueryBuilder<T>(_http, typeof(T).Name);
        }
    }

    public class QueryBuilder<T> where T : class
    {
        protected readonly HttpClient _http;
        protected readonly string _entityName;
        protected readonly List<Expression<Func<T, bool>>> _filters = new();
        protected LambdaExpression? _orderByExpression;
        protected bool _orderByDescending = false;
        protected readonly List<string> _includeExpressions = new();
        protected int? _skip;
        protected int? _take;
        
        // Search properties
        protected string? _searchTerm;
        protected readonly List<Expression<Func<T, object>>> _searchFields = new();

        public QueryBuilder(HttpClient http, string entityName)
        {
            _http = http;
            _entityName = entityName;
        }

        public QueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
        {
            _filters.Add(predicate);
            return this;
        }

        public QueryBuilder<T> OrderBy<TProperty>(Expression<Func<T, TProperty>> property, bool descending = false)
        {
            _orderByExpression = property;
            _orderByDescending = descending;
            return this;
        }

        public SelectQueryBuilder<T, TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
        {
            return new SelectQueryBuilder<T, TResult>(_http, _entityName, _filters, _orderByExpression, 
                _orderByDescending, selector, _includeExpressions, _skip, _take, _searchTerm, _searchFields);
        }

        public QueryBuilder<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationProperty)
        {
            var propertyPath = GetPropertyPath(navigationProperty.Body);
            _includeExpressions.Add(propertyPath);
            return this;
        }

        public QueryBuilder<T> Skip(int count)
        {
            _skip = count;
            return this;
        }

        public QueryBuilder<T> Take(int count)
        {
            _take = count;
            return this;
        }

        /// <summary>
        /// Búsqueda inteligente por término (funciona con strings, números, fechas)
        /// </summary>
        public QueryBuilder<T> Search(string searchTerm)
        {
            _searchTerm = searchTerm;
            return this;
        }

        /// <summary>
        /// Especificar campos donde buscar (fuertemente tipado)
        /// </summary>
        public QueryBuilder<T> InFields(params Expression<Func<T, object>>[] searchFields)
        {
            _searchFields.Clear();
            _searchFields.AddRange(searchFields);
            return this;
        }

        /// <summary>
        /// Agregar campo adicional para búsqueda (fuertemente tipado)
        /// </summary>
        public QueryBuilder<T> AlsoInField<TProperty>(Expression<Func<T, TProperty>> searchField)
        {
            _searchFields.Add(ConvertToObjectExpression(searchField));
            return this;
        }

        public async Task<List<T>> ToListAsync(bool autoInclude = false)
        {
            // Si hay búsqueda, usar endpoint de Search
            if (!string.IsNullOrWhiteSpace(_searchTerm))
            {
                return await ExecuteSearchAsync(autoInclude);
            }

            // Usar endpoint de Query normal
            var request = new QueryRequest
            {
                Filter = BuildFilterString(),
                OrderBy = BuildOrderByString(),
                Include = autoInclude ? null : new string[0],
                Skip = _skip,
                Take = _take
            };

            var response = await _http.PostAsJsonAsync($"/api/query/{_entityName}/query", request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Query failed: {response.StatusCode} - {errorContent}");
            }
            
            var result = await response.Content.ReadFromJsonAsync<List<T>>();
            return result ?? new List<T>();
        }

        /// <summary>
        /// Convierte el QueryBuilder a un QueryRequest para uso con exportaciones u otros servicios
        /// </summary>
        public QueryRequest ToQueryRequest(bool autoInclude = false)
        {
            // Si hay búsqueda, crear un SearchRequest en lugar de QueryRequest
            if (!string.IsNullOrWhiteSpace(_searchTerm))
            {
                // Para búsquedas, crear un QueryRequest con los filtros base
                var baseQuery = new QueryRequest
                {
                    Filter = BuildFilterString(),
                    OrderBy = BuildOrderByString(),
                    Include = autoInclude ? null : _includeExpressions.ToArray(),
                    Skip = _skip,
                    Take = _take
                };

                // TODO: Para exportaciones con búsqueda, se podría crear un SearchRequest
                // Por ahora retornamos el QueryRequest base
                return baseQuery;
            }

            // Query normal sin búsqueda
            return new QueryRequest
            {
                Filter = BuildFilterString(),
                OrderBy = BuildOrderByString(),
                Include = autoInclude ? null : _includeExpressions.ToArray(),
                Skip = _skip,
                Take = _take
            };
        }

        public async Task<PagedResult<T>> ToPagedResultAsync(bool autoInclude = false)
        {
            // Si hay búsqueda, usar endpoint de Search paginado
            if (!string.IsNullOrWhiteSpace(_searchTerm))
            {
                return await ExecuteSearchPagedAsync(autoInclude);
            }

            // Usar endpoint de Query paginado normal
            var request = ToQueryRequest(autoInclude);

            var response = await _http.PostAsJsonAsync($"/api/query/{_entityName}/paged", request);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<PagedResult<T>>();
            return result ?? new PagedResult<T>();
        }

        protected string? BuildFilterString()
        {
            if (!_filters.Any()) return null;
            
            var filterStrings = _filters.Select(expr => ExpressionToString(expr.Body)).ToList();
            return string.Join(" && ", filterStrings);
        }

        protected string? BuildOrderByString()
        {
            if (_orderByExpression == null) return null;
            
            var propertyPath = GetPropertyPath(_orderByExpression.Body);
            return _orderByDescending ? $"{propertyPath} desc" : propertyPath;
        }

        protected string ExpressionToString(Expression expression)
        {
            return expression switch
            {
                BinaryExpression binary => HandleBinaryExpression(binary),
                MemberExpression member => GetPropertyPath(member),
                ConstantExpression constant => FormatConstant(constant.Value),
                MethodCallExpression method => HandleMethodCall(method),
                UnaryExpression unary when unary.NodeType == ExpressionType.Not => 
                    $"!({ExpressionToString(unary.Operand)})",
                _ => expression.ToString()
            };
        }

        private string HandleBinaryExpression(BinaryExpression binary)
        {
            var left = ExpressionToString(binary.Left);
            var right = ExpressionToString(binary.Right);
            var op = GetOperator(binary.NodeType);
            return $"({left} {op} {right})";
        }

        private string HandleMethodCall(MethodCallExpression method)
        {
            if (method.Method.Name == "Contains" && method.Object != null)
            {
                var obj = ExpressionToString(method.Object);
                var arg = ExpressionToString(method.Arguments[0]);
                return $"{obj}.Contains({arg})";
            }
            
            if (method.Method.Name == "Any" && method.Arguments.Count > 0)
            {
                var source = GetPropertyPath(method.Arguments[0]);
                if (method.Arguments.Count > 1)
                {
                    var lambda = method.Arguments[1] as LambdaExpression;
                    if (lambda != null)
                    {
                        var paramName = lambda.Parameters[0].Name;
                        var bodyStr = ExpressionToString(lambda.Body);
                        return $"{source}.Any({paramName} => {bodyStr})";
                    }
                }
                return $"{source}.Any()";
            }

            return method.ToString();
        }

        protected string GetPropertyPath(Expression expression)
        {
            return expression switch
            {
                MemberExpression member when member.Expression is MemberExpression parent =>
                    $"{GetPropertyPath(parent)}.{member.Member.Name}",
                MemberExpression member when member.Expression is ParameterExpression =>
                    member.Member.Name,
                ParameterExpression param => param.Name ?? "x",
                _ => expression.ToString()
            };
        }

        private string GetOperator(ExpressionType type)
        {
            return type switch
            {
                ExpressionType.Equal => "==",
                ExpressionType.NotEqual => "!=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.AndAlso => "&&",
                ExpressionType.OrElse => "||",
                _ => type.ToString()
            };
        }

        private string FormatConstant(object? value)
        {
            return value switch
            {
                string str => $"\"{str}\"",
                DateTime date => $"DateTime.Parse(\"{date:yyyy-MM-dd HH:mm:ss}\")",
                bool boolean => boolean.ToString().ToLower(),
                null => "null",
                _ => value.ToString() ?? "null"
            };
        }

        #region Search Operations

        private async Task<List<T>> ExecuteSearchAsync(bool autoInclude)
        {
            var searchRequest = BuildSearchRequest(autoInclude);

            var response = await _http.PostAsJsonAsync($"/api/query/{_entityName}/search", searchRequest);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Search failed: {response.StatusCode} - {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<List<T>>();
            return result ?? new List<T>();
        }

        private async Task<PagedResult<T>> ExecuteSearchPagedAsync(bool autoInclude)
        {
            var searchRequest = BuildSearchRequest(autoInclude);

            var response = await _http.PostAsJsonAsync($"/api/query/{_entityName}/search-paged", searchRequest);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PagedResult<T>>();
            return result ?? new PagedResult<T>();
        }

        private SearchRequest BuildSearchRequest(bool autoInclude)
        {
            // Crear BaseQuery con los filtros y configuración actual
            var baseQuery = new QueryRequest
            {
                Filter = BuildFilterString(),
                OrderBy = BuildOrderByString(),
                Include = autoInclude ? null : _includeExpressions.ToArray(),
                Skip = _skip,
                Take = _take
            };

            // Crear SearchRequest con el BaseQuery
            var searchRequest = new SearchRequest
            {
                SearchTerm = _searchTerm ?? string.Empty,
                SearchFields = _searchFields.Any() ? _searchFields.Select(GetPropertyName).ToArray() : Array.Empty<string>(),
                BaseQuery = baseQuery
            };

            return searchRequest;
        }

        private string GetPropertyName(Expression<Func<T, object>> expression)
        {
            return expression switch
            {
                { Body: MemberExpression member } => member.Member.Name,
                { Body: UnaryExpression { Operand: MemberExpression member } } => member.Member.Name,
                _ => throw new ArgumentException($"Expression '{expression}' is not a valid property expression.")
            };
        }

        private Expression<Func<T, object>> ConvertToObjectExpression<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            return Expression.Lambda<Func<T, object>>(
                Expression.Convert(expression.Body, typeof(object)),
                expression.Parameters);
        }

        #endregion
    }
}