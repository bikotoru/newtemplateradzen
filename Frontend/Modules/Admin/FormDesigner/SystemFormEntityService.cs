using Frontend.Services;
using Shared.Models.Entities.SystemEntities;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.QueryModels;

namespace Frontend.Modules.Admin.FormDesigner
{
    public class SystemFormEntityService : BaseApiService<SystemFormEntity>
    {
        public SystemFormEntityService(API api, ILogger<SystemFormEntityService> logger)
            : base(api, logger, "api/systemformentity")
        {
        }

        // ✅ Hereda automáticamente todos los métodos base:

        // 📋 CRUD Individual:
        // - CreateAsync(CreateRequest<SystemFormEntity>)
        // - UpdateAsync(UpdateRequest<SystemFormEntity>)
        // - GetAllPagedAsync(page, pageSize)
        // - GetAllUnpagedAsync()
        // - GetByIdAsync(id)
        // - DeleteAsync(id)

        // 📦 CRUD por Lotes:
        // - CreateBatchAsync(CreateBatchRequest<SystemFormEntity>)
        // - UpdateBatchAsync(UpdateBatchRequest<SystemFormEntity>)

        // 🚀 Strongly Typed Query Builder:
        // - Query().Where(e => e.Active).Search("term").InFields(e => e.EntityName, e => e.DisplayName).ToListAsync()
        // - Query().Include(e => e.Organization).OrderBy(e => e.Category).ThenBy(e => e.DisplayName).ToPagedResultAsync()

        // ⚡ Health Check:
        // - HealthCheckAsync()

        // ✅ Solo métodos custom permitidos aquí

        #region Métodos Específicos para SystemFormEntity

        /// <summary>
        /// Obtener entidades disponibles filtradas por organización del usuario
        /// Incluye entidades del sistema (OrganizationId = null) y las específicas de la organización
        /// </summary>
        public async Task<ApiResponse<object>> GetAvailableEntitiesAsync(
            int page = 1,
            int pageSize = 50,
            string? search = null,
            string? category = null,
            bool? allowCustomFields = null)
        {
            try
            {
                var queryString = $"?page={page}&pageSize={pageSize}";

                if (!string.IsNullOrEmpty(search))
                    queryString += $"&search={Uri.EscapeDataString(search)}";

                if (!string.IsNullOrEmpty(category))
                    queryString += $"&category={Uri.EscapeDataString(category)}";

                if (allowCustomFields.HasValue)
                    queryString += $"&allowCustomFields={allowCustomFields.Value}";

                var endpoint = $"{_baseUrl}/available{queryString}";
                return await _api.GetAsync<object>(endpoint, BackendType.FormBackend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener entidades disponibles");
                return ApiResponse<object>.ErrorResponse($"Error al obtener entidades disponibles: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtener una entidad específica por nombre, verificando permisos de acceso
        /// </summary>
        public async Task<ApiResponse<SystemFormEntity>> GetByEntityNameAsync(string entityName)
        {
            try
            {
                var endpoint = $"{_baseUrl}/by-name/{entityName}";
                return await _api.GetAsync<SystemFormEntity>(endpoint, backendType: BackendType.FormBackend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener entidad por nombre: {EntityName}", entityName);
                return ApiResponse<SystemFormEntity>.ErrorResponse($"Error al obtener entidad '{entityName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Obtener categorías disponibles de entidades
        /// </summary>
        public async Task<ApiResponse<List<string>>> GetCategoriesAsync()
        {
            try
            {
                var endpoint = $"{_baseUrl}/categories";
                return await _api.GetAsync<List<string>>(endpoint, backendType: BackendType.FormBackend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categorías de entidades");
                return ApiResponse<List<string>>.ErrorResponse($"Error al obtener categorías: {ex.Message}");
            }
        }

        /// <summary>
        /// Ejecutar query segura con filtros automáticos por organización
        /// </summary>
        public async Task<ApiResponse<List<SystemFormEntity>>> QuerySecureAsync(QueryRequest queryRequest)
        {
            try
            {
                var endpoint = $"{_baseUrl}/query";
                return await _api.PostAsync<List<SystemFormEntity>>(endpoint, queryRequest, BackendType.FormBackend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar query segura");
                return ApiResponse<List<SystemFormEntity>>.ErrorResponse($"Error al ejecutar query segura: {ex.Message}");
            }
        }

        /// <summary>
        /// Ejecutar query segura paginada con filtros automáticos por organización
        /// </summary>
        public async Task<ApiResponse<PagedResult<SystemFormEntity>>> QuerySecurePagedAsync(QueryRequest queryRequest)
        {
            try
            {
                var endpoint = $"{_baseUrl}/paged";
                return await _api.PostAsync<PagedResult<SystemFormEntity>>(endpoint, queryRequest, BackendType.FormBackend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar query segura paginada");
                return ApiResponse<PagedResult<SystemFormEntity>>.ErrorResponse($"Error al ejecutar query segura paginada: {ex.Message}");
            }
        }

        #endregion

        #region Métodos de Búsqueda Avanzada

        /// <summary>
        /// Buscar entidades por término con filtros específicos
        /// </summary>
        public async Task<List<SystemFormEntity>> SearchEntitiesAsync(
            string searchTerm,
            string? category = null,
            bool? allowCustomFields = null,
            int maxResults = 10)
        {
            try
            {
                var queryBuilder = Query()
                    .Where(e => e.Active == true)
                    .Search(searchTerm)
                    .InFields(e => e.EntityName, e => e.DisplayName, e => e.Description)
                    .Take(maxResults);

                if (!string.IsNullOrEmpty(category))
                {
                    queryBuilder = queryBuilder.Where(e => e.Category == category);
                }

                if (allowCustomFields.HasValue)
                {
                    queryBuilder = queryBuilder.Where(e => e.AllowCustomFields == allowCustomFields.Value);
                }

                var result = await queryBuilder
                    .OrderBy(e => e.Category)
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en búsqueda de entidades con término: {SearchTerm}", searchTerm);
                return new List<SystemFormEntity>();
            }
        }

        /// <summary>
        /// Obtener entidades por categoría
        /// </summary>
        public async Task<List<SystemFormEntity>> GetEntitiesByCategoryAsync(string category)
        {
            try
            {
                var result = await Query()
                    .Where(e => e.Active == true && e.Category == category)
                    .OrderBy(e => e.DisplayName)
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener entidades por categoría: {Category}", category);
                return new List<SystemFormEntity>();
            }
        }

        /// <summary>
        /// Obtener solo entidades del sistema (OrganizationId = null)
        /// </summary>
        public async Task<List<SystemFormEntity>> GetSystemEntitiesAsync()
        {
            try
            {
                var result = await Query()
                    .Where(e => e.Active == true && e.OrganizationId == null)
                    .OrderBy(e => e.Category)
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener entidades del sistema");
                return new List<SystemFormEntity>();
            }
        }

        #endregion
    }
}