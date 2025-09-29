using System;
using System.Collections.Generic;

namespace Frontend.Components.CustomRadzen.QueryBuilder.Models
{
    /// <summary>
    /// Defines filter operators
    /// </summary>
    public enum FilterOperator
    {
        /// <summary>
        /// Satisfied if the current value equals the specified value.
        /// </summary>
        Equals,
        /// <summary>
        /// Satisfied if the current value does not equal the specified value.
        /// </summary>
        NotEquals,
        /// <summary>
        /// Satisfied if the current value is less than the specified value.
        /// </summary>
        LessThan,
        /// <summary>
        /// Satisfied if the current value is less than or equal to the specified value.
        /// </summary>
        LessThanOrEquals,
        /// <summary>
        /// Satisfied if the current value is greater than the specified value.
        /// </summary>
        GreaterThan,
        /// <summary>
        /// Satisfied if the current value is greater than or equal to the specified value.
        /// </summary>
        GreaterThanOrEquals,
        /// <summary>
        /// Satisfied if the current value contains the specified value.
        /// </summary>
        Contains,
        /// <summary>
        /// Satisfied if the current value does not contain the specified value.
        /// </summary>
        DoesNotContain,
        /// <summary>
        /// Satisfied if the current value ends with the specified value.
        /// </summary>
        EndsWith,
        /// <summary>
        /// Satisfied if the current value starts with the specified value.
        /// </summary>
        StartsWith,
        /// <summary>
        /// Satisfied if the current value is null.
        /// </summary>
        IsNull,
        /// <summary>
        /// Satisfied if the current value is not null.
        /// </summary>
        IsNotNull,
        /// <summary>
        /// Satisfied if the current value is empty.
        /// </summary>
        IsEmpty,
        /// <summary>
        /// Satisfied if the current value is not empty.
        /// </summary>
        IsNotEmpty,
        /// <summary>
        /// Satisfied if the current value is in the specified collection.
        /// </summary>
        In,
        /// <summary>
        /// Satisfied if the current value is not in the specified collection.
        /// </summary>
        NotIn,
        /// <summary>
        /// Use a custom filter.
        /// </summary>
        Custom,
        /// <summary>
        /// Filter by related entity (for Guid foreign keys).
        /// </summary>
        Related
    }

    /// <summary>
    /// Defines logical filter operators
    /// </summary>
    public enum LogicalFilterOperator
    {
        /// <summary>
        /// All filters should be satisfied.
        /// </summary>
        And,
        /// <summary>
        /// Any filter should be satisfied.
        /// </summary>
        Or
    }

    /// <summary>
    /// Defines filter case sensitivity options
    /// </summary>
    public enum FilterCaseSensitivity
    {
        /// <summary>
        /// Default case sensitivity
        /// </summary>
        Default,
        /// <summary>
        /// Case insensitive filtering
        /// </summary>
        CaseInsensitive
    }

    /// <summary>
    /// Represents a composite filter descriptor
    /// </summary>
    public class CompositeFilterDescriptor
    {
        /// <summary>
        /// Gets or sets the name of the filtered property.
        /// </summary>
        /// <value>The property.</value>
        public string Property { get; set; }

        /// <summary>
        /// Gets or sets the property type.
        /// </summary>
        /// <value>The property type.</value>
        public Type Type { get; set; }

        /// <summary>
        /// Gets or sets the name of the filtered property.
        /// </summary>
        /// <value>The property.</value>
        public string FilterProperty { get; set; }

        /// <summary>
        /// Gets or sets the value to filter by.
        /// </summary>
        /// <value>The filter value.</value>
        public object FilterValue { get; set; }

        /// <summary>
        /// Gets or sets the operator which will compare the property value with <see cref="FilterValue" />.
        /// </summary>
        /// <value>The filter operator.</value>
        public FilterOperator? FilterOperator { get; set; }

        /// <summary>
        /// Gets or sets the logic used to combine the outcome of filtering by <see cref="FilterValue" />.
        /// </summary>
        /// <value>The logical filter operator.</value>
        public LogicalFilterOperator LogicalFilterOperator { get; set; }

        /// <summary>
        /// Gets or sets the filters.
        /// </summary>
        /// <value>The filters.</value>
        public IEnumerable<CompositeFilterDescriptor> Filters { get; set; }
    }
}