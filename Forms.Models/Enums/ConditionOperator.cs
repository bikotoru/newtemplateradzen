namespace Forms.Models.Enums;

/// <summary>
/// Operadores para condiciones de campos personalizados
/// </summary>
public enum ConditionOperator
{
    Equals = 1,
    NotEquals = 2,
    In = 3,
    NotIn = 4,
    GreaterThan = 5,
    LessThan = 6,
    GreaterOrEqual = 7,
    LessOrEqual = 8,
    IsEmpty = 9,
    IsNotEmpty = 10,
    Contains = 11,
    NotContains = 12,
    StartsWith = 13,
    EndsWith = 14
}

/// <summary>
/// Tipos de condiciones para campos personalizados
/// </summary>
public enum ConditionType
{
    ShowIf = 1,
    HideIf = 2,
    RequiredIf = 3,
    OptionalIf = 4,
    ReadOnlyIf = 5,
    EditableIf = 6
}

/// <summary>
/// Extensiones para enums de condiciones
/// </summary>
public static class ConditionExtensions
{
    public static string ToStringValue(this ConditionOperator op) => op switch
    {
        ConditionOperator.Equals => "equals",
        ConditionOperator.NotEquals => "not_equals",
        ConditionOperator.In => "in",
        ConditionOperator.NotIn => "not_in",
        ConditionOperator.GreaterThan => "greater_than",
        ConditionOperator.LessThan => "less_than",
        ConditionOperator.GreaterOrEqual => "greater_or_equal",
        ConditionOperator.LessOrEqual => "less_or_equal",
        ConditionOperator.IsEmpty => "is_empty",
        ConditionOperator.IsNotEmpty => "is_not_empty",
        ConditionOperator.Contains => "contains",
        ConditionOperator.NotContains => "not_contains",
        ConditionOperator.StartsWith => "starts_with",
        ConditionOperator.EndsWith => "ends_with",
        _ => throw new ArgumentOutOfRangeException(nameof(op))
    };

    public static string ToStringValue(this ConditionType type) => type switch
    {
        ConditionType.ShowIf => "show_if",
        ConditionType.HideIf => "hide_if",
        ConditionType.RequiredIf => "required_if",
        ConditionType.OptionalIf => "optional_if",
        ConditionType.ReadOnlyIf => "readonly_if",
        ConditionType.EditableIf => "editable_if",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}