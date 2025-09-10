using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Shared.Models.DTOs.RolePermissions;
using Shared.Models.DTOs.RoleUsers;
using Shared.Models.DTOs.Auth;
using Shared.Models.QueryModels;

namespace Backend.Modules.Admin.SystemRoles
{
    public class SystemRoleService : BaseQueryService<Shared.Models.Entities.SystemEntities.SystemRoles>
    {
        private readonly AppDbContext _appContext;
        
        public SystemRoleService(AppDbContext context, ILogger<SystemRoleService> logger) 
            : base(context, logger)
        {
            _appContext = context;
        }

        /// <summary>
        /// Obtener permisos disponibles para un rol con paginación y búsqueda
        /// Incluye información de si están asignados o no al rol
        /// </summary>
        public async Task<PagedResult<RolePermissionDto>> GetRolePermissionsPagedAsync(RolePermissionSearchRequest request, SessionDataDto sessionData)
        {
            _logger.LogInformation("Obteniendo permisos para rol {RoleId}, página {Page}, búsqueda: '{SearchTerm}'", 
                request.RoleId, request.Page, request.SearchTerm);

            try
            {
                // Obtener todos los permisos disponibles (Global + Mi Organización)
                var permissionsQuery = _context.Set<Shared.Models.Entities.SystemEntities.SystemPermissions>()
                    .Where(p => p.OrganizationId == null || p.OrganizationId == sessionData.OrganizationId)
                    .Where(p => p.Active);

                // Obtener permisos actualmente asignados al rol
                var rolePermissionsIds = await _context.Set<Shared.Models.Entities.SystemEntities.SystemRolesPermissions>()
                    .Where(rp => rp.SystemRolesId == request.RoleId && rp.Active)
                    .Select(rp => rp.SystemPermissionsId)
                    .ToListAsync();

                // Aplicar filtro de búsqueda si se especifica
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    permissionsQuery = permissionsQuery.Where(p => 
                        p.Nombre.ToLower().Contains(searchTerm) ||
                        (p.Descripcion != null && p.Descripcion.ToLower().Contains(searchTerm)));
                }

                // Aplicar filtro por grupo si se especifica
                if (!string.IsNullOrWhiteSpace(request.GroupKey))
                {
                    permissionsQuery = permissionsQuery.Where(p => p.GroupKey == request.GroupKey);
                }

                // Convertir a DTOs con información de asignación
                var permissionDtosQuery = permissionsQuery.Select(p => new RolePermissionDto
                {
                    PermissionId = p.Id,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    GroupKey = p.GroupKey,
                    GrupoNombre = p.GrupoNombre,
                    ActionKey = p.ActionKey,
                    IsAssigned = rolePermissionsIds.Contains(p.Id)
                });

                // Aplicar filtro de "solo asignados" si está activado
                if (request.ShowOnlyAssigned)
                {
                    permissionDtosQuery = permissionDtosQuery.Where(dto => dto.IsAssigned);
                }

                // Ordenar por grupo y luego por nombre
                permissionDtosQuery = permissionDtosQuery
                    .OrderBy(dto => dto.GrupoNombre ?? "ZZZ")
                    .ThenBy(dto => dto.Nombre);

                // Contar total
                var totalCount = await permissionDtosQuery.CountAsync();

                // Aplicar paginación
                var skip = (request.Page - 1) * request.PageSize;
                var data = await permissionDtosQuery
                    .Skip(skip)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<RolePermissionDto>
                {
                    Data = data,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos del rol {RoleId}", request.RoleId);
                throw;
            }
        }

