using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Frontend.Components.CustomRadzen.QueryBuilder.Models;
using CompositeFilterDescriptor = Frontend.Components.CustomRadzen.QueryBuilder.Models.CompositeFilterDescriptor;
using FilterCaseSensitivity = Frontend.Components.CustomRadzen.QueryBuilder.Models.FilterCaseSensitivity;
using FilterOperator = Frontend.Components.CustomRadzen.QueryBuilder.Models.FilterOperator;
using LogicalFilterOperator = Frontend.Components.CustomRadzen.QueryBuilder.Models.LogicalFilterOperator;

namespace Frontend.Components.CustomRadzen.QueryBuilder.Extensions
{
    /// <summary>
    /// QueryableExtension for CustomDataFilter
    /// </summary>
    public static class CustomQueryableExtensions
    {
        /// <summary>
        /// Filters using the specified CustomDataFilter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="dataFilter">The DataFilter.</param>
        /// <returns>IQueryable&lt;T&gt;.</returns>
        public static IQueryable<T> Where<T>(this IQueryable<T> source, CustomDataFilter<T> dataFilter)
        {
            Func<CompositeFilterDescriptor, bool> canFilter = (c) => dataFilter.properties.Where(col => col.Property == c.Property).FirstOrDefault()?.FilterPropertyType != null &&
               (!(c.FilterValue == null || c.FilterValue as string == string.Empty)
                || c.FilterOperator == FilterOperator.IsNotNull || c.FilterOperator == FilterOperator.IsNull
                || c.FilterOperator == FilterOperator.IsEmpty || c.FilterOperator == FilterOperator.IsNotEmpty)
               && c.Property != null;

            if (dataFilter.Filters.Concat(dataFilter.Filters.SelectManyRecursive(i => i.Filters ?? Enumerable.Empty<CompositeFilterDescriptor>())).Where(canFilter).Any())
            {
                var filterExpressions = new List<Expression>();

                var parameter = Expression.Parameter(typeof(T), "x");

                foreach (var filter in dataFilter.Filters)
                {
                    AddWhereExpression<T>(parameter, filter, ref filterExpressions, dataFilter.FilterCaseSensitivity);
                }

                Expression combinedExpression = null;

                foreach (var expression in filterExpressions)
                {
                    combinedExpression = combinedExpression == null
                        ? expression
                        : dataFilter.LogicalFilterOperator == LogicalFilterOperator.And ?
                            Expression.AndAlso(combinedExpression, expression) :
                                Expression.OrElse(combinedExpression, expression);
                }

                if (combinedExpression != null)
                {
                    var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
                    return source.Where(lambda);
                }
            }

            return source;
        }

        private static void AddWhereExpression<T>(ParameterExpression parameter, CompositeFilterDescriptor filter, ref List<Expression> filterExpressions, FilterCaseSensitivity filterCaseSensitivity)
        {
            if (filter.Filters != null)
            {
                var innerFilterExpressions = new List<Expression>();

                foreach (var f in filter.Filters)
                {
                    AddWhereExpression<T>(parameter, f, ref innerFilterExpressions, filterCaseSensitivity);
                }

                if (innerFilterExpressions.Any())
                {
                    Expression combinedExpression = null;

                    foreach (var expression in innerFilterExpressions)
                    {
                        combinedExpression = combinedExpression == null
                            ? expression
                            : filter.LogicalFilterOperator == LogicalFilterOperator.And ?
                                Expression.AndAlso(combinedExpression, expression) :
                                    Expression.OrElse(combinedExpression, expression);
                    }

                    if (combinedExpression != null)
                    {
                        filterExpressions.Add(combinedExpression);
                    }
                }
            }
            else
            {
                if (filter.Property == null || filter.FilterOperator == null || (filter.FilterValue == null &&
                    filter.FilterOperator != FilterOperator.IsNull && filter.FilterOperator != FilterOperator.IsNotNull &&
                    filter.FilterOperator != FilterOperator.IsEmpty && filter.FilterOperator != FilterOperator.IsNotEmpty))
                {
                    return;
                }

                var expression = GetExpression<T>(parameter, filter, filterCaseSensitivity);
                if (expression != null)
                {
                    filterExpressions.Add(expression);
                }
            }
        }

