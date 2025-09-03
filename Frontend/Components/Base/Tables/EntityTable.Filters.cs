using Radzen;

namespace Frontend.Components.Base.Tables;

public partial class EntityTable<T>
{
    #region Filter Methods

    private string ConvertRadzenFilterToString(FilterDescriptor filter)
    {
        if (filter == null || string.IsNullOrEmpty(filter.Property)) 
            return string.Empty;

        var property = filter.Property;
        var value = filter.FilterValue?.ToString() ?? "";
        
        return filter.FilterOperator switch
        {
            FilterOperator.Equals => $"{property} == @0",
            FilterOperator.NotEquals => $"{property} != @0",
            FilterOperator.Contains => $"({property} != null && {property}.ToLower().Contains(@0))",
            FilterOperator.DoesNotContain => $"({property} == null || !{property}.ToLower().Contains(@0))",
            FilterOperator.StartsWith => $"({property} != null && {property}.ToLower().StartsWith(@0))",
            FilterOperator.EndsWith => $"({property} != null && {property}.ToLower().EndsWith(@0))",
            FilterOperator.GreaterThan => IsNumericValue(value) ? $"{property} > {value}" : $"{property} > @0",
            FilterOperator.GreaterThanOrEquals => IsNumericValue(value) ? $"{property} >= {value}" : $"{property} >= @0",
            FilterOperator.LessThan => IsNumericValue(value) ? $"{property} < {value}" : $"{property} < @0",
            FilterOperator.LessThanOrEquals => IsNumericValue(value) ? $"{property} <= {value}" : $"{property} <= @0",
            FilterOperator.IsNull => $"{property} == null",
            FilterOperator.IsNotNull => $"{property} != null",
            FilterOperator.IsEmpty => $"string.IsNullOrEmpty({property})",
            FilterOperator.IsNotEmpty => $"!string.IsNullOrEmpty({property})",
            _ => string.Empty
        };
    }

    private bool IsNumericValue(string value)
    {
        return decimal.TryParse(value, out _);
    }

    private string ConvertRadzenFilterToStringWithIndex(FilterDescriptor filter, int paramIndex)
    {
        if (filter == null || string.IsNullOrEmpty(filter.Property)) 
            return string.Empty;

        var property = filter.Property;
        var value = filter.FilterValue?.ToString() ?? "";
        var param = $"@{paramIndex}";
        
        return filter.FilterOperator switch
        {
            FilterOperator.Equals => $"{property} == {param}",
            FilterOperator.NotEquals => $"{property} != {param}",
            FilterOperator.Contains => $"({property} != null && {property}.ToLower().Contains({param}))",
            FilterOperator.DoesNotContain => $"({property} == null || !{property}.ToLower().Contains({param}))",
            FilterOperator.StartsWith => $"({property} != null && {property}.ToLower().StartsWith({param}))",
            FilterOperator.EndsWith => $"({property} != null && {property}.ToLower().EndsWith({param}))",
            FilterOperator.GreaterThan => IsNumericValue(value) ? $"{property} > {value}" : $"{property} > {param}",
            FilterOperator.GreaterThanOrEquals => IsNumericValue(value) ? $"{property} >= {value}" : $"{property} >= {param}",
            FilterOperator.LessThan => IsNumericValue(value) ? $"{property} < {value}" : $"{property} < {param}",
            FilterOperator.LessThanOrEquals => IsNumericValue(value) ? $"{property} <= {value}" : $"{property} <= {param}",
            FilterOperator.IsNull => $"{property} == null",
            FilterOperator.IsNotNull => $"{property} != null",
            FilterOperator.IsEmpty => $"string.IsNullOrEmpty({property})",
            FilterOperator.IsNotEmpty => $"!string.IsNullOrEmpty({property})",
            _ => string.Empty
        };
    }

    private string ConvertRadzenSortToString(SortDescriptor sort)
    {
        if (sort == null || string.IsNullOrEmpty(sort.Property))
            return string.Empty;

        var direction = sort.SortOrder == SortOrder.Descending ? " desc" : "";
        return $"{sort.Property}{direction}";
    }

    #endregion
}