        /// <summary>
        /// Actualizar permisos de un rol (agregar/remover)
        /// </summary>
        public async Task<bool> UpdateRolePermissionsAsync(RolePermissionUpdateRequest request, SessionDataDto sessionData)
        {
            _logger.LogInformation("Actualizando permisos del rol {RoleId}, Agregar: {AddCount}, Remover: {RemoveCount}", 
                request.RoleId, request.PermissionsToAdd.Count, request.PermissionsToRemove.Count);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;

                // 1. Remover permisos
                if (request.PermissionsToRemove.Any())
                {
                    var rolePermissionsToRemove = await _context.Set<Shared.Models.Entities.SystemEntities.SystemRolesPermissions>()
                        .Where(rp => rp.SystemRolesId == request.RoleId && 
                                   request.PermissionsToRemove.Contains(rp.SystemPermissionsId) &&
                                   rp.Active)
                        .ToListAsync();

                    foreach (var rolePermission in rolePermissionsToRemove)
                    {
                        rolePermission.Active = false;
                        rolePermission.FechaModificacion = now;
                        rolePermission.ModificadorId = sessionData.Id;
                    }

                    _logger.LogInformation("Desactivados {Count} permisos del rol {RoleId}", 
                        rolePermissionsToRemove.Count, request.RoleId);
                }

                // 2. Agregar nuevos permisos
                if (request.PermissionsToAdd.Any())
                {
                    // Verificar si ya existen relaciones inactivas para reactivar
                    var existingInactive = await _context.Set<Shared.Models.Entities.SystemEntities.SystemRolesPermissions>()
                        .Where(rp => rp.SystemRolesId == request.RoleId && 
                                   request.PermissionsToAdd.Contains(rp.SystemPermissionsId) &&
                                   !rp.Active)
                        .ToListAsync();

                    // Reactivar relaciones existentes
                    foreach (var rolePermission in existingInactive)
                    {
                        rolePermission.Active = true;
                        rolePermission.FechaModificacion = now;
                        rolePermission.ModificadorId = sessionData.Id;
                    }

                    // Crear nuevas relaciones para permisos que no existían
                    var existingPermissionIds = existingInactive.Select(rp => rp.SystemPermissionsId).ToList();
                    var newPermissionIds = request.PermissionsToAdd.Except(existingPermissionIds).ToList();

                    var newRolePermissions = newPermissionIds.Select(permissionId => new Shared.Models.Entities.SystemEntities.SystemRolesPermissions
                    {
                        Id = Guid.NewGuid(),
                        SystemRolesId = request.RoleId,
                        SystemPermissionsId = permissionId,
                        FechaCreacion = now,
                        FechaModificacion = now,
                        OrganizationId = sessionData.OrganizationId,
                        CreadorId = sessionData.Id,
                        ModificadorId = sessionData.Id,
                        Active = true
                    }).ToList();

                    if (newRolePermissions.Any())
                    {
                        _context.Set<Shared.Models.Entities.SystemEntities.SystemRolesPermissions>().AddRange(newRolePermissions);
                    }

                    _logger.LogInformation("Agregados {NewCount} nuevos permisos y reactivados {ReactivatedCount} permisos al rol {RoleId}", 
                        newRolePermissions.Count, existingInactive.Count, request.RoleId);
                }

                await _appContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Permisos del rol {RoleId} actualizados exitosamente", request.RoleId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al actualizar permisos del rol {RoleId}", request.RoleId);
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
        public async Task<RolePermissionChangesSummary> GetChangesSummaryAsync(RolePermissionUpdateRequest request, SessionDataDto sessionData)
        {
            _logger.LogInformation("Generando resumen de cambios para rol {RoleId}", request.RoleId);

            // Obtener información del rol
            var role = await _dbSet.FirstOrDefaultAsync(r => r.Id == request.RoleId);
            if (role == null)
            {
                throw new ArgumentException($"No se encontró el rol con ID {request.RoleId}");
            }

            var summary = new RolePermissionChangesSummary
            {
                RoleName = role.Nombre
            };

            // Obtener permisos a agregar
            if (request.PermissionsToAdd.Any())
            {
                summary.PermissionsToAdd = await _context.Set<Shared.Models.Entities.SystemEntities.SystemPermissions>()
                    .Where(p => request.PermissionsToAdd.Contains(p.Id))
                    .Select(p => new RolePermissionDto
                    {
                        PermissionId = p.Id,
                        Nombre = p.Nombre,
                        Descripcion = p.Descripcion,
                        GroupKey = p.GroupKey,
                        GrupoNombre = p.GrupoNombre,
                        ActionKey = p.ActionKey,
                        IsAssigned = false
                    })
                    .ToListAsync();
            }

            // Obtener permisos a remover
            if (request.PermissionsToRemove.Any())
            {
                summary.PermissionsToRemove = await _context.Set<Shared.Models.Entities.SystemEntities.SystemPermissions>()
                    .Where(p => request.PermissionsToRemove.Contains(p.Id))
                    .Select(p => new RolePermissionDto
                    {
                        PermissionId = p.Id,
                        Nombre = p.Nombre,
                        Descripcion = p.Descripcion,
                        GroupKey = p.GroupKey,
                        GrupoNombre = p.GrupoNombre,
                        ActionKey = p.ActionKey,
                        IsAssigned = true
                    })
                    .ToListAsync();
            }

            return summary;
        }

