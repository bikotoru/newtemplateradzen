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