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
    }
}