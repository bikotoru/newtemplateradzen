using Forms.Models.DTOs;
using Forms.Models.Validation;
using Forms.Models.Configurations;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;

namespace CustomFields.API.Services;

public class CustomFieldValidationService : ICustomFieldValidationService
{
    private readonly ILogger<CustomFieldValidationService> _logger;

    public CustomFieldValidationService(ILogger<CustomFieldValidationService> logger)
    {
        _logger = logger;
    }

    public async Task<CustomFieldValidationResult> ValidateEntityCustomFieldsAsync(
        string entityName,
        Dictionary<string, object?> fieldValues,
        Guid organizationId)
    {
        var result = new CustomFieldValidationResult();

        try
        {
            // TODO: Obtener field definitions de la base de datos
            // Por ahora, asumir que las validations se harán individualmente

            result.ProcessedValues = fieldValues;
            result.IsValid = true;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating entity custom fields for {EntityName}", entityName);
            return CustomFieldValidationResult.Failure("general", $"Error de validación: {ex.Message}");
        }
    }

    public async Task<CustomFieldValidationResult> ValidateFieldAsync(
        CustomFieldDefinitionDto fieldDefinition,
        object? value)
    {
        var result = new CustomFieldValidationResult();

        try
        {
            // 1. Validar campo requerido
            if (fieldDefinition.IsRequired && IsValueEmpty(value))
            {
                result.AddError(fieldDefinition.FieldName,
                    $"{fieldDefinition.DisplayName} es obligatorio");
                return result;
            }

            // 2. Si el valor está vacío y no es requerido, es válido
            if (IsValueEmpty(value))
            {
                result.ProcessedValues[fieldDefinition.FieldName] = null;
                result.IsValid = true;
                return result;
            }

            // 3. Validar según el tipo de campo
            var processedValue = await ValidateByFieldType(fieldDefinition, value);

            // 4. Validar configuraciones específicas
            await ValidateFieldConfiguration(fieldDefinition, processedValue, result);

            if (result.IsValid)
            {
                result.ProcessedValues[fieldDefinition.FieldName] = processedValue;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating field {FieldName}", fieldDefinition.FieldName);
            return CustomFieldValidationResult.Failure(fieldDefinition.FieldName,
                $"Error de validación: {ex.Message}");
        }
    }

    public async Task<Dictionary<string, object?>> ProcessFieldValuesAsync(
        List<CustomFieldDefinitionDto> fieldDefinitions,
        Dictionary<string, object?> rawValues)
    {
        var processedValues = new Dictionary<string, object?>();

        foreach (var fieldDef in fieldDefinitions)
        {
            if (rawValues.TryGetValue(fieldDef.FieldName, out var rawValue))
            {
                var validationResult = await ValidateFieldAsync(fieldDef, rawValue);

                if (validationResult.IsValid)
                {
                    processedValues[fieldDef.FieldName] = validationResult.ProcessedValues[fieldDef.FieldName];
                }
                else
                {
                    // Log validation errors but continue processing
                    _logger.LogWarning("Validation failed for field {FieldName}: {Errors}",
                        fieldDef.FieldName, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

                    // Store raw value for now
                    processedValues[fieldDef.FieldName] = rawValue;
                }
            }
        }

        return processedValues;
    }

    private static bool IsValueEmpty(object? value)
    {
        return value == null ||
               (value is string str && string.IsNullOrWhiteSpace(str)) ||
               (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Null);
    }

    private async Task<object?> ValidateByFieldType(CustomFieldDefinitionDto fieldDefinition, object? value)
    {
        return fieldDefinition.FieldType?.ToLowerInvariant() switch
        {
            "text" or "textarea" => ValidateTextValue(value),
            "number" => ValidateNumberValue(value),
            "date" => ValidateDateValue(value),
            "boolean" => ValidateBooleanValue(value),
            "select" => ValidateSelectValue(value, fieldDefinition.UIConfig?.Options),
            "multiselect" => ValidateMultiSelectValue(value, fieldDefinition.UIConfig?.Options),
            _ => value // Tipo no reconocido, pasar tal como está
        };
    }

    private static object? ValidateTextValue(object? value)
    {
        if (value == null) return null;

        return value switch
        {
            string str => str.Trim(),
            JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.String =>
                jsonElement.GetString()?.Trim(),
            _ => value.ToString()?.Trim()
        };
    }

    private static object? ValidateNumberValue(object? value)
    {
        if (value == null) return null;

        return value switch
        {
            decimal dec => dec,
            double dbl => (decimal)dbl,
            float flt => (decimal)flt,
            int i => (decimal)i,
            long lng => (decimal)lng,
            string str when decimal.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) => result,
            JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.Number => jsonElement.GetDecimal(),
            JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.String &&
                decimal.TryParse(jsonElement.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var result) => result,
            _ => throw new ArgumentException($"Cannot convert value '{value}' to number")
        };
    }

    private static object? ValidateDateValue(object? value)
    {
        if (value == null) return null;

        return value switch
        {
            DateTime dt => dt,
            DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
            string str when DateTime.TryParse(str, out var result) => result,
            JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(jsonElement.GetString(), out var result) => result,
            _ => throw new ArgumentException($"Cannot convert value '{value}' to date")
        };
    }

    private static object? ValidateBooleanValue(object? value)
    {
        if (value == null) return false; // Default para boolean

