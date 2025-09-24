using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Services;
using Backend.Utils.Data;
using Shared.Models.Entities.SystemEntities;
using Shared.Models.DTOs.Auth;
using System.Linq.Dynamic.Core;

namespace CustomFields.API.Services
{
    /// <summary>
    /// Servicio especializado para SystemFormEntities que maneja correctamente
    /// los filtros de organización permitiendo entidades del sistema (OrganizationId = null)
    /// </summary>
    public class SystemFormEntityService : BaseQueryService<SystemFormEntities>
    {
        public SystemFormEntityService(AppDbContext context, ILogger<SystemFormEntityService> logger)
            : base(context, logger)
        {
        }

        /// <summary>
        /// Filtro de organización especializado para SystemFormEntities
        /// Permite entidades del sistema (OrganizationId = null) + entidades de la organización del usuario
        /// </summary>
        private IQueryable<SystemFormEntities> ApplyCustomOrganizationFilter(IQueryable<SystemFormEntities> query, SessionDataDto sessionData)
        {
            var userOrganizationId = sessionData.Organization.Id;

            // Para SystemFormEntities permitimos tanto entidades del sistema como de la organización del usuario
            query = query.Where(e => e.OrganizationId == null || e.OrganizationId == userOrganizationId);

            _logger.LogDebug("Aplicado filtro de organización especial para SystemFormEntities: incluye sistema (null) + org {OrgId}", userOrganizationId);

            return query;
        }

