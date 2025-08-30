using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Text.Json;
using Shared.Models.QueryModels;

namespace Shared.Models.Services
{
    public class SelectQueryBuilder<T, TResult> where T : class
    {
        private readonly HttpClient _http;
        private readonly string _entityName;
        private readonly List<Expression<Func<T, bool>>> _filters;
        private readonly LambdaExpression? _orderByExpression;
        private readonly bool _orderByDescending;
        private readonly Expression<Func<T, TResult>> _selectExpression;
        private readonly List<string> _includeExpressions;
        private int? _skip;
        private int? _take;

        internal SelectQueryBuilder(
            HttpClient http,
            string entityName,
            List<Expression<Func<T, bool>>> filters,
            LambdaExpression? orderByExpression,
            bool orderByDescending,
            Expression<Func<T, TResult>> selectExpression,
            List<string> includeExpressions,
            int? skip,
            int? take)
        {
            _http = http;
            _entityName = entityName;
            _filters = filters;
            _orderByExpression = orderByExpression;
            _orderByDescending = orderByDescending;
            _selectExpression = selectExpression;
            _includeExpressions = includeExpressions;
            _skip = skip;
            _take = take;
        }

        public SelectQueryBuilder<T, TResult> Where(Expression<Func<T, bool>> predicate)
        {
            _filters.Add(predicate);
            return this;
        }

        public SelectQueryBuilder<T, TResult> OrderBy<TProperty>(Expression<Func<T, TProperty>> property, bool descending = false)
        {
            return new SelectQueryBuilder<T, TResult>(_http, _entityName, _filters, property, 
                descending, _selectExpression, _includeExpressions, _skip, _take);
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
            var request = new QueryRequest
            {
                Filter = BuildFilterString(),
                OrderBy = BuildOrderByString(),
                Select = BuildSelectString(),
                Include = _includeExpressions.ToArray(),
                Skip = _skip,
                Take = _take
            };

            var response = await _http.PostAsJsonAsync($"/api/query/{_entityName}/select", request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Select query failed: {response.StatusCode} - {errorContent}");
            }
            
            var result = await response.Content.ReadFromJsonAsync<List<TResult>>();
            return result ?? new List<TResult>();
        }

        public async Task<PagedResult<TResult>> ToPagedResultAsync()
        {
            var request = new QueryRequest
            {
                Filter = BuildFilterString(),
                OrderBy = BuildOrderByString(),
                Select = BuildSelectString(),
                Include = _includeExpressions.ToArray(),
                Skip = _skip,
                Take = _take
            };

            var response = await _http.PostAsJsonAsync($"/api/query/{_entityName}/select-paged", request);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<PagedResult<TResult>>();
            return result ?? new PagedResult<TResult>();
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
    }
}