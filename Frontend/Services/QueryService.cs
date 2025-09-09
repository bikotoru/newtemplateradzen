using System.Linq.Expressions;
using Shared.Models.QueryModels;
using Shared.Models.Responses;

namespace Frontend.Services
{
    public class QueryService
    {
        private readonly API _api;

        public QueryService(API api)
        {
            _api = api;
        }

        public QueryBuilder<T> For<T>() where T : class
        {
            return new QueryBuilder<T>(_api, typeof(T).Name);
        }
        
        public QueryBuilder<T> For<T>(string baseUrl) where T : class
        {
            return new QueryBuilder<T>(_api, typeof(T).Name, baseUrl);
        }
    }

    public class QueryBuilder<T> where T : class
    {
        protected readonly API _api;
        protected readonly string _entityName;
        public string? _baseUrl;
        protected readonly List<Expression<Func<T, bool>>> _filters = new();
        protected LambdaExpression? _orderByExpression;
        protected bool _orderByDescending = false;
        protected readonly List<string> _includeExpressions = new();
        protected int? _skip;
        protected int? _take;
        
        // Search properties
        protected string? _searchTerm;
        protected readonly List<Expression<Func<T, object>>> _searchFields = new();

        public QueryBuilder(API api, string entityName)
        {
            _api = api;
            _entityName = entityName;
            _baseUrl = null;
        }
        