        /// <summary>
        /// Método para obtener entidades disponibles con filtros de seguridad aplicados automáticamente
        /// </summary>
        public async Task<List<SystemFormEntities>> GetAvailableEntitiesAsync(
            SessionDataDto sessionData,
            string? search = null,
            string? category = null,
            bool? allowCustomFields = null,
            int skip = 0,
            int take = 50)
        {
            try
            {
                _logger.LogInformation($"Getting available SystemFormEntities for user {sessionData.Id}");

                // Construir query base (automáticamente aplica filtros de organización correctos)
                IQueryable<SystemFormEntities> query = _context.Set<SystemFormEntities>()
                    .Where(x => x.Active == true);

                // Aplicar filtro de organización personalizado
                query = ApplyCustomOrganizationFilter(query, sessionData);

                // Aplicar filtros adicionales
                if (!string.IsNullOrEmpty(search))
                {
                    var searchTerm = search.ToLower();
                    query = query.Where(x =>
                        x.EntityName.ToLower().Contains(searchTerm) ||
                        x.DisplayName.ToLower().Contains(searchTerm) ||
                        (x.Description != null && x.Description.ToLower().Contains(searchTerm)));
                }

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(x => x.Category == category);
                }

                if (allowCustomFields.HasValue)
                {
                    query = query.Where(x => x.AllowCustomFields == allowCustomFields.Value);
                }

                // Aplicar ordenamiento y paginación
                var result = await query
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.DisplayName)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available SystemFormEntities");
                throw;
            }
        }

        /// <summary>
        /// Obtener entidad por nombre con filtros de seguridad
        /// </summary>
        public async Task<SystemFormEntities?> GetByEntityNameAsync(string entityName, SessionDataDto sessionData)
        {
            try
            {
                _logger.LogInformation($"Getting SystemFormEntities by name: {entityName} for user {sessionData.Id}");

                var query = _context.Set<SystemFormEntities>()
                    .Where(x => x.Active == true && x.EntityName == entityName);

                // Aplicar filtro de organización personalizado
                query = ApplyCustomOrganizationFilter(query, sessionData);

                return await query.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting SystemFormEntities by name: {entityName}");
                throw;
            }
        }

        /// <summary>
        /// Obtener categorías disponibles con filtros de seguridad
        /// </summary>
        public async Task<List<string>> GetCategoriesAsync(SessionDataDto sessionData)
        {
            try
            {
                _logger.LogInformation($"Getting SystemFormEntities categories for user {sessionData.Id}");

                var query = _context.Set<SystemFormEntities>()
                    .Where(x => x.Active == true);

                // Aplicar filtro de organización personalizado
                query = ApplyCustomOrganizationFilter(query, sessionData);

                var categories = await query
                    .Select(x => x.Category)
                    .Where(x => x != null)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();

                return categories.Where(x => x != null).Cast<string>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SystemFormEntities categories");
                throw;
            }
        }

        /// <summary>
        /// Override QueryAsync para usar filtros de organización personalizados
        /// </summary>
        public override async Task<List<SystemFormEntities>> QueryAsync(Shared.Models.QueryModels.QueryRequest queryRequest, SessionDataDto sessionData)
        {
            _logger.LogInformation($"Executing custom query for {typeof(SystemFormEntities).Name}");

            try
            {
                var query = BuildCustomQuery(queryRequest, sessionData);
                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing custom query for {typeof(SystemFormEntities).Name}");
                throw;
            }
        }

        /// <summary>
        /// Override QueryPagedAsync para usar filtros de organización personalizados
        /// </summary>
        public override async Task<Shared.Models.QueryModels.PagedResult<SystemFormEntities>> QueryPagedAsync(Shared.Models.QueryModels.QueryRequest queryRequest, SessionDataDto sessionData)
        {
            _logger.LogInformation($"Executing custom paged query for {typeof(SystemFormEntities).Name}");

            try
            {
                var baseQuery = BuildCustomQuery(queryRequest, sessionData, skipPagination: true);

                // Contar total sin paginación
                var totalCount = await baseQuery.CountAsync();

                // Aplicar paginación
                var query = baseQuery;
                if (queryRequest.Skip.HasValue)
                    query = query.Skip(queryRequest.Skip.Value);
                if (queryRequest.Take.HasValue)
                    query = query.Take(queryRequest.Take.Value);

                var data = await query.ToListAsync();

                var page = queryRequest.Skip.HasValue && queryRequest.Take.HasValue
                    ? (queryRequest.Skip.Value / queryRequest.Take.Value) + 1
                    : 1;
                var pageSize = queryRequest.Take ?? totalCount;

                return new Shared.Models.QueryModels.PagedResult<SystemFormEntities>
                {
                    Data = data,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing custom paged query for {typeof(SystemFormEntities).Name}");
                throw;
            }
        }

        /// <summary>
        /// Construir query personalizada que usa filtros de organización correctos para SystemFormEntities
        /// </summary>
        private IQueryable<SystemFormEntities> BuildCustomQuery(Shared.Models.QueryModels.QueryRequest queryRequest, SessionDataDto sessionData, bool skipPagination = false)
        {
            IQueryable<SystemFormEntities> query = _context.Set<SystemFormEntities>();

            // CRÍTICO: Aplicar nuestro filtro de organización personalizado PRIMERO
            query = ApplyCustomOrganizationFilter(query, sessionData);

            // Aplicar includes
            if (queryRequest.Include != null && queryRequest.Include.Any())
            {
                foreach (var include in queryRequest.Include)
                {
                    if (!string.IsNullOrEmpty(include))
                    {
                        query = query.Include(include);
                    }
                }
            }

            // Aplicar filtros
            if (!string.IsNullOrEmpty(queryRequest.Filter))
            {
                query = query.Where(queryRequest.Filter);
            }

            // Aplicar ordenamiento
            if (!string.IsNullOrEmpty(queryRequest.OrderBy))
            {
                query = query.OrderBy(queryRequest.OrderBy);
            }

            // No aplicar paginación si se solicita skip
            if (!skipPagination)
            {
                if (queryRequest.Skip.HasValue)
                    query = query.Skip(queryRequest.Skip.Value);
                if (queryRequest.Take.HasValue)
                    query = query.Take(queryRequest.Take.Value);
            }

            return query;
        }
    }
}