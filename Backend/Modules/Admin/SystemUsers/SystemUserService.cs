using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Shared.Models.QueryModels;
using Shared.Models.Responses;
using Shared.Models.Requests;
using Shared.Models.DTOs.Auth;
using System.Linq.Dynamic.Core;

namespace Backend.Modules.Admin.SystemUsers
{
    public class SystemUserService : BaseQueryService<Shared.Models.Entities.SystemEntities.SystemUsers>
    {
        public SystemUserService(AppDbContext context, ILogger<SystemUserService> logger) 
            : base(context, logger)
        {
        }

        /// <summary>
        /// Obtener usuarios filtrados con paginación: Global (OrganizationId = null) + Mi Organización
        /// REGLA: Visualizar = Global + Mi Organización
        /// Retorna PagedResult compatible con EntityTable
        /// </summary>
        public async Task<Shared.Models.QueryModels.PagedResult<Shared.Models.Entities.SystemEntities.SystemUsers>> GetFilteredUsersPagedAsync(QueryRequest queryRequest, SessionDataDto sessionData)
        {
            _logger.LogInformation("Obteniendo usuarios filtrados para Organization {OrganizationId}, Filter: '{Filter}', OrderBy: '{OrderBy}', Skip: {Skip}, Take: {Take}", 
                sessionData.OrganizationId, queryRequest.Filter, queryRequest.OrderBy, queryRequest.Skip, queryRequest.Take);

            try
            {
                // REGLA DE NEGOCIO: Aplicar filtro Global + Mi Organización
                var baseQuery = _dbSet.Where(u => u.OrganizationId == null || u.OrganizationId == sessionData.OrganizationId)
                                      .Where(u => u.Active)
                                      .Include(u => u.Organization)
                                      .Include(u => u.Creador)
                                      .Include(u => u.Modificador);

                // Convertir a IQueryable para poder aplicar más filtros
                IQueryable<Shared.Models.Entities.SystemEntities.SystemUsers> filteredQuery = baseQuery;

                // Aplicar filtro de EntityTable si se especifica
                if (!string.IsNullOrEmpty(queryRequest.Filter))
                {
                    _logger.LogInformation("Aplicando filtro: {Filter}", queryRequest.Filter);
                    try 
                    {
                        // System.Linq.Dynamic.Core procesa filtros automáticamente
                        filteredQuery = filteredQuery.Where(queryRequest.Filter);
                    }
                    catch (Exception filterEx)
                    {
                        _logger.LogWarning(filterEx, "No se pudo aplicar filtro dinámico: {Filter}", queryRequest.Filter);
                        
                        // Fallback manual para filtros específicos
                        if (queryRequest.Filter.Contains("Nombre"))
                        {
                            var searchTerm = ExtractSearchTermFromFilter(queryRequest.Filter);
                            if (!string.IsNullOrEmpty(searchTerm))
                            {
                                filteredQuery = filteredQuery.Where(u => 
                                    u.Nombre != null && u.Nombre.ToLower().Contains(searchTerm.ToLower()));
                            }
                        }
                        else if (queryRequest.Filter.Contains("Email"))
                        {
                            var searchTerm = ExtractSearchTermFromFilter(queryRequest.Filter);
                            if (!string.IsNullOrEmpty(searchTerm))
                            {
                                filteredQuery = filteredQuery.Where(u => 
                                    u.Email != null && u.Email.ToLower().Contains(searchTerm.ToLower()));
                            }
                        }
                    }
                }

                // Aplicar ordenamiento
                if (!string.IsNullOrEmpty(queryRequest.OrderBy))
                {
                    try
                    {
                        filteredQuery = filteredQuery.OrderBy(queryRequest.OrderBy);
                    }
                    catch (Exception orderEx)
                    {
                        _logger.LogWarning(orderEx, "No se pudo aplicar ordenamiento: {OrderBy}", queryRequest.OrderBy);
                        // Fallback a orden por defecto
                        filteredQuery = filteredQuery.OrderBy(u => u.Nombre);
                    }
                }
                else
                {
                    // Orden por defecto
                    filteredQuery = filteredQuery.OrderBy(u => u.Nombre);
                }

                // Contar total antes de paginación
                var totalCount = await filteredQuery.CountAsync();

                // Aplicar paginación
                var skip = queryRequest.Skip ?? 0;
                var take = queryRequest.Take ?? 20;
                var data = await filteredQuery.Skip(skip).Take(take).ToListAsync();

                // Retornar PagedResult para compatibilidad con EntityTable
                return new Shared.Models.QueryModels.PagedResult<Shared.Models.Entities.SystemEntities.SystemUsers>
                {
                    Data = data,
                    TotalCount = totalCount,
                    Page = (skip / take) + 1,
                    PageSize = take
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios filtrados");
                throw;
            }
        }

        /// <summary>
        /// Validar si el nombre de usuario es único en Global + Mi Organización
        /// </summary>
        public async Task<bool> ValidateUsernameAsync(string username, Guid? organizationId, Guid? excludeId = null)
        {
            var query = _dbSet.Where(u => u.Nombre == username && 
                               (u.OrganizationId == null || u.OrganizationId == organizationId));
            
            if (excludeId.HasValue)
                query = query.Where(u => u.Id != excludeId.Value);
            
            return !await query.AnyAsync();
        }

        /// <summary>
        /// Validar si el email es único en Global + Mi Organización
        /// </summary>
        public async Task<bool> ValidateEmailAsync(string email, Guid? organizationId, Guid? excludeId = null)
        {
            var query = _dbSet.Where(u => u.Email == email && 
                               (u.OrganizationId == null || u.OrganizationId == organizationId));
            
            if (excludeId.HasValue)
                query = query.Where(u => u.Id != excludeId.Value);
            
            return !await query.AnyAsync();
        }

        /// <summary>
        /// Método helper para extraer término de búsqueda de filtros como "(Active == true) and (Nombre.Contains("test"))"
        /// </summary>
        private string? ExtractSearchTermFromFilter(string filter)
        {
            try
            {
                // Buscar patrón .Contains("valor")
                var containsMatch = System.Text.RegularExpressions.Regex.Match(filter, @"\.Contains\(""([^""]+)""\)");
                if (containsMatch.Success)
                {
                    return containsMatch.Groups[1].Value;
                }

                // Buscar patrón == "valor"
                var equalsMatch = System.Text.RegularExpressions.Regex.Match(filter, @"==\s*""([^""]+)""");
                if (equalsMatch.Success)
                {
                    return equalsMatch.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extrayendo término de búsqueda del filtro: {Filter}", filter);
            }

            return null;
        }
    }
}