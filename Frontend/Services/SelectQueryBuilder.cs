using System.Linq.Expressions;
using Shared.Models.QueryModels;
using Shared.Models.Responses;

namespace Frontend.Services
{
    public class SelectQueryBuilder<T, TResult> where T : class
    {
        private readonly API _api;
        private readonly string _entityName;
        private readonly string? _baseUrl;
        private readonly BackendType? _backendType;
        private readonly List<Expression<Func<T, bool>>> _filters;
        private readonly LambdaExpression? _orderByExpression;
        private readonly bool _orderByDescending;
        private readonly Expression<Func<T, TResult>> _selectExpression;
        private readonly List<string> _includeExpressions;
        private int? _skip;
        private int? _take;

        // Search properties
        private string? _searchTerm;
        private readonly List<Expression<Func<T, object>>> _searchFields = new();

        internal SelectQueryBuilder(
            API api,
            string entityName,
            string? baseUrl,
            List<Expression<Func<T, bool>>> filters,
            LambdaExpression? orderByExpression,
            bool orderByDescending,
            Expression<Func<T, TResult>> selectExpression,
            List<string> includeExpressions,
            int? skip,
            int? take,
            string? searchTerm = null,
            List<Expression<Func<T, object>>>? searchFields = null,
            BackendType? backendType = null)
        {
            _api = api;
            _entityName = entityName;
            _baseUrl = baseUrl?.TrimEnd('/');
            _backendType = backendType;
            _filters = filters;
            _orderByExpression = orderByExpression;
            _orderByDescending = orderByDescending;
            _selectExpression = selectExpression;
            _includeExpressions = includeExpressions;
            _skip = skip;
            _take = take;
            _searchTerm = searchTerm;
            if (searchFields != null)
                _searchFields.AddRange(searchFields);
        }

        public SelectQueryBuilder<T, TResult> Where(Expression<Func<T, bool>> predicate)
        {
            _filters.Add(predicate);
            return this;
        }

        public SelectQueryBuilder<T, TResult> OrderBy<TProperty>(Expression<Func<T, TProperty>> property, bool descending = false)
        {
            return new SelectQueryBuilder<T, TResult>(_api, _entityName, _baseUrl, _filters, property,
                descending, _selectExpression, _includeExpressions, _skip, _take, _searchTerm, _searchFields, _backendType);
        }

        /// <summary>
        /// Búsqueda inteligente por término (funciona con strings, números, fechas)
        /// </summary>
        public SelectQueryBuilder<T, TResult> Search(string searchTerm)
        {
            return new SelectQueryBuilder<T, TResult>(_api, _entityName, _baseUrl, _filters, _orderByExpression,
                _orderByDescending, _selectExpression, _includeExpressions, _skip, _take, searchTerm, _searchFields, _backendType);
        }

        /// <summary>
        /// Especificar campos donde buscar (fuertemente tipado)
        /// </summary>
        public SelectQueryBuilder<T, TResult> InFields(params Expression<Func<T, object>>[] searchFields)
        {
            var newSearchFields = new List<Expression<Func<T, object>>>(searchFields);
            return new SelectQueryBuilder<T, TResult>(_api, _entityName, _baseUrl, _filters, _orderByExpression,
                _orderByDescending, _selectExpression, _includeExpressions, _skip, _take, _searchTerm, newSearchFields, _backendType);
        }

        public SelectQueryBuilder<T, TResult> Include<TProperty>(Expression<Func<T, TProperty>> navigationProperty)
        {
            var propertyPath = GetPropertyPath(navigationProperty.Body);
            _includeExpressions.Add(propertyPath);
            return this;
        }

        public SelectQueryBuilder<T, TResult> Skip(int count)
        {
            _skip = count;
            return this;
        }

        public SelectQueryBuilder<T, TResult> Take(int count)
        {
            _take = count;
            return this;
        }

        public async Task<List<TResult>> ToListAsync()
        {
            // Si hay búsqueda, usar endpoint de Search con Select
            if (!string.IsNullOrWhiteSpace(_searchTerm))
            {
                return await ExecuteSearchSelectAsync();
            }

            // Usar endpoint de Select normal
            var request = new QueryRequest
            {
                Filter = BuildFilterString(),
                OrderBy = BuildOrderByString(),
                Select = BuildSelectString(),
                Include = _includeExpressions.ToArray(),
                Skip = _skip,
                Take = _take
            };

            // ✅ SOLUCIONADO: Usar _baseUrl del service si está disponible
            var endpoint = !string.IsNullOrEmpty(_baseUrl) ? $"{_baseUrl}/select" : $"/api/{_entityName}/select";
            var response = _backendType.HasValue
                ? await _api.PostAsync<List<TResult>>(endpoint, request, _backendType.Value)
                : await _api.PostAsync<List<TResult>>(endpoint, request);
            if (!response.Success)
            {
                throw new HttpRequestException($"Select query failed: {response.Message}");
            }
            
            return response.Data ?? new List<TResult>();
        }

