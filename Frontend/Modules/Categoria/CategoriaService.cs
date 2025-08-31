using Frontend.Services;
using Shared.Models.Entities;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.Builders;
using Shared.Models.QueryModels;

namespace Frontend.Modules.Categoria
{
    public class CategoriaService : BaseApiService<Shared.Models.Entities.Categoria>
    {
        public CategoriaService(HttpClient httpClient, ILogger<CategoriaService> logger) 
            : base(httpClient, logger, "api/categoria")
        {
        }

        // ✅ Hereda automáticamente todos los métodos base:
        
        // 📋 CRUD Individual:
        // - CreateAsync(CreateRequest<Categoria>)
        // - UpdateAsync(UpdateRequest<Categoria>)
        // - GetAllPagedAsync(page, pageSize)
        // - GetAllUnpagedAsync()
        // - GetByIdAsync(id)
        // - DeleteAsync(id)
        
        // 📦 CRUD por Lotes:
        // - CreateBatchAsync(CreateBatchRequest<Categoria>)
        // - UpdateBatchAsync(UpdateBatchRequest<Categoria>)
        
        
        // 🚀 Strongly Typed Query Builder:
        // - Query().Where(c => c.Active).Search("term").InFields(c => c.Nombre).ToListAsync()
        // - Query().Include(c => c.Usuario).OrderBy(c => c.Fecha).ToPagedResultAsync()
        
        // ⚡ Health Check:
        // - HealthCheckAsync()

        // ✅ Solo métodos custom permitidos aquí

        #region Custom Business Methods

        /// <summary>
        /// Ejemplo de búsqueda compleja fuertemente tipada:
        /// - Buscar por nombre con término
        /// - Incluir relación Usuario
        /// - Filtrar por clave específica
        /// </summary>
        public async Task<List<CategoriaResumen>> BuscarPorNombreConUsuarioAsync(string termino, string clave = "Soporte.2019")
        {
            try
            {
                return await Query()
                    .Where(c => c.Creador.Password == clave)           // ✅ WHERE fuertemente tipado
                    .Include(c => c.Creador)                // ✅ INCLUDE fuertemente tipado
                    .Search(termino)                        // ✅ SEARCH inteligente
                    .InFields(c => c.Nombre, c => c.Descripcion)  // ✅ Campos de búsqueda tipados
                    .OrderBy(c => c.FechaCreacion, true)    // ✅ ORDER BY descendente
                    .Select(c => new CategoriaResumen       // ✅ SELECT con proyección tipada
                    {
                        Id = c.Id,
                        Nombre = c.Nombre,
                        Descripcion = c.Descripcion,
                        CreadorNombre = c.Creador.Nombre,   // ✅ Acceso a relación incluida
                        FechaCreacion = c.FechaCreacion,
                        Activa = c.Active
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error buscando categorías por nombre con usuario. Término: {Termino}, Clave: {Clave}", termino, clave);
                throw new InvalidOperationException($"Error al buscar categorías: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Ejemplo de exportación a Excel con configuración completa
        /// </summary>
        public async Task<byte[]> ExportarCategoriasExcelAsync(bool soloActivas = true)
        {
            try
            {
                var exportRequest = new Shared.Models.Builders.ExcelExportBuilder<Shared.Models.Entities.Categoria>()
                    .WithQuery(
                        Query()
                            .Where(c => soloActivas ? c.Active == true : true)
                            .Include(c => c.Creador)
                            .ToQueryRequest()
                    )
                    .WithColumn(c => c.Codigo, "Código", Shared.Models.Export.ExcelFormat.Code)
                    .WithColumn(c => c.Nombre, "Categoría")
                    .WithColumn(c => c.Descripcion, "Descripción", wrapText: true, width: 30)
                    .WithColumn(c => c.Creador.Nombre, "Usuario Creador")
                    .WithColumn(c => c.FechaCreacion, "Fecha Creación", Shared.Models.Export.ExcelFormat.DateTime)
                    .WithColumn(c => c.Active, "Estado", Shared.Models.Export.ExcelFormat.ActiveInactive)
                    .WithTitle("Reporte de Categorías", soloActivas ? "Solo categorías activas" : "Todas las categorías")
                    .WithSheetName("Categorias")
                    .WithFormatting(autoFilter: true, formatAsTable: true, freezeHeaders: true)
                    .WithDocumentProperties(
                        title: "Reporte de Categorías",
                        author: "Sistema de Gestión",
                        company: "Mi Empresa",
                        subject: "Exportación de datos de categorías"
                    )
                    .Build();

                return await ExportToExcelAsync(exportRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exportando categorías a Excel");
                throw new InvalidOperationException($"Error al exportar categorías: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exportación simple con detección automática de formatos (retorna bytes)
        /// </summary>
        public async Task<byte[]> ExportarCategoriasSimpleAsync()
        {
            var exportRequest = new Shared.Models.Builders.ExcelExportBuilder<Shared.Models.Entities.Categoria>()
                .WithQuery(Query().Where(c => c.Active == true).ToQueryRequest())
                .WithColumn(c => c.Nombre, "Categoría")              // Auto: Text
                .WithColumn(c => c.Descripcion, "Descripción")       // Auto: Text  
                .WithColumn(c => c.FechaCreacion, "Fecha Creación")  // Auto: DateTime
                .WithColumn(c => c.Active, "Activa")                 // Auto: YesNo
                .Build();

            return await ExportToExcelAsync(exportRequest);
        }

        /// <summary>
        /// Descarga automática de Excel usando JavaScript
        /// </summary>
        public async Task DescargarCategoriasExcelAsync(Frontend.Services.FileDownloadService fileDownloadService, bool soloActivas = true)
        {
            try
            {
                var exportRequest = new Shared.Models.Builders.ExcelExportBuilder<Shared.Models.Entities.Categoria>()
                    .WithQuery(
                        Query()
                            .Where(c => soloActivas ? c.Active == true : true)
                            .Include(c => c.Creador)
                            .ToQueryRequest()
                    )
                    .WithColumn(c => c.Codigo, "Código", Shared.Models.Export.ExcelFormat.Code)
                    .WithColumn(c => c.Nombre, "Categoría")
                    .WithColumn(c => c.Descripcion, "Descripción", wrapText: true, width: 30)
                    .WithColumn(c => c.Creador.Nombre, "Usuario Creador")
                    .WithColumn(c => c.FechaCreacion, "Fecha Creación", Shared.Models.Export.ExcelFormat.DateTime)
                    .WithColumn(c => c.Active, "Estado", Shared.Models.Export.ExcelFormat.ActiveInactive)
                    .WithTitle("Reporte de Categorías", soloActivas ? "Solo categorías activas" : "Todas las categorías")
                    .WithSheetName("Categorias")
                    .WithFormatting(autoFilter: true, formatAsTable: true, freezeHeaders: true)
                    .Build();

                await DownloadExcelAsync(exportRequest, fileDownloadService, "Categorias_Reporte.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error descargando categorías Excel");
                throw new InvalidOperationException($"Error al descargar categorías: {ex.Message}", ex);
            }
        }

        #endregion
    }

    #region DTOs

    public class CategoriaResumen
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string CreadorNombre { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public bool Activa { get; set; }
    }

    #endregion
}