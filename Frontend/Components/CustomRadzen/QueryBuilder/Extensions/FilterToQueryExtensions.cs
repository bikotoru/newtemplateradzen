using System;
using System.Collections.Generic;
using System.Linq;
using Frontend.Components.CustomRadzen.QueryBuilder.Models;
using Shared.Models.QueryModels;
using CompositeFilterDescriptor = Frontend.Components.CustomRadzen.QueryBuilder.Models.CompositeFilterDescriptor;
using LogicalFilterOperator = Frontend.Components.CustomRadzen.QueryBuilder.Models.LogicalFilterOperator;
using FilterOperator = Frontend.Components.CustomRadzen.QueryBuilder.Models.FilterOperator;

namespace Frontend.Components.CustomRadzen.QueryBuilder.Extensions
{
    /// <summary>
    /// Extension methods to convert CustomDataFilter filters to QueryRequest with auto-includes
    /// </summary>
    public static class FilterToQueryExtensions
    {
        /// <summary>
        /// Converts CustomDataFilter filters to QueryRequest with automatically generated includes
        /// </summary>
        public static QueryRequest ToQueryRequestWithIncludes<T>(this CustomDataFilter<T> dataFilter)
        {
            var includes = ExtractRequiredIncludes(dataFilter.Filters);
            var filterString = BuildFilterString(dataFilter.Filters);

            return new QueryRequest
            {
                Filter = filterString,
                Include = includes.Any() ? includes.ToArray() : null
            };
        }

        /// <summary>
        /// Extracts all required navigation property includes from nested filters
        /// </summary>
        public static List<string> ExtractRequiredIncludes(IEnumerable<CompositeFilterDescriptor> filters)
        {
            var includes = new HashSet<string>(); // Use HashSet to avoid duplicates

            foreach (var filter in filters ?? Enumerable.Empty<CompositeFilterDescriptor>())
            {
                ExtractIncludesFromFilter(filter, string.Empty, includes);
            }

            return includes.ToList();
        }

        /// <summary>
        /// Recursively extracts includes from a single filter and its nested filters
        /// </summary>
        private static void ExtractIncludesFromFilter(CompositeFilterDescriptor filter, string currentPath, HashSet<string> includes)
        {
            // Si es un filtro Related con filtros anidados
            if (filter.FilterOperator == FilterOperator.Related &&
                !string.IsNullOrEmpty(filter.NavigationPath) &&
                filter.NestedFilters?.Any() == true)
            {
                // Construir el path completo para esta navegación
                var navigationPath = string.IsNullOrEmpty(currentPath)
                    ? filter.NavigationPath
                    : $"{currentPath}.{filter.NavigationPath}";

                // Agregar el include para esta navegación
                includes.Add(navigationPath);

                // Procesar recursivamente los filtros anidados
                foreach (var nestedFilter in filter.NestedFilters)
                {
                    ExtractIncludesFromFilter(nestedFilter, navigationPath, includes);
                }
            }

            // Si el filtro tiene sub-filtros (grupos), procesarlos también
            if (filter.Filters?.Any() == true)
            {
                foreach (var subFilter in filter.Filters)
                {
                    ExtractIncludesFromFilter(subFilter, currentPath, includes);
                }
            }
        }

        /// <summary>
        /// Builds filter string from composite filter descriptors
        /// </summary>
        private static string BuildFilterString(IEnumerable<CompositeFilterDescriptor> filters)
        {
            if (!filters?.Any() == true) return null;

            var filterParts = new List<string>();

            foreach (var filter in filters)
            {
                var filterPart = BuildSingleFilterString(filter);
                if (!string.IsNullOrEmpty(filterPart))
                {
                    filterParts.Add(filterPart);
                }
            }

            return filterParts.Any() ? string.Join(" && ", filterParts) : null;
        }

        /// <summary>
        /// Builds filter string for a single composite filter descriptor
        /// </summary>
        private static string BuildSingleFilterString(CompositeFilterDescriptor filter)
        {
            // Si tiene sub-filtros (es un grupo), procesarlos
            if (filter.Filters?.Any() == true)
            {
                var subFilterParts = new List<string>();
                foreach (var subFilter in filter.Filters)
                {
                    var subFilterPart = BuildSingleFilterString(subFilter);
                    if (!string.IsNullOrEmpty(subFilterPart))
                    {
                        subFilterParts.Add(subFilterPart);
                    }
                }

                if (subFilterParts.Any())
                {
                    var combiner = filter.LogicalFilterOperator == LogicalFilterOperator.Or ? " || " : " && ";
                    return $"({string.Join(combiner, subFilterParts)})";
                }
                return null;
            }

            // Para filtros Related, construir string con navegación anidada
            if (filter.FilterOperator == FilterOperator.Related &&
                !string.IsNullOrEmpty(filter.NavigationPath) &&
                filter.NestedFilters?.Any() == true)
            {
                return BuildNestedFilterString(filter.NavigationPath, filter.NestedFilters);
            }

            // Para filtros normales, usar lógica estándar
            return BuildBasicFilterString(filter);
        }

