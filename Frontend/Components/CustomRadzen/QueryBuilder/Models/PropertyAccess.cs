using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Frontend.Components.CustomRadzen.QueryBuilder.Models
{
    /// <summary>
    /// Property access utilities
    /// </summary>
    public static class PropertyAccess
    {
        /// <summary>
        /// Gets the property type for a nested property path
        /// </summary>
        public static Type GetPropertyType(Type type, string property)
        {
            if (string.IsNullOrEmpty(property))
                return type;

            var parts = property.Split('.');
            var currentType = type;

            foreach (var part in parts)
            {
                var prop = currentType.GetProperty(part, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null)
                    return null;
                currentType = prop.PropertyType;
            }

            return currentType;
        }

        /// <summary>
        /// Creates a getter function for a property
        /// </summary>
        public static Func<TItem, object> Getter<TItem, TProperty>(string property)
        {
            var parameter = Expression.Parameter(typeof(TItem), "item");
            var propertyExpression = GetNestedPropertyExpression(parameter, property);
            var convertExpression = Expression.Convert(propertyExpression, typeof(object));
            var lambda = Expression.Lambda<Func<TItem, object>>(convertExpression, parameter);
            return lambda.Compile();
        }

        /// <summary>
        /// Checks if a type is an enum
        /// </summary>
        public static bool IsEnum(Type type)
        {
            return type.IsEnum;
        }

        /// <summary>
        /// Checks if a type is a nullable enum
        /// </summary>
        public static bool IsNullableEnum(Type type)
        {
            return Nullable.GetUnderlyingType(type)?.IsEnum == true;
        }

        /// <summary>
        /// Checks if a type is numeric
        /// </summary>
        public static bool IsNumeric(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            return underlyingType == typeof(int) || underlyingType == typeof(long) ||
                   underlyingType == typeof(short) || underlyingType == typeof(byte) ||
                   underlyingType == typeof(uint) || underlyingType == typeof(ulong) ||
                   underlyingType == typeof(ushort) || underlyingType == typeof(sbyte) ||
                   underlyingType == typeof(float) || underlyingType == typeof(double) ||
                   underlyingType == typeof(decimal);
        }

        /// <summary>
        /// Checks if a type is a date type
        /// </summary>
        public static bool IsDate(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            return underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset) || underlyingType == typeof(DateOnly);
        }

        /// <summary>
        /// Checks if a property is a Guid that represents a foreign key relationship
        /// </summary>
        public static bool IsGuidRelation(Type entityType, string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return false;

            var property = entityType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                return false;

            var underlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (underlyingType != typeof(Guid))
                return false;

            // Check if property name ends with "Id" and there's a corresponding navigation property
            if (propertyName.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
            {
                var navigationPropertyName = propertyName.Substring(0, propertyName.Length - 2);
                var navigationProperty = entityType.GetProperty(navigationPropertyName, BindingFlags.Public | BindingFlags.Instance);
                return navigationProperty != null && navigationProperty.PropertyType.IsClass;
            }

            return false;
        }

        /// <summary>
        /// Gets the display name for a property, using the navigation property name if it's a foreign key
        /// </summary>
        public static string GetDisplayName(Type entityType, string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return propertyName;

            if (IsGuidRelation(entityType, propertyName) && propertyName.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
            {
                var navigationPropertyName = propertyName.Substring(0, propertyName.Length - 2);
                var navigationProperty = entityType.GetProperty(navigationPropertyName, BindingFlags.Public | BindingFlags.Instance);
                if (navigationProperty != null)
                {
                    return navigationPropertyName;
                }
            }

            return propertyName;
        }

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
    }
}