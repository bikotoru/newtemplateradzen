using System;
using System.Collections.Generic;
using System.Linq;

namespace Frontend.Components.CustomRadzen.QueryBuilder.Models
{
    /// <summary>
    /// Extension methods for enums
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Converts an enum to key-value pairs
        /// </summary>
        public static IEnumerable<object> EnumAsKeyValuePair(Type enumType)
        {
            if (!enumType.IsEnum)
                return Enumerable.Empty<object>();

            return Enum.GetValues(enumType)
                .Cast<object>()
                .Select(value => new {
                    Text = value.ToString(),
                    Value = value
                });
        }
    }
}