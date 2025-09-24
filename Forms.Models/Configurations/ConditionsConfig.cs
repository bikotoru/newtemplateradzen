namespace Forms.Models.Configurations;

/// <summary>
/// Configuración de condiciones para campos personalizados
/// </summary>
public class ConditionsConfig
{
    /// <summary>
    /// Condiciones para mostrar/ocultar el campo
    /// </summary>
    public List<FieldCondition>? ShowIf { get; set; }

    /// <summary>
    /// Condiciones para hacer el campo requerido
    /// </summary>
    public List<FieldCondition>? RequiredIf { get; set; }

    /// <summary>
    /// Condiciones para hacer el campo de solo lectura
    /// </summary>
    public List<FieldCondition>? ReadOnlyIf { get; set; }

    /// <summary>
    /// Operador lógico entre condiciones del mismo tipo (AND/OR)
    /// </summary>
    public string LogicalOperator { get; set; } = "AND"; // "AND" o "OR"
}

/// <summary>
/// Condición individual para un campo
/// </summary>
public class FieldCondition
{
    /// <summary>
    /// Nombre del campo que se evalúa
    /// </summary>
    public string FieldName { get; set; } = null!;

    /// <summary>
    /// Operador de comparación
    /// </summary>
    public string Operator { get; set; } = null!; // "equals", "not_equals", "contains", "greater_than", etc.

    /// <summary>
    /// Valor a comparar
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Tipo de campo que se está evaluando (para conversiones)
    /// </summary>
    public string? FieldType { get; set; }
}

/// <summary>
/// Resultado de evaluación de condiciones
/// </summary>
public class ConditionEvaluationResult
{
    public bool IsVisible { get; set; } = true;
    public bool IsRequired { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
    public List<string> Errors { get; set; } = new();
}