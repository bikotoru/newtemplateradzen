namespace Forms.Models.Configurations;

/// <summary>
/// Configuración de interfaz de usuario para campos personalizados
/// </summary>
public class UIConfig
{
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }

    // TextArea específico
    public int? Rows { get; set; }

    // Number específico
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public int? DecimalPlaces { get; set; } // N0, N1, N2, etc.

    // Boolean específico
    public string? Style { get; set; } // "checkbox", "switch", "radio"
    public string? TrueLabel { get; set; }
    public string? FalseLabel { get; set; }

    // Select/MultiSelect específico
    public List<SelectOption>? Options { get; set; }
    public bool ShowSelectAll { get; set; }

    // Date específico
    public string? Format { get; set; }
    public bool ShowCalendar { get; set; } = true;

    // Reference específico
    public ReferenceConfig? ReferenceConfig { get; set; }
}

/// <summary>
/// Opción para campos Select/MultiSelect
/// </summary>
public class SelectOption
{
    public string Value { get; set; } = null!;
    public string Label { get; set; } = null!;
    public bool Disabled { get; set; }
    public string? Description { get; set; }
}