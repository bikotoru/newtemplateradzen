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
    MultiSelect = 7,
    // Nuevos tipos para referencias
    EntityReference = 8,
    UserReference = 9,
    FileReference = 10
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
        FieldType.EntityReference => "entity_reference",
        FieldType.UserReference => "user_reference",
        FieldType.FileReference => "file_reference",
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
        "entity_reference" => FieldType.EntityReference,
        "user_reference" => FieldType.UserReference,
        "file_reference" => FieldType.FileReference,
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
        FieldType.EntityReference => "Referencia a Entidad",
        FieldType.UserReference => "Referencia a Usuario",
        FieldType.FileReference => "Referencia a Archivo",
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
        FieldType.EntityReference => "Referencia a otro registro del sistema",
        FieldType.UserReference => "Referencia a un usuario del sistema",
        FieldType.FileReference => "Referencia a un archivo o documento",
        _ => throw new ArgumentOutOfRangeException(nameof(fieldType))
    };

    public static bool HasOptions(this FieldType fieldType) =>
        fieldType == FieldType.Select || fieldType == FieldType.MultiSelect;

    public static bool IsNumeric(this FieldType fieldType) =>
        fieldType == FieldType.Number;

    public static bool IsText(this FieldType fieldType) =>
        fieldType == FieldType.Text || fieldType == FieldType.TextArea;

    public static bool IsReference(this FieldType fieldType) =>
        fieldType == FieldType.EntityReference ||
        fieldType == FieldType.UserReference ||
        fieldType == FieldType.FileReference;

    public static bool RequiresLookup(this FieldType fieldType) =>
        IsReference(fieldType);
}