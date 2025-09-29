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
                // For "Related" operator, we need to filter based on navigation property
                // E.g., if filter.Property is "RegionId", we filter by "Region.SomeProperty"
                if (string.IsNullOrEmpty(filter.Property) || !filter.Property.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                    return null;

                var navigationPropertyName = filter.Property.Substring(0, filter.Property.Length - 2);
                var navigationProperty = typeof(T).GetProperty(navigationPropertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (navigationProperty == null)
                    return null;

                // Build expression for related entity property
                // Example: Comuna.Region.Nombre contains "Santiago"
                var navigationExpression = Expression.PropertyOrField(parameter, navigationPropertyName);

                // For now, we'll filter by the navigation property not being null (basic "Related" functionality)
                // This can be extended to filter by specific properties of the related entity
                return Expression.NotEqual(navigationExpression, Expression.Constant(null, navigationProperty.PropertyType));
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