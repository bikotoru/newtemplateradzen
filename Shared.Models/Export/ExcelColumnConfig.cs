namespace Shared.Models.Export
{
    /// <summary>
    /// Configuración de una columna para exportación a Excel
    /// </summary>
    public class ExcelColumnConfig
    {
        /// <summary>
        /// Ruta de la propiedad (ej: "Nombre" o "Creador.Nombre")
        /// </summary>
        public string PropertyPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Nombre que aparecerá como encabezado en Excel
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>
        /// Formato predefinido para la columna
        /// </summary>
        public ExcelFormat? Format { get; set; }
        
        /// <summary>
        /// Formato personalizado (si Format es null)
        /// </summary>
        public string? CustomFormat { get; set; }
        
        /// <summary>
        /// Ancho de la columna (en caracteres)
        /// </summary>
        public double? Width { get; set; }
        
        /// <summary>
        /// Si la columna debe aparecer en negrita
        /// </summary>
        public bool Bold { get; set; } = false;
        
        /// <summary>
        /// Alineación del contenido
        /// </summary>
        public ExcelAlignment? Alignment { get; set; }
        
        /// <summary>
        /// Activar ajuste de texto (wrap)
        /// </summary>
        public bool WrapText { get; set; } = false;
        
        /// <summary>
        /// Color de fondo de la columna (formato hex: #FFFFFF)
        /// </summary>
        public string? BackgroundColor { get; set; }
        
        /// <summary>
        /// Color del texto (formato hex: #000000)
        /// </summary>
        public string? TextColor { get; set; }
        
        /// <summary>
        /// Orden de la columna (si no se especifica, usa el orden de agregado)
        /// </summary>
        public int? Order { get; set; }
        
        /// <summary>
        /// Si la columna está visible (permite preparar columnas ocultas)
        /// </summary>
        public bool Visible { get; set; } = true;
        
        /// <summary>
        /// Función de transformación personalizada para el valor
        /// </summary>
        public Func<object?, string>? ValueTransform { get; set; }
        
        /// <summary>
        /// Comentario que aparece al hacer hover sobre el encabezado
        /// </summary>
        public string? Comment { get; set; }
    }
}