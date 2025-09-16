namespace Forms.Models.Enums;

/// <summary>
/// Tipos de campos personalizados soportados
/// </summary>
public enum FieldType
{
    Text = 1,
    TextArea = 2,
    Number = 3,
    Date = 4,
    Boolean = 5,
    Select = 6,
    MultiSelect = 7
}

/// <summary>
/// Extensiones para FieldType
/// </summary>
public static class FieldTypeExtensions
{
    public static string ToStringValue(this FieldType fieldType) => fieldType switch
    {
        FieldType.Text => "text",
        FieldType.TextArea => "textarea",
        FieldType.Number => "number",
        FieldType.Date => "date",
        FieldType.Boolean => "boolean",
        FieldType.Select => "select",
        FieldType.MultiSelect => "multiselect",
        _ => throw new ArgumentOutOfRangeException(nameof(fieldType))
    };

    public static FieldType FromString(string value) => value.ToLower() switch
    {
        "text" => FieldType.Text,
        "textarea" => FieldType.TextArea,
        "number" => FieldType.Number,
        "date" => FieldType.Date,
        "boolean" => FieldType.Boolean,
        "select" => FieldType.Select,
        "multiselect" => FieldType.MultiSelect,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid field type")
    };

    public static string GetDisplayName(this FieldType fieldType) => fieldType switch
    {
        FieldType.Text => "Texto",
        FieldType.TextArea => "Área de Texto",
        FieldType.Number => "Número",
        FieldType.Date => "Fecha",
        FieldType.Boolean => "Verdadero/Falso",
        FieldType.Select => "Lista Desplegable",
        FieldType.MultiSelect => "Selección Múltiple",
        _ => throw new ArgumentOutOfRangeException(nameof(fieldType))
    };

    public static string GetDescription(this FieldType fieldType) => fieldType switch
    {
        FieldType.Text => "Campo de texto corto",
        FieldType.TextArea => "Campo de texto largo con múltiples líneas",
        FieldType.Number => "Campo numérico con validaciones",
        FieldType.Date => "Campo de fecha con calendario",
        FieldType.Boolean => "Campo de sí/no, verdadero/falso",
        FieldType.Select => "Lista desplegable con una sola selección",
        FieldType.MultiSelect => "Lista con selección múltiple",
        _ => throw new ArgumentOutOfRangeException(nameof(fieldType))
    };

    public static bool HasOptions(this FieldType fieldType) =>
        fieldType == FieldType.Select || fieldType == FieldType.MultiSelect;

    public static bool IsNumeric(this FieldType fieldType) =>
        fieldType == FieldType.Number;

    public static bool IsText(this FieldType fieldType) =>
        fieldType == FieldType.Text || fieldType == FieldType.TextArea;
}