        /// <summary>
        /// Builds filter string for nested filters with navigation path
        /// </summary>
        private static string BuildNestedFilterString(string navigationPath, IEnumerable<CompositeFilterDescriptor> nestedFilters)
        {
            var nestedFilterParts = new List<string>();

            // Agregar verificación de que la navegación no sea null
            nestedFilterParts.Add($"{navigationPath} != null");

            foreach (var nestedFilter in nestedFilters)
            {
                if (nestedFilter.FilterOperator == FilterOperator.Related &&
                    !string.IsNullOrEmpty(nestedFilter.NavigationPath) &&
                    nestedFilter.NestedFilters?.Any() == true)
                {
                    // Recursivo para relaciones anidadas más profundas
                    var deepPath = $"{navigationPath}.{nestedFilter.NavigationPath}";
                    var deepFilter = BuildNestedFilterString(deepPath, nestedFilter.NestedFilters);
                    if (!string.IsNullOrEmpty(deepFilter))
                    {
                        nestedFilterParts.Add(deepFilter);
                    }
                }
                else
                {
                    // Filtro normal en la entidad relacionada
                    var nestedFilterString = BuildBasicFilterString(nestedFilter, navigationPath);
                    if (!string.IsNullOrEmpty(nestedFilterString))
                    {
                        nestedFilterParts.Add(nestedFilterString);
                    }
                }
            }

            return nestedFilterParts.Any() ? string.Join(" && ", nestedFilterParts) : null;
        }

        /// <summary>
        /// Builds basic filter string for non-related filters
        /// </summary>
        private static string BuildBasicFilterString(CompositeFilterDescriptor filter, string navigationPrefix = null)
        {
            if (string.IsNullOrEmpty(filter.Property) || filter.FilterOperator == null)
                return null;

            var propertyPath = string.IsNullOrEmpty(navigationPrefix)
                ? filter.Property
                : $"{navigationPrefix}.{filter.Property}";

            var filterValue = filter.FilterValue?.ToString() ?? string.Empty;
            var quotedValue = $"\"{filterValue}\"";

            return filter.FilterOperator switch
            {
                FilterOperator.Equals => $"{propertyPath} == {quotedValue}",
                FilterOperator.NotEquals => $"{propertyPath} != {quotedValue}",
                FilterOperator.LessThan => $"{propertyPath} < {quotedValue}",
                FilterOperator.LessThanOrEquals => $"{propertyPath} <= {quotedValue}",
                FilterOperator.GreaterThan => $"{propertyPath} > {quotedValue}",
                FilterOperator.GreaterThanOrEquals => $"{propertyPath} >= {quotedValue}",
                FilterOperator.Contains => $"{propertyPath}.Contains({quotedValue})",
                FilterOperator.DoesNotContain => $"!{propertyPath}.Contains({quotedValue})",
                FilterOperator.StartsWith => $"{propertyPath}.StartsWith({quotedValue})",
                FilterOperator.EndsWith => $"{propertyPath}.EndsWith({quotedValue})",
                FilterOperator.IsNull => $"{propertyPath} == null",
                FilterOperator.IsNotNull => $"{propertyPath} != null",
                FilterOperator.IsEmpty => $"{propertyPath} == \"\"",
                FilterOperator.IsNotEmpty => $"{propertyPath} != \"\"",
                _ => null
            };
        }

        /// <summary>
        /// Logs the generated includes for debugging purposes
        /// </summary>
        public static void LogGeneratedIncludes(List<string> includes, string entityName = "Unknown")
        {
            if (includes?.Any() == true)
            {
                Console.WriteLine($"🔗 Auto-generated includes for {entityName}:");
                foreach (var include in includes)
                {
                    Console.WriteLine($"   📋 Include(\"{include}\")");
                }
            }
            else
            {
                Console.WriteLine($"🔗 No includes needed for {entityName}");
            }
        }
    }
}