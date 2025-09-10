using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Shared.Models.QueryModels;
using Shared.Models.Responses;
using Shared.Models.Requests;
using Shared.Models.DTOs.Auth;
using Shared.Models.DTOs.UserPermissions;
using Shared.Models.DTOs.RolePermissions;
using System.Linq.Dynamic.Core;

namespace Backend.Modules.Admin.SystemPermissions
{
    public class SystemPermissionService : BaseQueryService<Shared.Models.Entities.SystemEntities.SystemPermissions>
    {
        public SystemPermissionService(AppDbContext context, ILogger<SystemPermissionService> logger) 
            : base(context, logger)
        {
        }

        /// <summary>
        /// Obtener permisos filtrados con paginación: Global (OrganizationId = null) + Mi Organización
        /// REGLA: Visualizar = Global + Mi Organización
        /// Retorna PagedResult compatible con EntityTable
        /// </summary>
        public async Task<Shared.Models.QueryModels.PagedResult<Shared.Models.Entities.SystemEntities.SystemPermissions>> GetFilteredPermissionsPagedAsync(QueryRequest queryRequest, SessionDataDto sessionData)
        {
            _logger.LogInformation("Obteniendo permisos filtrados para Organization {OrganizationId}, Filter: '{Filter}', OrderBy: '{OrderBy}', Skip: {Skip}, Take: {Take}", 
                sessionData.OrganizationId, queryRequest.Filter, queryRequest.OrderBy, queryRequest.Skip, queryRequest.Take);

            try
            {
                // REGLA DE NEGOCIO: Aplicar filtro Global + Mi Organización
                var baseQuery = _dbSet.Where(p => p.OrganizationId == null || p.OrganizationId == sessionData.OrganizationId)
                                      .Where(p => p.Active)
                                      .Include(p => p.Organization)
                                      .Include(p => p.Creador)
                                      .Include(p => p.Modificador);

                // Convertir a IQueryable para poder aplicar más filtros
                IQueryable<Shared.Models.Entities.SystemEntities.SystemPermissions> filteredQuery = baseQuery;

                // Aplicar filtro de EntityTable si se especifica
                if (!string.IsNullOrEmpty(queryRequest.Filter))
                {
                    // El filtro viene como "(Active == true) and (ActionKey.Contains(\"test\"))"
                    // Necesitamos parsearlo y aplicarlo
                    _logger.LogInformation("Aplicando filtro: {Filter}", queryRequest.Filter);
                    // Por ahora usaremos System.Linq.Dynamic.Core si está disponible
                    // o implementaremos parsing básico
                    try 
                    {
                        filteredQuery = filteredQuery.Where(queryRequest.Filter);
                    }
                    catch (Exception filterEx)
                    {
                        _logger.LogWarning(filterEx, "No se pudo aplicar filtro dinámico: {Filter}", queryRequest.Filter);
                        // Fallback: si el filtro contiene ActionKey, aplicar búsqueda manual
                        if (queryRequest.Filter.Contains("ActionKey"))
                        {
                            // Extraer valor del filtro manualmente
                            var searchTerm = ExtractSearchTermFromFilter(queryRequest.Filter);
                            if (!string.IsNullOrEmpty(searchTerm))
                            {
                                filteredQuery = filteredQuery.Where(p => 
                                    p.ActionKey != null && p.ActionKey.ToLower().Contains(searchTerm.ToLower()));
                            }
                        }
                    }
                }

                // Aplicar ordenamiento
                IOrderedQueryable<Shared.Models.Entities.SystemEntities.SystemPermissions> orderedQuery;
                if (!string.IsNullOrEmpty(queryRequest.OrderBy))
                {
                    // Separar campo y dirección
                    var orderParts = queryRequest.OrderBy.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var fieldName = orderParts[0];
                    var isDescending = orderParts.Length > 1 && orderParts[1].ToLower() == "desc";
                    
                    if (isDescending)
                    {
                        orderedQuery = filteredQuery.OrderByDescending(p => EF.Property<object>(p, fieldName));
                    }
                    else
                    {
                        orderedQuery = filteredQuery.OrderBy(p => EF.Property<object>(p, fieldName));
                    }
                }
                else
                {
                    orderedQuery = filteredQuery.OrderBy(p => p.Nombre);
                }

                // Contar total sin paginación (usando filteredQuery para contar con búsqueda aplicada)
                var totalCount = await filteredQuery.CountAsync();

                // Aplicar paginación
                var skip = queryRequest.Skip ?? 0;
                var take = queryRequest.Take ?? 20;
                var data = await orderedQuery.Skip(skip).Take(take).ToListAsync();

                // Calcular página actual
                var pageSize = take;
                var currentPage = (skip / pageSize) + 1;

                return new Shared.Models.QueryModels.PagedResult<Shared.Models.Entities.SystemEntities.SystemPermissions>
                {
                    Data = data,
                    TotalCount = totalCount,
                    Page = currentPage,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos filtrados para Organization {OrganizationId}", sessionData.OrganizationId);
                throw;
            }
        }

        /// <summary>
        /// Validar que ActionKey sea único en Global + Mi Organización
        /// REGLA: No pueden existir dos ActionKey iguales en (Global + Mi Organización)
        /// </summary>
        public async Task<bool> ValidateActionKeyAsync(string actionKey, Guid? organizationId, Guid? excludeId = null)
        {
            _logger.LogInformation("Validando ActionKey {ActionKey} para Organization {OrganizationId}", actionKey, organizationId);

            if (string.IsNullOrWhiteSpace(actionKey))
                return false;

            // REGLA DE NEGOCIO: ActionKey único en (Global + Mi Organización)
            var query = _dbSet.Where(p => p.ActionKey == actionKey && 
                                     (p.OrganizationId == null || p.OrganizationId == organizationId) &&
                                     p.Active);

            if (excludeId.HasValue)
                query = query.Where(p => p.Id != excludeId.Value);

            var exists = await query.AnyAsync();
            return !exists; // Return true if ActionKey is available (doesn't exist)
        }

        /// <summary>
        /// Obtener grupos existentes para el dropdown
        /// </summary>
        public async Task<List<string>> GetGruposExistentesAsync(SessionDataDto sessionData)
        {
            _logger.LogInformation("Obteniendo grupos existentes para Organization {OrganizationId}", sessionData.OrganizationId);

            var grupos = await _dbSet.Where(p => (p.OrganizationId == null || p.OrganizationId == sessionData.OrganizationId) &&
                                           p.Active &&
                                           !string.IsNullOrEmpty(p.GrupoNombre))
                                   .Select(p => p.GrupoNombre!)
                                   .Distinct()
                                   .OrderBy(g => g)
                                   .ToListAsync();

            return grupos;
        }

        /// <summary>
        /// Override Create para aplicar reglas de negocio
        /// REGLA: Siempre crear en mi OrganizationId (nunca null)
        /// REGLA: ActionKey = Nombre
        /// </summary>
        public override async Task<Shared.Models.Entities.SystemEntities.SystemPermissions> CreateAsync(CreateRequest<Shared.Models.Entities.SystemEntities.SystemPermissions> request, SessionDataDto sessionData)
        {
            _logger.LogInformation("Creando permiso para Organization {OrganizationId}", sessionData.OrganizationId);

            // REGLAS DE NEGOCIO:
            // 1. SIEMPRE crear en mi organización (nunca null)
            request.Entity.OrganizationId = sessionData.OrganizationId;
            
            // 2. ActionKey = Nombre (sincronizar siempre)
            request.Entity.ActionKey = request.Entity.Nombre;

            // 3. Validar ActionKey único antes de crear
            var isUnique = await ValidateActionKeyAsync(request.Entity.ActionKey!, sessionData.OrganizationId);
            if (!isUnique)
            {
                throw new InvalidOperationException($"Ya existe un permiso con ActionKey '{request.Entity.ActionKey}' en tu organización o globalmente.");
            }

            return await base.CreateAsync(request, sessionData);
        }

        /// <summary>
        /// Override Update para aplicar reglas de negocio
        /// REGLA: ActionKey = Nombre
        /// </summary>
        public override async Task<Shared.Models.Entities.SystemEntities.SystemPermissions> UpdateAsync(UpdateRequest<Shared.Models.Entities.SystemEntities.SystemPermissions> request, SessionDataDto sessionData)
        {
            _logger.LogInformation("Actualizando permiso {Id} para Organization {OrganizationId}", request.Entity.Id, sessionData.OrganizationId);

            // REGLA DE NEGOCIO: ActionKey = Nombre (sincronizar siempre)
            request.Entity.ActionKey = request.Entity.Nombre;

            // Validar ActionKey único (excluyendo el actual)
            var isUnique = await ValidateActionKeyAsync(request.Entity.ActionKey!, sessionData.OrganizationId, request.Entity.Id);
            if (!isUnique)
            {
                throw new InvalidOperationException($"Ya existe un permiso con ActionKey '{request.Entity.ActionKey}' en tu organización o globalmente.");
            }

            return await base.UpdateAsync(request, sessionData);
        }


        /// <summary>
        /// Método helper para extraer término de búsqueda de filtros como "(Active == true) and (ActionKey.Contains(\"test\"))"
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

        /// <summary>
        /// Obtener usuarios que tienen un permiso específico (paginado)
        /// </summary>
        public async Task<Shared.Models.QueryModels.PagedResult<PermissionUserDto>> GetPermissionUsersPagedAsync(PermissionUserSearchRequest request, SessionDataDto sessionData)
        {
            _logger.LogInformation("Obteniendo usuarios con permiso {PermissionId} para Organization {OrganizationId}", 
                request.PermissionId, sessionData.OrganizationId);

            try
            {
                // Verificar que el permiso existe y es accesible
                var permission = await _dbSet
                    .Where(p => p.Id == request.PermissionId && 
                               (p.OrganizationId == null || p.OrganizationId == sessionData.OrganizationId))
                    .FirstOrDefaultAsync();

                if (permission == null)
                {
                    throw new InvalidOperationException("Permiso no encontrado o sin acceso");
                }

                // Query base de usuarios en la organización
                var usersQuery = _context.Set<Shared.Models.Entities.SystemEntities.SystemUsers>()
                    .Where(u => u.OrganizationId == sessionData.OrganizationId && u.Active);

                // Aplicar filtro de búsqueda si se especifica
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    usersQuery = usersQuery.Where(u => 
                        u.Nombre.ToLower().Contains(searchTerm) ||
                        (u.Email != null && u.Email.ToLower().Contains(searchTerm)));
                }

                // Obtener usuarios con información de permisos
                var usersWithPermissions = await usersQuery
                    .Select(u => new PermissionUserDto
                    {
                        UserId = u.Id,
                        Nombre = u.Nombre,
                        Email = u.Email,
                        IsDirectlyAssigned = u.SystemUsersPermissionsSystemUsers
                            .Any(up => up.SystemPermissionsId == request.PermissionId),
                        IsInheritedFromRole = u.SystemUsersRolesSystemUsers
                            .Any(ur => ur.SystemRoles.SystemRolesPermissions
                                .Any(rp => rp.SystemPermissionsId == request.PermissionId)),
                        InheritedFromRoles = u.SystemUsersRolesSystemUsers
                            .Where(ur => ur.SystemRoles.SystemRolesPermissions
                                .Any(rp => rp.SystemPermissionsId == request.PermissionId))
                            .Select(ur => ur.SystemRoles.Nombre)
                            .ToList()
                    })
                    .ToListAsync();

                // Aplicar filtros adicionales
                if (request.ShowOnlyWithPermission)
                {
                    usersWithPermissions = usersWithPermissions
                        .Where(u => u.HasPermission)
                        .ToList();
                }

                if (request.ShowOnlyDirectAssignments)
                {
                    usersWithPermissions = usersWithPermissions
                        .Where(u => u.IsDirectlyAssigned)
                        .ToList();
                }

                // Aplicar paginación
                var totalCount = usersWithPermissions.Count;
                var pagedUsers = usersWithPermissions
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new Shared.Models.QueryModels.PagedResult<PermissionUserDto>
                {
                    Data = pagedUsers,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios con permiso {PermissionId}", request.PermissionId);
                throw;
            }
        }

        /// <summary>
        /// Obtener roles que tienen un permiso específico (paginado)
        /// </summary>
        public async Task<Shared.Models.QueryModels.PagedResult<PermissionRoleDto>> GetPermissionRolesPagedAsync(PermissionRoleSearchRequest request, SessionDataDto sessionData)
        {
            _logger.LogInformation("Obteniendo roles con permiso {PermissionId} para Organization {OrganizationId}", 
                request.PermissionId, sessionData.OrganizationId);

            try
            {
                // Verificar que el permiso existe y es accesible
                var permission = await _dbSet
                    .Where(p => p.Id == request.PermissionId && 
                               (p.OrganizationId == null || p.OrganizationId == sessionData.OrganizationId))
                    .FirstOrDefaultAsync();

                if (permission == null)
                {
                    throw new InvalidOperationException("Permiso no encontrado o sin acceso");
                }

                // Query base de roles en la organización
                var rolesQuery = _context.Set<Shared.Models.Entities.SystemEntities.SystemRoles>()
                    .Where(r => r.OrganizationId == sessionData.OrganizationId);

                // Aplicar filtro de activos si se especifica
                if (request.ShowOnlyActive)
                {
                    rolesQuery = rolesQuery.Where(r => r.Active);
                }

                // Aplicar filtro de búsqueda si se especifica
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    rolesQuery = rolesQuery.Where(r => 
                        r.Nombre.ToLower().Contains(searchTerm) ||
                        (r.Descripcion != null && r.Descripcion.ToLower().Contains(searchTerm)));
                }

                // Obtener roles con información de permisos
                var rolesWithPermissions = await rolesQuery
                    .Select(r => new PermissionRoleDto
                    {
                        RoleId = r.Id,
                        Nombre = r.Nombre,
                        Descripcion = r.Descripcion,
                        Active = r.Active,
                        HasPermission = r.SystemRolesPermissions
                            .Any(rp => rp.SystemPermissionsId == request.PermissionId),
                        UsersCount = r.SystemUsersRoles.Count
                    })
                    .ToListAsync();

                // Aplicar filtro de solo con permiso
                if (request.ShowOnlyWithPermission)
                {
                    rolesWithPermissions = rolesWithPermissions
                        .Where(r => r.HasPermission)
                        .ToList();
                }

                // Aplicar paginación
                var totalCount = rolesWithPermissions.Count;
                var pagedRoles = rolesWithPermissions
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new Shared.Models.QueryModels.PagedResult<PermissionRoleDto>
                {
                    Data = pagedRoles,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener roles con permiso {PermissionId}", request.PermissionId);
                throw;
            }
        }
    }
}