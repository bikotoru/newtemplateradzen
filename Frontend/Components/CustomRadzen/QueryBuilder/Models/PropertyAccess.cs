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
        /// Uses intelligent analysis to detect navigation properties
        /// </summary>
        public static bool IsGuidRelation(Type entityType, string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || entityType == null)
                return false;

            var property = entityType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                return false;

            // Must be a Guid or Guid? type
            var underlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (underlyingType != typeof(Guid))
                return false;

            // Must end with "Id" (convention for foreign keys)
            if (!propertyName.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                return false;

            // Get potential navigation property name by removing "Id" suffix
            var navigationPropertyName = propertyName.Substring(0, propertyName.Length - 2);

            // Look for corresponding navigation property
            var navigationProperty = entityType.GetProperty(navigationPropertyName, BindingFlags.Public | BindingFlags.Instance);

            if (navigationProperty == null)
                return false;

            // Navigation property should be a class (not string or primitive)
            var navigationUnderlyingType = Nullable.GetUnderlyingType(navigationProperty.PropertyType) ?? navigationProperty.PropertyType;

            return navigationUnderlyingType.IsClass &&
                   navigationUnderlyingType != typeof(string) &&
                   !navigationUnderlyingType.IsPrimitive;
        }

        /// <summary>
        /// Gets the display name for a property, using the navigation property name if it's a foreign key
        /// Uses intelligent analysis to convert "RegionId" -> "Region" automatically
        /// </summary>
        public static string GetDisplayName(Type entityType, string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return propertyName;

            // If it's a Guid relation (detected intelligently), return the navigation property name
            if (IsGuidRelation(entityType, propertyName))
            {
                var navigationPropertyName = propertyName.Substring(0, propertyName.Length - 2);
                return navigationPropertyName;
            }

            // If not a relation, return the original property name
            return propertyName;
        }

        /// <summary>
        /// Gets the type of the related entity for a Guid relation property
        /// </summary>
        public static Type GetRelatedEntityType(Type entityType, string propertyName)
        {
            if (!IsGuidRelation(entityType, propertyName))
                return null;

            var navigationPropertyName = propertyName.Substring(0, propertyName.Length - 2);
            var navigationProperty = entityType.GetProperty(navigationPropertyName, BindingFlags.Public | BindingFlags.Instance);

            if (navigationProperty == null)
                return null;

            // Get the actual type, handling nullable types
            return Nullable.GetUnderlyingType(navigationProperty.PropertyType) ?? navigationProperty.PropertyType;
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