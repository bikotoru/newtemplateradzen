namespace Frontend.Services.Validation;

public static class ValidationExtensions
{
    /// <summary>
    /// Crea reglas de validación para Categoría con sintaxis fluida
    /// </summary>
    public static FormValidationRules CreateCategoriaValidationRules()
    {
        return FormValidationRulesBuilder
            .Create()
            .Field("Nombre", field => field
                .Required("El nombre es obligatorio")
                .Length(3, 100, "El nombre debe tener entre 3 y 100 caracteres"))
            .Field("Descripcion", field => field
                .MaxLength(255, "La descripción no puede exceder 255 caracteres"))
            .Build();
    }
    
    /// <summary>
    /// Método genérico para crear reglas de validación
    /// </summary>
    public static FormValidationRulesBuilder CreateRules()
    {
        return FormValidationRulesBuilder.Create();
    }
}

/// <summary>
/// Extensiones adicionales para el builder
/// </summary>
public static class FieldValidationBuilderExtensions
{
    public static FieldValidationBuilder Email(this FieldValidationBuilder builder, string? errorMessage = null)
    {
        return builder.Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", errorMessage ?? "Formato de email inválido");
    }
    
    public static FieldValidationBuilder Matches(this FieldValidationBuilder builder, string pattern, string? errorMessage = null)
    {
        // Implementación de RegexRule se puede agregar después
        return builder;
    }
}