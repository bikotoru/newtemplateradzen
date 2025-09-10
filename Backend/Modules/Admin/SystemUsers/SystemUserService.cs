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

        #region Gestión de Permisos de Usuarios

        /// <summary>
        /// Obtener permisos disponibles para un usuario con paginación y búsqueda
        /// Incluye información de si están asignados directamente o heredados de roles
        /// </summary>
        public async Task<Shared.Models.QueryModels.PagedResult<UserPermissionDto>> GetUserPermissionsPagedAsync(UserPermissionSearchRequest request, SessionDataDto sessionData)
        {
            _logger.LogInformation("Obteniendo permisos para usuario {UserId}, filtro: '{Filter}'", 
                request.UserId, request.Filter);

            try
            {
                // Obtener todos los permisos disponibles (Global + Mi Organización)
                var permissionsQuery = _context.Set<Shared.Models.Entities.SystemEntities.SystemPermissions>()
                    .Where(p => p.OrganizationId == null || p.OrganizationId == sessionData.OrganizationId)
                    .Where(p => p.Active);

                // Obtener permisos directos del usuario
                var directUserPermissions = await _context.Set<Shared.Models.Entities.SystemEntities.SystemUsersPermissions>()
                    .Where(up => up.SystemUsersId == request.UserId && up.Active)
                    .Select(up => up.SystemPermissionsId)
                    .ToListAsync();

                // Verificar si el usuario tiene permiso SuperAdmin (directo o por roles)
                var hasSuperAdmin = false;
                var superAdminRoles = new List<string>();
                
                // Verificar SuperAdmin directo
                var directSuperAdmin = await _context.Set<Shared.Models.Entities.SystemEntities.SystemUsersPermissions>()
                    .Join(_context.Set<Shared.Models.Entities.SystemEntities.SystemPermissions>(),
                          up => up.SystemPermissionsId,
                          p => p.Id,
                          (up, p) => new { up, p })
                    .Where(x => x.up.SystemUsersId == request.UserId && 
                               x.up.Active && 
                               x.p.Active && 
                               x.p.Nombre == "SuperAdmin")
                    .AnyAsync();

                if (directSuperAdmin)
                {
                    hasSuperAdmin = true;
                    superAdminRoles.Add("Permiso Directo");
                }

                // Verificar SuperAdmin por roles
                var roleSuperAdmin = await (from userRole in _context.Set<Shared.Models.Entities.SystemEntities.SystemUsersRoles>()
                                           join role in _context.Set<Shared.Models.Entities.SystemEntities.SystemRoles>()
                                              on userRole.SystemRolesId equals role.Id
                                           join rolePermission in _context.Set<Shared.Models.Entities.SystemEntities.SystemRolesPermissions>() 
                                              on role.Id equals rolePermission.SystemRolesId
                                           join permission in _context.Set<Shared.Models.Entities.SystemEntities.SystemPermissions>()
                                              on rolePermission.SystemPermissionsId equals permission.Id
                                           where userRole.SystemUsersId == request.UserId && 
                                                 userRole.Active && 
                                                 role.Active && 
                                                 rolePermission.Active && 
                                                 permission.Active &&
                                                 permission.Nombre == "SuperAdmin"
                                           select role.Nombre)
                                          .ToListAsync();

                if (roleSuperAdmin.Any())
                {
                    hasSuperAdmin = true;
                    superAdminRoles.AddRange(roleSuperAdmin);
                }

                _logger.LogInformation("Usuario {UserId} - SuperAdmin: {HasSuperAdmin}, Roles: [{Roles}]", 
                    request.UserId, hasSuperAdmin, string.Join(", ", superAdminRoles));

                // Obtener permisos heredados de roles del usuario
                var userRolePermissions = await (from userRole in _context.Set<Shared.Models.Entities.SystemEntities.SystemUsersRoles>()
                                                 join rolePermission in _context.Set<Shared.Models.Entities.SystemEntities.SystemRolesPermissions>() 
                                                    on userRole.SystemRolesId equals rolePermission.SystemRolesId
                                                 join role in _context.Set<Shared.Models.Entities.SystemEntities.SystemRoles>()
                                                    on userRole.SystemRolesId equals role.Id
                                                 where userRole.SystemUsersId == request.UserId && 
                                                       userRole.Active && 
                                                       rolePermission.Active && 
                                                       role.Active
                                                 select new { rolePermission.SystemPermissionsId, RoleName = role.Nombre })
                                                .GroupBy(rp => rp.SystemPermissionsId)
                                                .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.RoleName).ToList());

                // Aplicar filtro de búsqueda si se especifica
                if (!string.IsNullOrWhiteSpace(request.Filter))
                {
                    // Simple filter parsing for Contains operation
                    if (request.Filter.Contains("Nombre.Contains("))
                    {
                        var searchTerm = request.Filter.Replace("Nombre.Contains(\"", "").Replace("\")", "").ToLower();
                        permissionsQuery = permissionsQuery.Where(p => 
                            p.Nombre.ToLower().Contains(searchTerm) ||
                            (p.Descripcion != null && p.Descripcion.ToLower().Contains(searchTerm)));
                    }
                }

                // Aplicar filtro por grupo si se especifica
                if (!string.IsNullOrWhiteSpace(request.GroupKey))
                {
                    permissionsQuery = permissionsQuery.Where(p => p.GroupKey == request.GroupKey);
                }

                // Obtener datos base para conversion
                var basePermissions = await permissionsQuery.Select(p => new
                {
                    PermissionId = p.Id,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    GroupKey = p.GroupKey,
                    GrupoNombre = p.GrupoNombre,
                    ActionKey = p.ActionKey
                }).ToListAsync();

                // Convertir a DTOs con información de asignación aplicando lógica de SuperAdmin
                var permissionDtos = basePermissions.Select(p => new UserPermissionDto
                {
                    PermissionId = p.PermissionId,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    GroupKey = p.GroupKey,
                    GrupoNombre = p.GrupoNombre,
                    ActionKey = p.ActionKey,
                    IsDirectlyAssigned = directUserPermissions.Contains(p.PermissionId),
                    // Si tiene SuperAdmin, todos los permisos (excepto SuperAdmin mismo) son heredados
                    IsInheritedFromRole = (hasSuperAdmin && p.Nombre != "SuperAdmin") || userRolePermissions.ContainsKey(p.PermissionId),
                    // Si tiene SuperAdmin, mostrar los roles que dan SuperAdmin para todos los permisos
                    InheritedFromRoles = (hasSuperAdmin && p.Nombre != "SuperAdmin") 
                        ? superAdminRoles 
                        : (userRolePermissions.ContainsKey(p.PermissionId) ? userRolePermissions[p.PermissionId] : new List<string>())
                }).ToList();

                // Aplicar filtros usando IEnumerable en lugar de IQueryable
                var filteredPermissions = permissionDtos.AsQueryable();

                // Aplicar filtros adicionales
                if (request.ShowOnlyDirectlyAssigned)
                {
                    filteredPermissions = filteredPermissions.Where(dto => dto.IsDirectlyAssigned);
                }
                else if (request.ShowOnlyUserPermissions)
                {
                    filteredPermissions = filteredPermissions.Where(dto => dto.HasPermission);
                }

                // Ordenar por grupo y luego por nombre
                filteredPermissions = filteredPermissions
                    .OrderBy(dto => dto.GrupoNombre ?? "ZZZ")
                    .ThenBy(dto => dto.Nombre);

                // Contar total
                var totalCount = filteredPermissions.Count();

                // Aplicar paginación
                var skip = request.Skip ?? 0;
                var take = request.Take ?? 20;
                var data = filteredPermissions
                    .Skip(skip)
                    .Take(take)
                    .ToList();

                var currentPage = take > 0 ? (skip / take) + 1 : 1;
                
                return new Shared.Models.QueryModels.PagedResult<UserPermissionDto>
                {
                    Data = data,
                    TotalCount = totalCount,
                    Page = currentPage,
                    PageSize = take
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos del usuario {UserId}", request.UserId);
                throw;
            }
        }

        /// <summary>
        /// Actualizar permisos directos de un usuario (agregar/remover)
        /// </summary>
        public async Task<bool> UpdateUserPermissionsAsync(UserPermissionUpdateRequest request, SessionDataDto sessionData)
        {
            _logger.LogInformation("Actualizando permisos del usuario {UserId}, Agregar: {AddCount}, Remover: {RemoveCount}", 
                request.UserId, request.PermissionsToAdd.Count, request.PermissionsToRemove.Count);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;

                // 1. Remover permisos directos
                if (request.PermissionsToRemove.Any())
                {
                    var userPermissionsToRemove = await _context.Set<Shared.Models.Entities.SystemEntities.SystemUsersPermissions>()
                        .Where(up => up.SystemUsersId == request.UserId && 
                                   request.PermissionsToRemove.Contains(up.SystemPermissionsId) &&
                                   up.Active)
                        .ToListAsync();

                    foreach (var userPermission in userPermissionsToRemove)
                    {
                        userPermission.Active = false;
                        userPermission.FechaModificacion = now;
                        userPermission.ModificadorId = sessionData.Id;
                    }

                    _logger.LogInformation("Desactivados {Count} permisos directos del usuario {UserId}", 
                        userPermissionsToRemove.Count, request.UserId);
                }

                // 2. Agregar nuevos permisos directos
                if (request.PermissionsToAdd.Any())
                {
                    // Verificar si ya existen relaciones inactivas para reactivar
                    var existingInactive = await _context.Set<Shared.Models.Entities.SystemEntities.SystemUsersPermissions>()
                        .Where(up => up.SystemUsersId == request.UserId && 
                                   request.PermissionsToAdd.Contains(up.SystemPermissionsId) &&
                                   !up.Active)
                        .ToListAsync();

                    // Reactivar relaciones existentes
                    foreach (var userPermission in existingInactive)
                    {
                        userPermission.Active = true;
                        userPermission.FechaModificacion = now;
                        userPermission.ModificadorId = sessionData.Id;
                    }

                    // Crear nuevas relaciones para permisos que no existían
                    var existingPermissionIds = existingInactive.Select(up => up.SystemPermissionsId).ToList();
                    var newPermissionIds = request.PermissionsToAdd.Except(existingPermissionIds).ToList();

                    var newUserPermissions = newPermissionIds.Select(permissionId => new Shared.Models.Entities.SystemEntities.SystemUsersPermissions
                    {
                        Id = Guid.NewGuid(),
                        SystemUsersId = request.UserId,
                        SystemPermissionsId = permissionId,
                        FechaCreacion = now,
                        FechaModificacion = now,
                        OrganizationId = sessionData.OrganizationId,
                        CreadorId = sessionData.Id,
                        ModificadorId = sessionData.Id,
                        Active = true
                    }).ToList();

                    if (newUserPermissions.Any())
                    {
                        _context.Set<Shared.Models.Entities.SystemEntities.SystemUsersPermissions>().AddRange(newUserPermissions);
                    }

                    _logger.LogInformation("Agregados {NewCount} nuevos permisos directos y reactivados {ReactivatedCount} permisos al usuario {UserId}", 
                        newUserPermissions.Count, existingInactive.Count, request.UserId);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Permisos directos del usuario {UserId} actualizados exitosamente", request.UserId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al actualizar permisos directos del usuario {UserId}", request.UserId);
                throw;
            }
        }

        /// <summary>
        /// Obtener grupos de permisos disponibles
        /// </summary>
        public async Task<List<string>> GetAvailablePermissionGroupsAsync(SessionDataDto sessionData)
        {
            _logger.LogInformation("Obteniendo grupos de permisos disponibles para Organization {OrganizationId}", sessionData.OrganizationId);

            var groups = await _context.Set<Shared.Models.Entities.SystemEntities.SystemPermissions>()
                .Where(p => (p.OrganizationId == null || p.OrganizationId == sessionData.OrganizationId) &&
                           p.Active &&
                           !string.IsNullOrEmpty(p.GrupoNombre))
                .Select(p => p.GrupoNombre!)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            return groups;
        }

        /// <summary>
        /// Generar resumen de cambios antes de aplicar
        /// </summary>
        public async Task<UserPermissionChangesSummary> GetChangesSummaryAsync(UserPermissionUpdateRequest request, SessionDataDto sessionData)
        {
            _logger.LogInformation("Generando resumen de cambios para usuario {UserId}", request.UserId);

            // Obtener información del usuario
            var user = await _context.Set<Shared.Models.Entities.SystemEntities.SystemUsers>()
                .FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user == null)
            {
                throw new ArgumentException($"No se encontró el usuario con ID {request.UserId}");
            }

            var summary = new UserPermissionChangesSummary
            {
                UserName = user.Nombre,
                PermissionsToAdd = new List<UserPermissionDto>(),
                PermissionsToRemove = new List<UserPermissionDto>()
            };

            // Obtener permisos a agregar
            if (request.PermissionsToAdd.Any())
            {
                summary.PermissionsToAdd = await _context.Set<Shared.Models.Entities.SystemEntities.SystemPermissions>()
                    .Where(p => request.PermissionsToAdd.Contains(p.Id))
                    .Select(p => new UserPermissionDto
                    {
                        PermissionId = p.Id,
                        Nombre = p.Nombre,
                        Descripcion = p.Descripcion,
                        GroupKey = p.GroupKey,
                        GrupoNombre = p.GrupoNombre,
                        ActionKey = p.ActionKey,
                        IsDirectlyAssigned = false
                    })
                    .ToListAsync();
            }

            // Obtener permisos a remover
            if (request.PermissionsToRemove.Any())
            {
                summary.PermissionsToRemove = await _context.Set<Shared.Models.Entities.SystemEntities.SystemPermissions>()
                    .Where(p => request.PermissionsToRemove.Contains(p.Id))
                    .Select(p => new UserPermissionDto
                    {
                        PermissionId = p.Id,
                        Nombre = p.Nombre,
                        Descripcion = p.Descripcion,
                        GroupKey = p.GroupKey,
                        GrupoNombre = p.GrupoNombre,
                        ActionKey = p.ActionKey,
                        IsDirectlyAssigned = true
                    })
                    .ToListAsync();
            }

            return summary;
        }

        #endregion

        #region Gestión de Roles de Usuarios

        /// <summary>
        /// Obtener roles asignados a un usuario
        /// </summary>
        public async Task<Shared.Models.QueryModels.PagedResult<Shared.Models.DTOs.UserRoles.UserRoleDto>> GetUserRolesPagedAsync(Shared.Models.DTOs.UserRoles.UserRoleSearchRequest request, SessionDataDto sessionData)
        {
            _logger.LogInformation("Obteniendo roles para usuario {UserId}", request.UserId);

            try
            {
                var userRolesQuery = from userRole in _context.Set<Shared.Models.Entities.SystemEntities.SystemUsersRoles>()
                                    join role in _context.Set<Shared.Models.Entities.SystemEntities.SystemRoles>()
                                        on userRole.SystemRolesId equals role.Id
                                    where userRole.SystemUsersId == request.UserId &&
                                          (role.OrganizationId == null || role.OrganizationId == sessionData.OrganizationId) &&
                                          (!request.ShowOnlyActive || (userRole.Active && role.Active))
                                    select new { userRole, role };

                // Aplicar filtro de búsqueda si se especifica
                if (!string.IsNullOrWhiteSpace(request.Filter))
                {
                    if (request.Filter.Contains("Nombre.Contains("))
                    {
                        var searchTerm = request.Filter.Replace("Nombre.Contains(\"", "").Replace("\")", "").ToLower();
                        userRolesQuery = userRolesQuery.Where(x => 
                            x.role.Nombre.ToLower().Contains(searchTerm) ||
                            (x.role.Descripcion != null && x.role.Descripcion.ToLower().Contains(searchTerm)));
                    }
                }

                var totalCount = await userRolesQuery.CountAsync();

                // Aplicar paginación
                var skip = request.Skip ?? 0;
                var take = request.Take ?? 20;
                
                var userRolesData = await userRolesQuery
                    .OrderBy(x => x.role.Nombre)
                    .Skip(skip)
                    .Take(take)
                    .Select(x => new 
                    {
                        x.role.Id,
                        x.role.Nombre,
                        x.role.Descripcion,
                        x.role.FechaCreacion,
                        CreadorNombre = x.role.Creador != null ? x.role.Creador.Nombre : "",
                        x.role.Active,
                        FechaAsignacion = x.userRole.FechaCreacion,
                        AsignadoPor = x.userRole.Creador != null ? x.userRole.Creador.Nombre : ""
                    })
                    .ToListAsync();

                // Obtener permisos para cada rol
                var roleIds = userRolesData.Select(x => x.Id).ToList();
                var rolePermissions = await _context.Set<Shared.Models.Entities.SystemEntities.SystemRolesPermissions>()
                    .Where(rp => roleIds.Contains(rp.SystemRolesId) && rp.Active)
                    .Include(rp => rp.SystemPermissions)
                    .Where(rp => rp.SystemPermissions.Active)
                    .GroupBy(rp => rp.SystemRolesId)
                    .ToDictionaryAsync(
                        g => g.Key,
                        g => g.Select(rp => rp.SystemPermissions.Nombre).ToList()
                    );

                // Crear DTOs
                var data = userRolesData.Select(x => new Shared.Models.DTOs.UserRoles.UserRoleDto
                {
                    RoleId = x.Id,
                    Nombre = x.Nombre,
                    Descripcion = x.Descripcion,
                    FechaCreacion = x.FechaCreacion,
                    CreadoPor = x.CreadorNombre,
                    Active = x.Active,
                    FechaAsignacion = x.FechaAsignacion,
                    AsignadoPor = x.AsignadoPor,
                    CantidadPermisos = rolePermissions.ContainsKey(x.Id) ? rolePermissions[x.Id].Count : 0,
                    Permisos = rolePermissions.ContainsKey(x.Id) ? rolePermissions[x.Id] : new List<string>()
                }).ToList();

                var currentPage = take > 0 ? (skip / take) + 1 : 1;

                return new Shared.Models.QueryModels.PagedResult<Shared.Models.DTOs.UserRoles.UserRoleDto>
                {
                    Data = data,
                    TotalCount = totalCount,
                    Page = currentPage,
                    PageSize = take
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener roles del usuario {UserId}", request.UserId);
                throw;
            }
        }

        /// <summary>
        /// Obtener roles disponibles para asignar a un usuario
        /// </summary>
        public async Task<List<Shared.Models.DTOs.UserRoles.AvailableRoleDto>> GetAvailableRolesAsync(Guid userId, SessionDataDto sessionData, string? searchTerm = null)
        {
            _logger.LogInformation("Obteniendo roles disponibles para usuario {UserId}", userId);

            try
            {
                // Obtener roles ya asignados al usuario
                var assignedRoleIds = await _context.Set<Shared.Models.Entities.SystemEntities.SystemUsersRoles>()
                    .Where(ur => ur.SystemUsersId == userId && ur.Active)
                    .Select(ur => ur.SystemRolesId)
                    .ToListAsync();

                // Obtener todos los roles disponibles
                var rolesQuery = _context.Set<Shared.Models.Entities.SystemEntities.SystemRoles>()
                    .Where(r => (r.OrganizationId == null || r.OrganizationId == sessionData.OrganizationId) && r.Active);

                // Aplicar filtro de búsqueda si se especifica
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    rolesQuery = rolesQuery.Where(r => 
                        r.Nombre.ToLower().Contains(searchTerm.ToLower()) ||
                        (r.Descripcion != null && r.Descripcion.ToLower().Contains(searchTerm.ToLower())));
                }

                var roles = await rolesQuery
                    .OrderBy(r => r.Nombre)
                    .Select(r => new 
                    {
                        r.Id,
                        r.Nombre,
                        r.Descripcion,
                        r.FechaCreacion,
                        r.Active
                    })
                    .ToListAsync();

                // Obtener permisos para cada rol
                var roleIds = roles.Select(r => r.Id).ToList();
                var rolePermissions = await _context.Set<Shared.Models.Entities.SystemEntities.SystemRolesPermissions>()
                    .Where(rp => roleIds.Contains(rp.SystemRolesId) && rp.Active)
                    .Include(rp => rp.SystemPermissions)
                    .Where(rp => rp.SystemPermissions.Active)
                    .GroupBy(rp => rp.SystemRolesId)
                    .ToDictionaryAsync(
                        g => g.Key,
                        g => new 
                        {
                            Nombres = g.Select(rp => rp.SystemPermissions.Nombre).ToList(),
                            Acciones = g.Select(rp => rp.SystemPermissions.ActionKey).Where(a => !string.IsNullOrEmpty(a)).ToList()
                        }
                    );

                // Crear DTOs
                var availableRoles = roles.Select(r => new Shared.Models.DTOs.UserRoles.AvailableRoleDto
                {
                    RoleId = r.Id,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    FechaCreacion = r.FechaCreacion,
                    Active = r.Active,
                    YaAsignado = assignedRoleIds.Contains(r.Id),
                    CantidadPermisos = rolePermissions.ContainsKey(r.Id) ? rolePermissions[r.Id].Nombres.Count : 0,
                    PermisosNombres = rolePermissions.ContainsKey(r.Id) ? rolePermissions[r.Id].Nombres : new List<string>(),
                    PermisosAcciones = rolePermissions.ContainsKey(r.Id) ? rolePermissions[r.Id].Acciones : new List<string>()
                }).ToList();

                return availableRoles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener roles disponibles para usuario {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Asignar rol a un usuario
        /// </summary>
        public async Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId, SessionDataDto sessionData)
        {
            _logger.LogInformation("Asignando rol {RoleId} al usuario {UserId}", roleId, userId);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Verificar si ya existe la asignación
                var existingAssignment = await _context.Set<Shared.Models.Entities.SystemEntities.SystemUsersRoles>()
                    .FirstOrDefaultAsync(ur => ur.SystemUsersId == userId && ur.SystemRolesId == roleId);

                if (existingAssignment != null)
                {
                    if (existingAssignment.Active)
                    {
                        _logger.LogWarning("El rol {RoleId} ya está asignado al usuario {UserId}", roleId, userId);
                        return true; // Ya está asignado y activo
                    }
                    else
                    {
                        // Reactivar asignación existente
                        existingAssignment.Active = true;
                        existingAssignment.FechaModificacion = DateTime.UtcNow;
                        existingAssignment.ModificadorId = sessionData.Id;
                    }
                }
                else
                {
                    // Crear nueva asignación
                    var userRole = new Shared.Models.Entities.SystemEntities.SystemUsersRoles
                    {
                        Id = Guid.NewGuid(),
                        SystemUsersId = userId,
                        SystemRolesId = roleId,
                        OrganizationId = sessionData.OrganizationId,
                        Active = true,
                        FechaCreacion = DateTime.UtcNow,
                        CreadorId = sessionData.Id
                    };

                    _context.Set<Shared.Models.Entities.SystemEntities.SystemUsersRoles>().Add(userRole);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Rol {RoleId} asignado exitosamente al usuario {UserId}", roleId, userId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al asignar rol {RoleId} al usuario {UserId}", roleId, userId);
                throw;
            }
        }

        /// <summary>
        /// Remover rol de un usuario
        /// </summary>
        public async Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid roleId, SessionDataDto sessionData)
        {
            _logger.LogInformation("Removiendo rol {RoleId} del usuario {UserId}", roleId, userId);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userRole = await _context.Set<Shared.Models.Entities.SystemEntities.SystemUsersRoles>()
                    .FirstOrDefaultAsync(ur => ur.SystemUsersId == userId && ur.SystemRolesId == roleId && ur.Active);

                if (userRole == null)
                {
                    _logger.LogWarning("No se encontró asignación activa del rol {RoleId} para el usuario {UserId}", roleId, userId);
                    return false;
                }

                // Desactivar la asignación
                userRole.Active = false;
                userRole.FechaModificacion = DateTime.UtcNow;
                userRole.ModificadorId = sessionData.Id;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Rol {RoleId} removido exitosamente del usuario {UserId}", roleId, userId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al remover rol {RoleId} del usuario {UserId}", roleId, userId);
                throw;
            }
        }

        #endregion
    }
}