        public async Task<PagedResult<TResult>> ToPagedResultAsync()
        {
            // Si hay búsqueda, usar endpoint de Search con Select paginado
            if (!string.IsNullOrWhiteSpace(_searchTerm))
            {
                return await ExecuteSearchSelectPagedAsync();
            }

            // Usar endpoint de Select paginado normal
            var request = new QueryRequest
            {
                Filter = BuildFilterString(),
                OrderBy = BuildOrderByString(),
                Select = BuildSelectString(),
                Include = _includeExpressions.ToArray(),
                Skip = _skip,
                Take = _take
            };

            // ✅ SOLUCIONADO: Usar _baseUrl del service si está disponible
            var endpoint = !string.IsNullOrEmpty(_baseUrl) ? $"{_baseUrl}/select-paged" : $"/api/{_entityName}/select-paged";
            var response = _backendType.HasValue
                ? await _api.PostAsync<PagedResult<TResult>>(endpoint, request, _backendType.Value)
                : await _api.PostAsync<PagedResult<TResult>>(endpoint, request);
            if (!response.Success)
            {
                throw new HttpRequestException($"Paged select query failed: {response.Message}");
            }
            
            return response.Data ?? new PagedResult<TResult>();
        }

        private string? BuildFilterString()
        {
            if (!_filters.Any()) return null;
            
            var filterStrings = _filters.Select(expr => ExpressionToString(expr.Body)).ToList();
            return string.Join(" && ", filterStrings);
        }

        private string? BuildOrderByString()
        {
            if (_orderByExpression == null) return null;
            
            var propertyPath = GetPropertyPath(_orderByExpression.Body);
            return _orderByDescending ? $"{propertyPath} desc" : propertyPath;
        }

        private string BuildSelectString()
        {
            return BuildSelectExpression(_selectExpression.Body);
        }

        private string BuildSelectExpression(Expression expression)
        {
            return expression switch
            {
                NewExpression newExpr => BuildNewExpression(newExpr),
                MemberInitExpression memberInit => BuildMemberInitExpression(memberInit),
                MemberExpression member => GetPropertyPath(member),
                _ => "new { " + GetPropertyPath(expression) + " }"
            };
        }

        private string BuildNewExpression(NewExpression newExpr)
        {
            if (newExpr.Members == null || !newExpr.Members.Any())
            {
                var args = newExpr.Arguments.Select(arg => GetPropertyPath(arg));
                return "new { " + string.Join(", ", args) + " }";
            }

            var properties = newExpr.Members
                .Zip(newExpr.Arguments, (member, arg) => $"{member.Name} = {GetPropertyPath(arg)}")
                .ToList();

            return "new { " + string.Join(", ", properties) + " }";
        }

        private string BuildMemberInitExpression(MemberInitExpression memberInit)
        {
            var bindings = memberInit.Bindings.OfType<MemberAssignment>()
                .Select(binding => $"{binding.Member.Name} = {GetPropertyPath(binding.Expression)}")
                .ToList();

            var typeName = memberInit.NewExpression.Type.Name;
            return $"new {typeName} {{ {string.Join(", ", bindings)} }}";
        }

        private string ExpressionToString(Expression expression)
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

        private string GetPropertyPath(Expression expression)
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

        private async Task<List<TResult>> ExecuteSearchSelectAsync()
        {
            var searchRequest = BuildSearchRequest();

            // ✅ SOLUCIONADO: Usar _baseUrl del service si está disponible
            var endpoint = !string.IsNullOrEmpty(_baseUrl) ? $"{_baseUrl}/search-select" : $"/api/{_entityName}/search-select";
            var response = _backendType.HasValue
                ? await _api.PostAsync<List<TResult>>(endpoint, searchRequest, _backendType.Value)
                : await _api.PostAsync<List<TResult>>(endpoint, searchRequest);
            if (!response.Success)
            {
                throw new HttpRequestException($"Search select failed: {response.Message}");
            }

            return response.Data ?? new List<TResult>();
        }

        private async Task<PagedResult<TResult>> ExecuteSearchSelectPagedAsync()
        {
            var searchRequest = BuildSearchRequest();

            // ✅ SOLUCIONADO: Usar _baseUrl del service si está disponible
            var endpoint = !string.IsNullOrEmpty(_baseUrl) ? $"{_baseUrl}/search-select-paged" : $"/api/{_entityName}/search-select-paged";
            var response = _backendType.HasValue
                ? await _api.PostAsync<PagedResult<TResult>>(endpoint, searchRequest, _backendType.Value)
                : await _api.PostAsync<PagedResult<TResult>>(endpoint, searchRequest);
            if (!response.Success)
            {
                throw new HttpRequestException($"Paged search select failed: {response.Message}");
            }

            return response.Data ?? new PagedResult<TResult>();
        }

        private SearchRequest BuildSearchRequest()
        {
            // Crear BaseQuery con los filtros y configuración actual, incluyendo Select
            var baseQuery = new QueryRequest
            {
                Filter = BuildFilterString(),
                OrderBy = BuildOrderByString(),
                Select = BuildSelectString(),
                Include = _includeExpressions.ToArray(),
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

        #endregion
    }
}