using Frontend.Services;
using Shared.Models.Entities.SystemEntities;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.QueryModels;
using System.Linq.Expressions;

namespace Frontend.Modules.Admin.FormDesigner
{
    public class SystemFormEntitiesService : BaseApiService<SystemFormEntities>
    {
        public SystemFormEntitiesService(API api, ILogger<SystemFormEntitiesService> logger)
            : base(api, logger, "api/form-designer/entities")
        {
        }

        // ✅ Hereda automáticamente todos los métodos base:

        // 📋 CRUD Individual:
        // - CreateAsync(CreateRequest<SystemFormEntities>)
        // - UpdateAsync(UpdateRequest<SystemFormEntities>)
        // - GetAllPagedAsync(page, pageSize)
        // - GetAllUnpagedAsync()
        // - GetByIdAsync(id)
        // - DeleteAsync(id)

        // 📦 CRUD por Lotes:
        // - CreateBatchAsync(CreateBatchRequest<SystemFormEntities>)
        // - UpdateBatchAsync(UpdateBatchRequest<SystemFormEntities>)

        // 🚀 Strongly Typed Query Builder:
        // - Query().Where(e => e.Active).Search("term").InFields(e => e.EntityName, e => e.DisplayName).ToListAsync()
        // - Query().Include(e => e.Organization).OrderBy(e => e.Category).ThenBy(e => e.DisplayName).ToPagedResultAsync()

        // ⚡ Health Check:
        // - HealthCheckAsync()

        // ✅ Solo métodos custom permitidos aquí

        #region Métodos Específicos para SystemFormEntities

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
        public async Task<ApiResponse<SystemFormEntities>> GetByEntityNameAsync(string entityName)
        {
            try
            {
                var endpoint = $"{_baseUrl}/by-name/{entityName}";
                return await _api.GetAsync<SystemFormEntities>(endpoint, backendType: BackendType.FormBackend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener entidad por nombre: {EntityName}", entityName);
                return ApiResponse<SystemFormEntities>.ErrorResponse($"Error al obtener entidad '{entityName}': {ex.Message}");
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
        public async Task<ApiResponse<List<SystemFormEntities>>> QuerySecureAsync(QueryRequest queryRequest)
        {
            try
            {
                var endpoint = $"{_baseUrl}/query";
                return await _api.PostAsync<List<SystemFormEntities>>(endpoint, queryRequest, BackendType.FormBackend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar query segura");
                return ApiResponse<List<SystemFormEntities>>.ErrorResponse($"Error al ejecutar query segura: {ex.Message}");
            }
        }

        /// <summary>
        /// Ejecutar query segura paginada con filtros automáticos por organización
        /// </summary>
        public async Task<ApiResponse<PagedResult<SystemFormEntities>>> QuerySecurePagedAsync(QueryRequest queryRequest)
        {
            try
            {
                var endpoint = $"{_baseUrl}/paged";
                return await _api.PostAsync<PagedResult<SystemFormEntities>>(endpoint, queryRequest, BackendType.FormBackend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar query segura paginada");
                return ApiResponse<PagedResult<SystemFormEntities>>.ErrorResponse($"Error al ejecutar query segura paginada: {ex.Message}");
            }
        }

        #endregion

        #region Override de Métodos Base para usar FormBackend

        /// <summary>
        /// Override para usar FormBackend en lugar de GlobalBackend
        /// </summary>
        public override async Task<ApiResponse<PagedResult<SystemFormEntities>>> QueryPagedAsync(QueryRequest queryRequest, BackendType backendType = BackendType.NotSet)
        {
            try
            {
                if(backendType == BackendType.NotSet)
                {
                    backendType = BackendType.FormBackend; // Este servicio siempre usa FormBackend por defecto
                }
                _logger.LogInformation($"Executing paged query for SystemFormEntities using {backendType}");
                return await _api.PostAsync<PagedResult<SystemFormEntities>>($"{_baseUrl}/paged", queryRequest, backendType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar query paginada");
                return ApiResponse<PagedResult<SystemFormEntities>>.ErrorResponse($"Error al ejecutar query paginada: {ex.Message}");
            }
        }

        /// <summary>
        /// Override LoadDataAsync básico para usar FormBackend
        /// </summary>
        public override async Task<ApiResponse<PagedResult<SystemFormEntities>>> LoadDataAsync(LoadDataArgs args, BackendType? backendType = null)
        {
            try
            {
                _logger.LogInformation($"LoadDataAsync basic for SystemFormEntities using FormBackend - Skip: {args.Skip}, Top: {args.Top}");

                // Llamar al método base que hace toda la lógica, pero nuestro QueryPagedAsync override usa FormBackend
                return await base.LoadDataAsync(args, BackendType.FormBackend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync for SystemFormEntities");
                return ApiResponse<PagedResult<SystemFormEntities>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        /// <summary>
        /// Override LoadDataAsync con typed search fields para usar FormBackend
        /// </summary>
        public override async Task<ApiResponse<PagedResult<SystemFormEntities>>> LoadDataAsync(
            LoadDataArgs args,
            BackendType? backendType = null,
            params Expression<Func<SystemFormEntities, object>>[] searchFields)
        {
            try
            {
                // Llamar al método base que hace toda la lógica, pero nuestro QueryPagedAsync override usa FormBackend
                return await base.LoadDataAsync(args, BackendType.FormBackend, searchFields);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync with typed search fields for SystemFormEntities");
                return ApiResponse<PagedResult<SystemFormEntities>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        /// <summary>
        /// Override LoadDataAsync con base query para usar FormBackend
        /// </summary>
        public override async Task<ApiResponse<PagedResult<SystemFormEntities>>> LoadDataAsync(
            LoadDataArgs args,
            QueryBuilder<SystemFormEntities>? baseQuery = null,
            BackendType? backendType = null,
            params Expression<Func<SystemFormEntities, object>>[] searchFields)
        {
            try
            {
                // Llamar al método base que hace toda la lógica, pero nuestro QueryPagedAsync override usa FormBackend
                return await base.LoadDataAsync(args, baseQuery, BackendType.FormBackend, searchFields);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync with base query and typed search fields for SystemFormEntities");
                return ApiResponse<PagedResult<SystemFormEntities>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        #endregion

        #region Métodos de Búsqueda Avanzada

        /// <summary>
        /// Buscar entidades por término con filtros específicos
        /// </summary>
        public async Task<List<SystemFormEntities>> SearchEntitiesAsync(
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
                return new List<SystemFormEntities>();
            }
        }

        /// <summary>
        /// Obtener entidades por categoría
        /// </summary>
        public async Task<List<SystemFormEntities>> GetEntitiesByCategoryAsync(string category)
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
                return new List<SystemFormEntities>();
            }
        }

        /// <summary>
        /// Obtener solo entidades del sistema (OrganizationId = null)
        /// </summary>
        public async Task<List<SystemFormEntities>> GetSystemEntitiesAsync()
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
                return new List<SystemFormEntities>();
            }
        }

        #endregion
    }
}