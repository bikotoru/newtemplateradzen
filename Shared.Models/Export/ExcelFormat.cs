namespace Shared.Models.Export
{
    /// <summary>
    /// Formatos predefinidos para columnas de Excel
    /// </summary>
    public enum ExcelFormat
    {
        // Fechas
        Date,                    // "dd/MM/yyyy"
        DateTime,               // "dd/MM/yyyy HH:mm:ss"
        DateTimeShort,          // "dd/MM/yyyy HH:mm"
        DateOnly,               // "dd/MM/yyyy"
        TimeOnly,               // "HH:mm:ss"
        
        // Números
        Integer,                // "#,##0"
        Decimal2,               // "#,##0.00"
        Decimal4,               // "#,##0.0000"
        Currency,               // "$#,##0.00"
        CurrencyNoSymbol,       // "#,##0.00"
        Percentage,             // "0.00%"
        Scientific,             // "0.00E+00"
        
        // Texto
        Text,                   // "@" (fuerza texto)
        TextWrap,               // "@" con wrap text activado
        
        // Booleanos
        YesNo,                  // "Sí"/"No"
        TrueFalse,              // "Verdadero"/"Falso"
        ActiveInactive,         // "Activo"/"Inactivo"
        EnabledDisabled,        // "Habilitado"/"Deshabilitado"
        OnOff,                  // "Encendido"/"Apagado"
        
        // Personalizados comunes
        Phone,                  // Formato teléfono (texto)
        Email,                  // Formato email (texto)
        Url,                    // Formato URL (texto)
        Guid,                   // Formato GUID (texto)
        Code,                   // Código (texto con fuente monospace)
        
        // Automático (detecta por tipo)
        Auto
    }

    /// <summary>
    /// Alineación de columnas en Excel
    /// </summary>
    public enum ExcelAlignment
    {
        Left,
        Center,
        Right,
        Justify
    }
}