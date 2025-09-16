namespace Forms.Models.Configurations;

/// <summary>
/// Configuración de validaciones para campos personalizados
/// </summary>
public class ValidationConfig
{
    // Validaciones comunes
    public bool Required { get; set; }
    public string? RequiredMessage { get; set; }

    // Text/TextArea validaciones
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Pattern { get; set; } // Regex
    public string? PatternMessage { get; set; }

    // Number validaciones
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public decimal? Step { get; set; }

    // Date validaciones
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }

    // Select/MultiSelect validaciones
    public bool AllowEmpty { get; set; } = true;
    public int? MinSelections { get; set; }
    public int? MaxSelections { get; set; }

    // Validaciones personalizadas
    public List<CustomValidationRule>? CustomRules { get; set; }
}

/// <summary>
/// Regla de validación personalizada
/// </summary>
public class CustomValidationRule
{
    public string RuleType { get; set; } = null!; // "email", "phone", "rut", etc.
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
}