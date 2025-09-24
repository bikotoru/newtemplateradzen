using Forms.Models.Configurations;
using Forms.Models.Enums;
using System.Text.Json;

namespace CustomFields.API.Services;

/// <summary>
/// Evaluador de condiciones para campos personalizados
/// </summary>
public class FieldConditionEvaluator
{
    private readonly ILogger<FieldConditionEvaluator> _logger;

    public FieldConditionEvaluator(ILogger<FieldConditionEvaluator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Evalúa todas las condiciones de un campo personalizado
    /// </summary>
    public ConditionEvaluationResult EvaluateConditions(ConditionsConfig? conditions, Dictionary<string, object?> fieldValues)
    {
        var result = new ConditionEvaluationResult();

        if (conditions == null)
        {
            return result; // Sin condiciones = visible, no requerido, no readonly
        }

        try
        {
            // Evaluar condiciones ShowIf
            if (conditions.ShowIf?.Any() == true)
            {
                result.IsVisible = EvaluateConditionGroup(conditions.ShowIf, fieldValues, conditions.LogicalOperator);
            }

            // Evaluar condiciones RequiredIf
            if (conditions.RequiredIf?.Any() == true)
            {
                result.IsRequired = EvaluateConditionGroup(conditions.RequiredIf, fieldValues, conditions.LogicalOperator);
            }

            // Evaluar condiciones ReadOnlyIf
            if (conditions.ReadOnlyIf?.Any() == true)
            {
                result.IsReadOnly = EvaluateConditionGroup(conditions.ReadOnlyIf, fieldValues, conditions.LogicalOperator);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluando condiciones");
            result.Errors.Add("Error evaluando condiciones del campo");
        }

        return result;
    }

    /// <summary>
    /// Evalúa un grupo de condiciones con un operador lógico
    /// </summary>
    private bool EvaluateConditionGroup(List<FieldCondition> conditions, Dictionary<string, object?> fieldValues, string logicalOperator)
    {
        if (!conditions.Any()) return true;

        var results = conditions.Select(condition => EvaluateSingleCondition(condition, fieldValues)).ToList();

        return logicalOperator.ToUpper() switch
        {
            "OR" => results.Any(r => r),
            "AND" or _ => results.All(r => r)
        };
    }

    /// <summary>
    /// Evalúa una condición individual
    /// </summary>
    private bool EvaluateSingleCondition(FieldCondition condition, Dictionary<string, object?> fieldValues)
    {
        try
        {
            if (!fieldValues.TryGetValue(condition.FieldName, out var fieldValue))
            {
                // Campo no encontrado - asumir valor null
                fieldValue = null;
            }

            return condition.Operator.ToLower() switch
            {
                "equals" => AreEqual(fieldValue, condition.Value, condition.FieldType),
                "not_equals" => !AreEqual(fieldValue, condition.Value, condition.FieldType),
                "contains" => Contains(fieldValue, condition.Value),
                "not_contains" => !Contains(fieldValue, condition.Value),
                "greater_than" => IsGreaterThan(fieldValue, condition.Value, condition.FieldType),
                "less_than" => IsLessThan(fieldValue, condition.Value, condition.FieldType),
                "greater_or_equal" => IsGreaterOrEqual(fieldValue, condition.Value, condition.FieldType),
                "less_or_equal" => IsLessOrEqual(fieldValue, condition.Value, condition.FieldType),
                "is_empty" => IsEmpty(fieldValue),
                "is_not_empty" => !IsEmpty(fieldValue),
                "starts_with" => StartsWith(fieldValue, condition.Value),
                "ends_with" => EndsWith(fieldValue, condition.Value),
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluando condición {Operator} para campo {FieldName}",
                condition.Operator, condition.FieldName);
            return false;
        }
    }

    #region Operadores de Comparación

    private static bool AreEqual(object? fieldValue, string? conditionValue, string? fieldType)
    {
        if (fieldValue == null && conditionValue == null) return true;
        if (fieldValue == null || conditionValue == null) return false;

        return fieldType?.ToLower() switch
        {
            "number" => ParseAsDecimal(fieldValue) == ParseAsDecimal(conditionValue),
            "date" => ParseAsDateTime(fieldValue) == ParseAsDateTime(conditionValue),
            "boolean" => ParseAsBoolean(fieldValue) == ParseAsBoolean(conditionValue),
            _ => fieldValue.ToString()?.Equals(conditionValue, StringComparison.OrdinalIgnoreCase) == true
        };
    }

    private static bool Contains(object? fieldValue, string? conditionValue)
    {
        if (fieldValue == null || conditionValue == null) return false;
        return fieldValue.ToString()?.Contains(conditionValue, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsGreaterThan(object? fieldValue, string? conditionValue, string? fieldType)
    {
        return fieldType?.ToLower() switch
        {
            "number" => ParseAsDecimal(fieldValue) > ParseAsDecimal(conditionValue),
            "date" => ParseAsDateTime(fieldValue) > ParseAsDateTime(conditionValue),
            _ => string.Compare(fieldValue?.ToString(), conditionValue, StringComparison.OrdinalIgnoreCase) > 0
        };
    }

    private static bool IsLessThan(object? fieldValue, string? conditionValue, string? fieldType)
    {
        return fieldType?.ToLower() switch
        {
            "number" => ParseAsDecimal(fieldValue) < ParseAsDecimal(conditionValue),
            "date" => ParseAsDateTime(fieldValue) < ParseAsDateTime(conditionValue),
            _ => string.Compare(fieldValue?.ToString(), conditionValue, StringComparison.OrdinalIgnoreCase) < 0
        };
    }

    private static bool IsGreaterOrEqual(object? fieldValue, string? conditionValue, string? fieldType)
    {
        return fieldType?.ToLower() switch
        {
            "number" => ParseAsDecimal(fieldValue) >= ParseAsDecimal(conditionValue),
            "date" => ParseAsDateTime(fieldValue) >= ParseAsDateTime(conditionValue),
            _ => string.Compare(fieldValue?.ToString(), conditionValue, StringComparison.OrdinalIgnoreCase) >= 0
        };
    }

    private static bool IsLessOrEqual(object? fieldValue, string? conditionValue, string? fieldType)
    {
        return fieldType?.ToLower() switch
        {
            "number" => ParseAsDecimal(fieldValue) <= ParseAsDecimal(conditionValue),
            "date" => ParseAsDateTime(fieldValue) <= ParseAsDateTime(conditionValue),
            _ => string.Compare(fieldValue?.ToString(), conditionValue, StringComparison.OrdinalIgnoreCase) <= 0
        };
    }

    private static bool IsEmpty(object? fieldValue)
    {
        return fieldValue == null || string.IsNullOrWhiteSpace(fieldValue.ToString());
    }

    private static bool StartsWith(object? fieldValue, string? conditionValue)
    {
        if (fieldValue == null || conditionValue == null) return false;
        return fieldValue.ToString()?.StartsWith(conditionValue, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool EndsWith(object? fieldValue, string? conditionValue)
    {
        if (fieldValue == null || conditionValue == null) return false;
        return fieldValue.ToString()?.EndsWith(conditionValue, StringComparison.OrdinalIgnoreCase) == true;
    }

    #endregion

    #region Helpers de Conversión

    private static decimal? ParseAsDecimal(object? value)
    {
        if (value == null) return null;
        if (decimal.TryParse(value.ToString(), out var result)) return result;
        return null;
    }

    private static DateTime? ParseAsDateTime(object? value)
    {
        if (value == null) return null;
        if (DateTime.TryParse(value.ToString(), out var result)) return result;
        return null;
    }

    private static bool? ParseAsBoolean(object? value)
    {
        if (value == null) return null;
        if (bool.TryParse(value.ToString(), out var result)) return result;

        // Intentar parsear como número (0 = false, resto = true)
        if (decimal.TryParse(value.ToString(), out var numResult))
            return numResult != 0;

        return null;
    }

    #endregion

    /// <summary>
    /// Evalúa múltiples campos con sus condiciones
    /// </summary>
    public Dictionary<string, ConditionEvaluationResult> EvaluateAllFieldConditions(
        Dictionary<string, ConditionsConfig?> fieldConditions,
        Dictionary<string, object?> fieldValues)
    {
        var results = new Dictionary<string, ConditionEvaluationResult>();

        foreach (var (fieldName, conditions) in fieldConditions)
        {
            results[fieldName] = EvaluateConditions(conditions, fieldValues);
        }

        return results;
    }
}