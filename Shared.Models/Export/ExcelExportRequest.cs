using Shared.Models.QueryModels;

namespace Shared.Models.Export
{
    /// <summary>
    /// Request para exportación a Excel
    /// </summary>
    public class ExcelExportRequest
    {
        /// <summary>
        /// Query que define qué datos exportar
        /// </summary>
        public QueryRequest Query { get; set; } = new();
        
        /// <summary>
        /// Configuración de las columnas
        /// </summary>
        public List<ExcelColumnConfig> Columns { get; set; } = new();
        
        /// <summary>
        /// Nombre de la hoja de Excel
        /// </summary>
        public string SheetName { get; set; } = "Datos";
        
        /// <summary>
        /// Incluir encabezados (nombres de columnas)
        /// </summary>
        public bool IncludeHeaders { get; set; } = true;
        
        /// <summary>
        /// Título del documento (aparece en la primera fila)
        /// </summary>
        public string? Title { get; set; }
        
        /// <summary>
        /// Subtítulo del documento (aparece en la segunda fila)
        /// </summary>
        public string? Subtitle { get; set; }
        
        /// <summary>
        /// Aplicar filtros automáticos a las columnas
        /// </summary>
        public bool AutoFilter { get; set; } = true;
        
        /// <summary>
        /// Congelar panel en la primera fila (encabezados)
        /// </summary>
        public bool FreezeHeaders { get; set; } = true;
        
        /// <summary>
        /// Aplicar formato de tabla a los datos
        /// </summary>
        public bool FormatAsTable { get; set; } = true;
        
        /// <summary>
        /// Nombre del estilo de tabla (ej: "TableStyleMedium2")
        /// </summary>
        public string? TableStyleName { get; set; }
        
        /// <summary>
        /// Ajustar automáticamente el ancho de las columnas
        /// </summary>
        public bool AutoFitColumns { get; set; } = true;
        
        /// <summary>
        /// Máximo número de filas a exportar (0 = sin límite)
        /// </summary>
        public int MaxRows { get; set; } = 0;
        
        /// <summary>
        /// Incluir fila de totales al final
        /// </summary>
        public bool IncludeTotalsRow { get; set; } = false;
        
        /// <summary>
        /// Configuración de totales por columna
        /// </summary>
        public Dictionary<string, ExcelTotalFunction>? ColumnTotals { get; set; }
        
        /// <summary>
        /// Metadatos adicionales para el archivo
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
        
        /// <summary>
        /// Propiedades del documento
        /// </summary>
        public ExcelDocumentProperties? DocumentProperties { get; set; }
    }

    /// <summary>
    /// Funciones de total para columnas
    /// </summary>
    public enum ExcelTotalFunction
    {
        None,
        Sum,
        Average,
        Count,
        CountNumbers,
        Max,
        Min,
        StdDev,
        Var
    }

    /// <summary>
    /// Propiedades del documento Excel
    /// </summary>
    public class ExcelDocumentProperties
    {
        public string? Title { get; set; }
        public string? Subject { get; set; }
        public string? Author { get; set; }
        public string? Manager { get; set; }
        public string? Company { get; set; }
        public string? Category { get; set; }
        public string? Keywords { get; set; }
        public string? Comments { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}