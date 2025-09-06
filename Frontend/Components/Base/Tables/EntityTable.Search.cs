using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Shared.Models.QueryModels;
using Shared.Models.Requests;
using System.Reflection;

namespace Frontend.Components.Base.Tables;

public partial class EntityTable<T>
{
    #region Search Methods

    private List<string> GetEffectiveSearchFields()
    {
        if (CustomSearchFields?.Any() == true)
        {
            return CustomSearchFields;
        }

        if (SearchFields?.Any() == true)
        {
            return SearchFields;
        }

        if (!string.IsNullOrWhiteSpace(currentSearchFieldsInput))
        {
            return currentSearchFieldsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        if (AutoDetectSearchFields)
        {
            return GetAutoDetectedSearchFields();
        }

        return GetVisibleFieldNames();
    }

    private List<string> GetAutoDetectedSearchFields()
    {
        var fields = new List<string>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (ExcludeProperties?.Contains(prop.Name) == true)
                continue;

            if (SearchOnlyStringAndNumeric)
            {
                if (IsStringOrNumericType(prop.PropertyType))
                {
                    fields.Add(prop.Name);
                }
            }
            else
            {
                if (IsSimpleProperty(prop.Name))
                {
                    fields.Add(prop.Name);
                }
            }
        }

        return fields;
    }

    private bool IsStringOrNumericType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        return underlyingType == typeof(string) ||
               underlyingType == typeof(int) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(double) ||
               underlyingType == typeof(float) ||
               underlyingType == typeof(short) ||
               underlyingType == typeof(byte);
    }

    private SearchRequest BuildSearchRequest(LoadDataArgs args, List<string> searchFields)
    {
        var baseQuery = new QueryRequest
        {
            Skip = args.Skip,
            Take = args.Top
        };

        if (args.Filters != null && args.Filters.Any())
        {
            var filters = args.Filters.Select(ConvertRadzenFilterToString).Where(f => !string.IsNullOrEmpty(f));
            if (filters.Any())
            {
                baseQuery.Filter = string.Join(" && ", filters);
            }
        }

        if (args.Sorts != null && args.Sorts.Any())
        {
            var sorts = args.Sorts.Select(ConvertRadzenSortToString).Where(s => !string.IsNullOrEmpty(s));
            if (sorts.Any())
            {
                baseQuery.OrderBy = string.Join(", ", sorts);
            }
        }

        if (BaseQuery != null)
        {
            // TODO: Integrar BaseQuery con baseQuery
        }

        var searchRequest = new SearchRequest
        {
            SearchTerm = searchTerm,
            SearchFields = searchFields.ToArray(),
            BaseQuery = baseQuery,
            Skip = args.Skip,
            Take = args.Top
        };

        return searchRequest;
    }

    private async Task OnSearchSubmit()
    {
        if (OnSearch.HasDelegate)
        {
            await OnSearch.InvokeAsync(searchTerm);
        }
        else
        {
            await FirstPage();
        }
    }

    private async Task OnSearchKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await OnSearchSubmit();
        }
    }

    #endregion
}