        public QueryBuilder(API api, string entityName, string baseUrl)
        {
            _api = api;
            _entityName = entityName;
            _baseUrl = baseUrl?.TrimEnd('/');
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
            return new SelectQueryBuilder<T, TResult>(_api, _entityName, _baseUrl, _filters, _orderByExpression, 
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

        /// <summary>
        /// Combina este QueryBuilder con otro usando operador AND (crea nuevo QueryBuilder)
        /// </summary>
        public QueryBuilder<T> And(QueryBuilder<T> other)
        {
            var newQuery = new QueryBuilder<T>(_api, _entityName);
            
            // Copiar filtros de ambos QueryBuilders
            newQuery._filters.AddRange(_filters);
            newQuery._filters.AddRange(other._filters);
            
            // Copiar otras propiedades del QueryBuilder actual
            newQuery._orderByExpression = _orderByExpression;
            newQuery._orderByDescending = _orderByDescending;
            newQuery._includeExpressions.AddRange(_includeExpressions);
            newQuery._skip = _skip;
            newQuery._take = _take;
            newQuery._searchTerm = _searchTerm;
            newQuery._searchFields.AddRange(_searchFields);
            
            return newQuery;
        }

        /// <summary>
        /// Combina este QueryBuilder con otro usando operador OR (crea nuevo QueryBuilder)
        /// </summary>
        public QueryBuilder<T> Or(QueryBuilder<T> other)
        {
            var newQuery = new QueryBuilder<T>(_api, _entityName);
            
            // Crear una expresión OR que combine los filtros de ambos QueryBuilders
            if (_filters.Any() && other._filters.Any())
            {
                // Combinar todos los filtros del primer QueryBuilder con AND
                Expression<Func<T, bool>>? leftExpression = null;
                foreach (var filter in _filters)
                {
                    leftExpression = leftExpression == null ? filter : CombineWithAnd(leftExpression, filter);
                }
                
                // Combinar todos los filtros del segundo QueryBuilder con AND
                Expression<Func<T, bool>>? rightExpression = null;
                foreach (var filter in other._filters)
                {
                    rightExpression = rightExpression == null ? filter : CombineWithAnd(rightExpression, filter);
                }
                
                // Combinar ambas expresiones con OR
                if (leftExpression != null && rightExpression != null)
                {
                    var orExpression = CombineWithOr(leftExpression, rightExpression);
                    newQuery._filters.Add(orExpression);
                }
            }
            else if (_filters.Any())
            {
                newQuery._filters.AddRange(_filters);
            }
            else if (other._filters.Any())
            {
                newQuery._filters.AddRange(other._filters);
            }
            
            // Copiar otras propiedades del QueryBuilder actual
            newQuery._orderByExpression = _orderByExpression;
            newQuery._orderByDescending = _orderByDescending;
            newQuery._includeExpressions.AddRange(_includeExpressions);
            newQuery._skip = _skip;
            newQuery._take = _take;
            newQuery._searchTerm = _searchTerm;
            newQuery._searchFields.AddRange(_searchFields);
            
            return newQuery;
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

            // ✅ SOLUCIONADO: Usar _baseUrl del service si está disponible
            var endpoint = !string.IsNullOrEmpty(_baseUrl) ? $"{_baseUrl}/query" : $"/api/{_entityName}/query";
            var response = await _api.PostAsync<List<T>>(endpoint, request);
            if (!response.Success)
            {
                throw new HttpRequestException($"Query failed: {response.Message}");
            }
            
            return response.Data ?? new List<T>();
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

            // ✅ SOLUCIONADO: Usar _baseUrl del service si está disponible, sino fallback al patrón antiguo
            var endpoint = !string.IsNullOrEmpty(_baseUrl) ? $"{_baseUrl}/paged" : $"/api/{_entityName}/paged";
            var response = await _api.PostAsync<PagedResult<T>>(endpoint, request);
            if (!response.Success)
            {
                throw new HttpRequestException($"Paged query failed: {response.Message}");
            }
            
            return response.Data ?? new PagedResult<T>();
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
                var obj = GetPropertyPath(method.Object);
                var arg = ExpressionToString(method.Arguments[0]);
                return $"{obj}.Contains({arg})";
            }
            
            if (method.Method.Name == "StartsWith" && method.Object != null)
            {
                var obj = GetPropertyPath(method.Object);
                var arg = ExpressionToString(method.Arguments[0]);
                return $"{obj}.StartsWith({arg})";
            }
            
            if (method.Method.Name == "EndsWith" && method.Object != null)
            {
                var obj = GetPropertyPath(method.Object);
                var arg = ExpressionToString(method.Arguments[0]);
                return $"{obj}.EndsWith({arg})";
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

            // ✅ SOLUCIONADO: Usar _baseUrl del service si está disponible
            var endpoint = !string.IsNullOrEmpty(_baseUrl) ? $"{_baseUrl}/search" : $"/api/{_entityName}/search";
            var response = await _api.PostAsync<List<T>>(endpoint, searchRequest);
            if (!response.Success)
            {
                throw new HttpRequestException($"Search failed: {response.Message}");
            }

            return response.Data ?? new List<T>();
        }

        private async Task<PagedResult<T>> ExecuteSearchPagedAsync(bool autoInclude)
        {
            var searchRequest = BuildSearchRequest(autoInclude);

            // ✅ SOLUCIONADO: Usar _baseUrl del service si está disponible
            var endpoint = !string.IsNullOrEmpty(_baseUrl) ? $"{_baseUrl}/search-paged" : $"/api/{_entityName}/search-paged";
            var response = await _api.PostAsync<PagedResult<T>>(endpoint, searchRequest);
            if (!response.Success)
            {
                throw new HttpRequestException($"Paged search failed: {response.Message}");
            }

            return response.Data ?? new PagedResult<T>();
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

        /// <summary>
        /// Combina dos expresiones con operador AND
        /// </summary>
        private Expression<Func<T, bool>> CombineWithAnd(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            
            var leftBody = ReplaceParameter(left.Body, left.Parameters[0], parameter);
            var rightBody = ReplaceParameter(right.Body, right.Parameters[0], parameter);
            
            var andExpression = Expression.AndAlso(leftBody, rightBody);
            return Expression.Lambda<Func<T, bool>>(andExpression, parameter);
        }

        /// <summary>
        /// Combina dos expresiones con operador OR
        /// </summary>
        private Expression<Func<T, bool>> CombineWithOr(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            
            var leftBody = ReplaceParameter(left.Body, left.Parameters[0], parameter);
            var rightBody = ReplaceParameter(right.Body, right.Parameters[0], parameter);
            
            var orExpression = Expression.OrElse(leftBody, rightBody);
            return Expression.Lambda<Func<T, bool>>(orExpression, parameter);
        }

        /// <summary>
        /// Reemplaza un parámetro en una expresión
        /// </summary>
        private Expression ReplaceParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
        }

        /// <summary>
        /// Visitor para reemplazar parámetros en expresiones
        /// </summary>
        private class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParameter;
            private readonly ParameterExpression _newParameter;

            public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _oldParameter ? _newParameter : node;
            }
        }

        #endregion
    }
}