        private static Expression GetExpression<T>(ParameterExpression parameter, CompositeFilterDescriptor filter, FilterCaseSensitivity filterCaseSensitivity)
        {
            var property = GetNestedPropertyExpression(parameter, filter.Property);
            var caseInsensitive = property.Type == typeof(string) && filterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive;

            var constant = Expression.Constant(caseInsensitive ?
                $"{filter.FilterValue}".ToLowerInvariant() : filter.FilterValue, property.Type);

            if (caseInsensitive)
            {
                property = Expression.Call(NotNullCheck(property), typeof(string).GetMethod("ToLower", System.Type.EmptyTypes));
            }

            return filter.FilterOperator switch
            {
                FilterOperator.Equals => Expression.Equal(NotNullCheck(property), constant),
                FilterOperator.NotEquals => Expression.NotEqual(NotNullCheck(property), constant),
                FilterOperator.LessThan => Expression.LessThan(NotNullCheck(property), constant),
                FilterOperator.LessThanOrEquals => Expression.LessThanOrEqual(NotNullCheck(property), constant),
                FilterOperator.GreaterThan => Expression.GreaterThan(NotNullCheck(property), constant),
                FilterOperator.GreaterThanOrEquals => Expression.GreaterThanOrEqual(NotNullCheck(property), constant),
                FilterOperator.Contains => Expression.Call(NotNullCheck(property), typeof(string).GetMethod("Contains", new[] { typeof(string) }), constant),
                FilterOperator.DoesNotContain => Expression.Not(Expression.Call(NotNullCheck(property), typeof(string).GetMethod("Contains", new[] { typeof(string) }), constant)),
                FilterOperator.StartsWith => Expression.Call(NotNullCheck(property), typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), constant),
                FilterOperator.EndsWith => Expression.Call(NotNullCheck(property), typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), constant),
                FilterOperator.IsNull => Expression.Equal(property, Expression.Constant(null, property.Type)),
                FilterOperator.IsNotNull => Expression.NotEqual(property, Expression.Constant(null, property.Type)),
                FilterOperator.IsEmpty => Expression.Equal(property, Expression.Constant(String.Empty)),
                FilterOperator.IsNotEmpty => Expression.NotEqual(property, Expression.Constant(String.Empty)),
                FilterOperator.Related => HandleRelatedFilter<T>(parameter, filter),
                _ => null
            };
        }

        private static Expression HandleRelatedFilter<T>(ParameterExpression parameter, CompositeFilterDescriptor filter)
        {
            try
            {
                // Verificar que es un filtro de relación válido
                if (string.IsNullOrEmpty(filter.Property) || !filter.Property.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                    return null;

                var navigationPropertyName = filter.Property.Substring(0, filter.Property.Length - 2);
                var navigationProperty = typeof(T).GetProperty(navigationPropertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (navigationProperty == null)
                    return null;

                var navigationExpression = Expression.PropertyOrField(parameter, navigationPropertyName);

                // 🆕 Si hay filtros anidados, construir expresión compleja
                if (filter.NestedFilters?.Any() == true)
                {
                    return BuildNestedFilterExpression<T>(parameter, filter, navigationExpression, navigationProperty.PropertyType);
                }

                // Si no hay filtros anidados, solo verificar que la relación no sea null
                return Expression.NotEqual(navigationExpression, Expression.Constant(null, navigationProperty.PropertyType));
            }
            catch (Exception ex)
            {
                // Log para debugging
                Console.WriteLine($"Error in HandleRelatedFilter: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Construye una expresión de filtro anidada para entidades relacionadas
        /// </summary>
        private static Expression BuildNestedFilterExpression<T>(
            ParameterExpression rootParameter,
            CompositeFilterDescriptor filter,
            Expression navigationExpression,
            Type relatedEntityType)
        {
            try
            {
                var nestedExpressions = new List<Expression>();

                // Agregar verificación de que la navegación no sea null
                var notNullExpression = Expression.NotEqual(navigationExpression, Expression.Constant(null, relatedEntityType));
                nestedExpressions.Add(notNullExpression);

                // Procesar cada filtro anidado
                foreach (var nestedFilter in filter.NestedFilters)
                {
                    var nestedExpression = BuildSingleNestedExpression(navigationExpression, nestedFilter, relatedEntityType);
                    if (nestedExpression != null)
                    {
                        nestedExpressions.Add(nestedExpression);
                    }
                }

                if (!nestedExpressions.Any())
                    return notNullExpression;

                // Combinar todas las expresiones con AND
                Expression combinedExpression = nestedExpressions.First();
                foreach (var expr in nestedExpressions.Skip(1))
                {
                    combinedExpression = Expression.AndAlso(combinedExpression, expr);
                }

                return combinedExpression;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BuildNestedFilterExpression: {ex.Message}");
                return Expression.NotEqual(navigationExpression, Expression.Constant(null, relatedEntityType));
            }
        }

        /// <summary>
        /// Construye una expresión para un solo filtro anidado
        /// </summary>
        private static Expression BuildSingleNestedExpression(
            Expression navigationExpression,
            CompositeFilterDescriptor nestedFilter,
            Type relatedEntityType)
        {
            try
            {
                if (string.IsNullOrEmpty(nestedFilter.Property) || nestedFilter.FilterOperator == null)
                    return null;

                // Obtener la propiedad de la entidad relacionada
                var nestedProperty = relatedEntityType.GetProperty(nestedFilter.Property, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (nestedProperty == null)
                    return null;

                // Construir expresión: navigationExpression.Property
                var nestedPropertyExpression = Expression.PropertyOrField(navigationExpression, nestedFilter.Property);

                // 🔄 Recursivo: Si este filtro anidado también es Related, procesar recursivamente
                if (nestedFilter.FilterOperator == FilterOperator.Related && nestedFilter.NestedFilters?.Any() == true)
                {
                    return BuildNestedFilterExpression<object>(null, nestedFilter, nestedPropertyExpression, nestedProperty.PropertyType);
                }

                // Crear constante para el valor del filtro
                var filterValue = nestedFilter.FilterValue;
                var constant = Expression.Constant(filterValue, nestedProperty.PropertyType);

                // Construir expresión según el operador
                return nestedFilter.FilterOperator switch
                {
                    FilterOperator.Equals => Expression.Equal(NotNullCheck(nestedPropertyExpression), constant),
                    FilterOperator.NotEquals => Expression.NotEqual(NotNullCheck(nestedPropertyExpression), constant),
                    FilterOperator.LessThan => Expression.LessThan(NotNullCheck(nestedPropertyExpression), constant),
                    FilterOperator.LessThanOrEquals => Expression.LessThanOrEqual(NotNullCheck(nestedPropertyExpression), constant),
                    FilterOperator.GreaterThan => Expression.GreaterThan(NotNullCheck(nestedPropertyExpression), constant),
                    FilterOperator.GreaterThanOrEquals => Expression.GreaterThanOrEqual(NotNullCheck(nestedPropertyExpression), constant),
                    FilterOperator.Contains => BuildStringMethodExpression(nestedPropertyExpression, "Contains", filterValue),
                    FilterOperator.DoesNotContain => Expression.Not(BuildStringMethodExpression(nestedPropertyExpression, "Contains", filterValue)),
                    FilterOperator.StartsWith => BuildStringMethodExpression(nestedPropertyExpression, "StartsWith", filterValue),
                    FilterOperator.EndsWith => BuildStringMethodExpression(nestedPropertyExpression, "EndsWith", filterValue),
                    FilterOperator.IsNull => Expression.Equal(nestedPropertyExpression, Expression.Constant(null, nestedProperty.PropertyType)),
                    FilterOperator.IsNotNull => Expression.NotEqual(nestedPropertyExpression, Expression.Constant(null, nestedProperty.PropertyType)),
                    FilterOperator.IsEmpty => Expression.Equal(nestedPropertyExpression, Expression.Constant(string.Empty)),
                    FilterOperator.IsNotEmpty => Expression.NotEqual(nestedPropertyExpression, Expression.Constant(string.Empty)),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BuildSingleNestedExpression: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Construye expresiones para métodos de string (Contains, StartsWith, EndsWith)
        /// </summary>
        private static Expression BuildStringMethodExpression(Expression propertyExpression, string methodName, object filterValue)
        {
            try
            {
                if (propertyExpression.Type != typeof(string))
                    return null;

                var method = typeof(string).GetMethod(methodName, new[] { typeof(string) });
                if (method == null)
                    return null;

                var valueExpression = Expression.Constant(filterValue?.ToString() ?? string.Empty, typeof(string));
                return Expression.Call(NotNullCheck(propertyExpression), method, valueExpression);
            }
            catch
            {
                return null;
            }
        }

        private static Expression NotNullCheck(Expression property) =>
            Nullable.GetUnderlyingType(property.Type) != null || property.Type == typeof(string) ?
                Expression.Coalesce(property, property.Type == typeof(string) ? Expression.Constant(string.Empty) : Expression.Constant(null, property.Type)) : property;

        private static Expression GetNestedPropertyExpression(Expression expression, string property)
        {
            var parts = property.Split('.');
            Expression current = expression;

            foreach (var part in parts)
            {
                current = Expression.PropertyOrField(current, part);
            }

            return current;
        }

        /// <summary>
        /// Selects the many recursive.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="selector">The selector.</param>
        /// <returns>IEnumerable&lt;T&gt;.</returns>
        public static IEnumerable<T> SelectManyRecursive<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> selector)
        {
            var result = source.SelectMany(selector);
            if (!result.Any())
            {
                return result;
            }
            return result.Concat(result.SelectManyRecursive(selector));
        }
    }
}