using Forms.Models.DTOs;
using Forms.Models.Validation;

namespace CustomFields.API.Services;

public interface ICustomFieldValidationService
{
    /// <summary>
    /// Valida todos los custom fields para una entidad
    /// </summary>
    Task<CustomFieldValidationResult> ValidateEntityCustomFieldsAsync(
        string entityName,
        Dictionary<string, object?> fieldValues,
        Guid organizationId);

    /// <summary>
    /// Valida un campo personalizado específico
    /// </summary>
    Task<CustomFieldValidationResult> ValidateFieldAsync(
        CustomFieldDefinitionDto fieldDefinition,
        object? value);

    /// <summary>
    /// Procesa y transforma valores de campos antes de guardar
    /// </summary>
    Task<Dictionary<string, object?>> ProcessFieldValuesAsync(
        List<CustomFieldDefinitionDto> fieldDefinitions,
        Dictionary<string, object?> rawValues);
}