        return value switch
        {
            bool b => b,
            string str when bool.TryParse(str, out var result) => result,
            string str when str.ToLowerInvariant() is "sí" or "si" or "yes" or "1" or "true" => true,
            string str when str.ToLowerInvariant() is "no" or "0" or "false" => false,
            int i => i != 0,
            JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.True => true,
            JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.False => false,
            JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.String &&
                bool.TryParse(jsonElement.GetString(), out var result) => result,
            _ => throw new ArgumentException($"Cannot convert value '{value}' to boolean")
        };
    }

    private static object? ValidateSelectValue(object? value, List<SelectOption>? options)
    {
        if (value == null) return null;

        var stringValue = value switch
        {
            string str => str,
            JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.String => jsonElement.GetString(),
            _ => value.ToString()
        };

        if (options != null && options.Any())
        {
            var validOption = options.FirstOrDefault(o => o.Value == stringValue && !o.Disabled);
            if (validOption == null)
            {
                throw new ArgumentException($"Value '{stringValue}' is not a valid option");
            }
        }

        return stringValue;
    }

    private static object? ValidateMultiSelectValue(object? value, List<SelectOption>? options)
    {
        if (value == null) return new List<string>();

        List<string> values;

        switch (value)
        {
            case List<string> list:
                values = list;
                break;
            case string[] array:
                values = array.ToList();
                break;
            case string str when !string.IsNullOrEmpty(str):
                try
                {
                    values = JsonSerializer.Deserialize<List<string>>(str) ?? new List<string>();
                }
                catch
                {
                    values = new List<string> { str };
                }
                break;
            case JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.Array:
                values = jsonElement.EnumerateArray()
                    .Select(e => e.GetString() ?? "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
                break;
            default:
                values = new List<string>();
                break;
        }

        // Validar que todos los valores sean opciones válidas
        if (options != null && options.Any())
        {
            var validOptionValues = options.Where(o => !o.Disabled).Select(o => o.Value).ToHashSet();
            var invalidValues = values.Where(v => !validOptionValues.Contains(v)).ToList();

            if (invalidValues.Any())
            {
                throw new ArgumentException($"Invalid options: {string.Join(", ", invalidValues)}");
            }
        }

        return values;
    }

    private async Task ValidateFieldConfiguration(
        CustomFieldDefinitionDto fieldDefinition,
        object? processedValue,
        CustomFieldValidationResult result)
    {
        if (fieldDefinition.ValidationConfig == null) return;

        var validationConfig = fieldDefinition.ValidationConfig;

        // Validaciones de texto
        if (fieldDefinition.FieldType?.ToLowerInvariant() is "text" or "textarea" && processedValue is string textValue)
        {
            if (validationConfig.MinLength.HasValue && textValue.Length < validationConfig.MinLength.Value)
            {
                result.AddError(fieldDefinition.FieldName,
                    $"{fieldDefinition.DisplayName} debe tener al menos {validationConfig.MinLength.Value} caracteres");
            }

            if (validationConfig.MaxLength.HasValue && textValue.Length > validationConfig.MaxLength.Value)
            {
                result.AddError(fieldDefinition.FieldName,
                    $"{fieldDefinition.DisplayName} no puede tener más de {validationConfig.MaxLength.Value} caracteres");
            }

            if (!string.IsNullOrEmpty(validationConfig.Pattern))
            {
                try
                {
                    var regex = new Regex(validationConfig.Pattern);
                    if (!regex.IsMatch(textValue))
                    {
                        result.AddError(fieldDefinition.FieldName,
                            $"{fieldDefinition.DisplayName} no tiene el formato correcto");
                    }
                }
                catch (ArgumentException)
                {
                    _logger.LogWarning("Invalid regex pattern for field {FieldName}: {Pattern}",
                        fieldDefinition.FieldName, validationConfig.Pattern);
                }
            }
        }

        // Validaciones numéricas
        if (fieldDefinition.FieldType?.ToLowerInvariant() == "number" && processedValue is decimal numberValue)
        {
            if (validationConfig.Min.HasValue && numberValue < validationConfig.Min.Value)
            {
                result.AddError(fieldDefinition.FieldName,
                    $"{fieldDefinition.DisplayName} debe ser mayor o igual a {validationConfig.Min.Value}");
            }

            if (validationConfig.Max.HasValue && numberValue > validationConfig.Max.Value)
            {
                result.AddError(fieldDefinition.FieldName,
                    $"{fieldDefinition.DisplayName} debe ser menor o igual a {validationConfig.Max.Value}");
            }
        }

        // Validaciones de fecha
        if (fieldDefinition.FieldType?.ToLowerInvariant() == "date" && processedValue is DateTime dateValue)
        {
            if (validationConfig.MinDate.HasValue && dateValue < validationConfig.MinDate.Value)
            {
                result.AddError(fieldDefinition.FieldName,
                    $"{fieldDefinition.DisplayName} debe ser posterior a {validationConfig.MinDate.Value:dd/MM/yyyy}");
            }

            if (validationConfig.MaxDate.HasValue && dateValue > validationConfig.MaxDate.Value)
            {
                result.AddError(fieldDefinition.FieldName,
                    $"{fieldDefinition.DisplayName} debe ser anterior a {validationConfig.MaxDate.Value:dd/MM/yyyy}");
            }
        }
    }
}