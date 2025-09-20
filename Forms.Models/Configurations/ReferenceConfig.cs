namespace Forms.Models.Configurations;

/// <summary>
/// Configuración para campos de referencia
/// </summary>
public class ReferenceConfig
{
    /// <summary>
    /// Entidad objetivo de la referencia
    /// </summary>
    public string TargetEntity { get; set; } = "";

    /// <summary>
    /// Campo que se mostrará en el dropdown (ej: "Name", "DisplayName")
    /// </summary>
    public string DisplayProperty { get; set; } = "Name";

    /// <summary>
    /// Campo que se usará como valor (ej: "Id")
    /// </summary>
    public string ValueProperty { get; set; } = "Id";

    /// <summary>
    /// Permite selección múltiple
    /// </summary>
    public bool AllowMultiple { get; set; } = false;

    /// <summary>
    /// Permite crear nuevos registros desde el campo
    /// </summary>
    public bool AllowCreate { get; set; } = true;

    /// <summary>
    /// Permite limpiar la selección
    /// </summary>
    public bool AllowClear { get; set; } = true;

    /// <summary>
    /// Filtros base para la consulta
    /// </summary>
    public List<ReferenceFilter> Filters { get; set; } = new();

    /// <summary>
    /// Campos adicionales para mostrar en el dropdown
    /// </summary>
    public List<string> AdditionalDisplayFields { get; set; } = new();

    /// <summary>
    /// Configuración del modal para crear nuevos elementos
    /// </summary>
    public ModalConfig? CreateModalConfig { get; set; }

    /// <summary>
    /// Usar cache para mejor performance
    /// </summary>
    public bool EnableCache { get; set; } = true;

    /// <summary>
    /// Tiempo de vida del cache en minutos
    /// </summary>
    public int CacheTTLMinutes { get; set; } = 5;
}

/// <summary>
/// Filtro para referencias
/// </summary>
public class ReferenceFilter
{
    /// <summary>
    /// Campo a filtrar
    /// </summary>
    public string Field { get; set; } = "";

    /// <summary>
    /// Operador de comparación
    /// </summary>
    public string Operator { get; set; } = "equals"; // equals, contains, greater_than, less_than, etc.

    /// <summary>
    /// Valor del filtro
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Si el valor viene de otro campo del formulario
    /// </summary>
    public string? SourceField { get; set; }
}

/// <summary>
/// Configuración del modal
/// </summary>
public class ModalConfig
{
    /// <summary>
    /// Ancho del modal
    /// </summary>
    public string Width { get; set; } = "600px";

    /// <summary>
    /// Alto del modal
    /// </summary>
    public string Height { get; set; } = "400px";

    /// <summary>
    /// Permitir redimensionar
    /// </summary>
    public bool Resizable { get; set; } = true;

    /// <summary>
    /// Permitir arrastrar
    /// </summary>
    public bool Draggable { get; set; } = true;

    /// <summary>
    /// Título del modal
    /// </summary>
    public string? Title { get; set; }
}