        /// <summary>
        /// Obtener usuarios de un rol con paginación y filtros
        /// </summary>
        public async Task<PagedResult<RoleUserDto>> GetRoleUsersPagedAsync(RoleUserSearchRequest request, SessionDataDto sessionData)
        {
            try
            {
                var query = _appContext.SystemUsers.AsQueryable();

                // Filtrar por término de búsqueda si se proporciona
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    query = query.Where(u => u.Nombre.ToLower().Contains(searchTerm) ||
                                           (u.Email != null && u.Email.ToLower().Contains(searchTerm)));
                }

                // Si showOnlyAssigned es true, solo mostrar usuarios asignados al rol
                if (request.ShowOnlyAssigned)
                {
                    query = query.Where(u => u.SystemUsersRolesSystemUsers.Any(ru => ru.SystemRolesId == request.RoleId));
                }

                // Obtener usuarios con información de asignación
                var totalCount = await query.CountAsync();
                
                var users = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(u => new RoleUserDto
                    {
                        UserId = u.Id,
                        Nombre = u.Nombre,
                        Email = u.Email ?? string.Empty,
                        IsAssigned = u.SystemUsersRolesSystemUsers.Any(ru => ru.SystemRolesId == request.RoleId),
                        FechaCreacion = u.FechaCreacion,
                        Active = u.Active
                    })
                    .ToListAsync();

                return new PagedResult<RoleUserDto>
                {
                    Data = users,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios del rol {RoleId}", request.RoleId);
                throw;
            }
        }

        /// <summary>
        /// Asignar usuario a rol
        /// </summary>
        public async Task AssignUserToRoleAsync(AssignUserToRoleRequest request, SessionDataDto sessionData)
        {
            try
            {
                // Verificar si el rol existe
                var roleExists = await _appContext.SystemRoles
                    .AnyAsync(r => r.Id == request.RoleId);
                
                if (!roleExists)
                {
                    throw new ArgumentException("El rol especificado no existe");
                }

                // Verificar si el usuario existe
                var userExists = await _appContext.SystemUsers
                    .AnyAsync(u => u.Id == request.UserId);
                
                if (!userExists)
                {
                    throw new ArgumentException("El usuario especificado no existe");
                }

                // Verificar si ya existe la asignación
                var existingAssignment = await _appContext.SystemUsersRoles
                    .AnyAsync(ru => ru.SystemRolesId == request.RoleId && ru.SystemUsersId == request.UserId);

                if (existingAssignment)
                {
                    throw new InvalidOperationException("El usuario ya está asignado a este rol");
                }

                // Crear nueva asignación
                var roleUser = new Shared.Models.Entities.SystemEntities.SystemUsersRoles
                {
                    SystemRolesId = request.RoleId,
                    SystemUsersId = request.UserId,
                    FechaCreacion = DateTime.UtcNow,
                    CreadorId = sessionData.Id
                };

                _appContext.SystemUsersRoles.Add(roleUser);
                await _appContext.SaveChangesAsync();

                _logger.LogInformation("Usuario {UserId} asignado al rol {RoleId} por {CreatedBy}", 
                    request.UserId, request.RoleId, sessionData.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar usuario {UserId} al rol {RoleId}", request.UserId, request.RoleId);
                throw;
            }
        }

        /// <summary>
        /// Remover usuario de rol
        /// </summary>
        public async Task RemoveUserFromRoleAsync(RemoveUserFromRoleRequest request, SessionDataDto sessionData)
        {
            try
            {
                // Buscar la asignación existente
                var roleUser = await _appContext.SystemUsersRoles
                    .FirstOrDefaultAsync(ru => ru.SystemRolesId == request.RoleId && ru.SystemUsersId == request.UserId);

                if (roleUser == null)
                {
                    throw new ArgumentException("El usuario no está asignado a este rol");
                }

                // Remover la asignación
                _appContext.SystemUsersRoles.Remove(roleUser);
                await _appContext.SaveChangesAsync();

                _logger.LogInformation("Usuario {UserId} removido del rol {RoleId} por {RemovedBy}", 
                    request.UserId, request.RoleId, sessionData.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al remover usuario {UserId} del rol {RoleId}", request.UserId, request.RoleId);
                throw;
            }
        }
    }
}