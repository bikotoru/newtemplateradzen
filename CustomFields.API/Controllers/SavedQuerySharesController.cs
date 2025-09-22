using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Utils.Data;
using Shared.Models.Entities.SystemEntities;
using Backend.Utils.Security;
using Shared.Models.DTOs.Auth;
using Shared.Models.Responses;

namespace CustomFields.API.Controllers;

/// <summary>
/// API para gestión de compartidos de búsquedas guardadas
/// </summary>
[ApiController]
[Route("api/saved-queries/{savedQueryId}/shares")]
public class SavedQuerySharesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<SavedQuerySharesController> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly PermissionService _permissionService;

    public SavedQuerySharesController(
        AppDbContext context,
        ILogger<SavedQuerySharesController> logger,
        ICurrentUserService currentUserService,
        PermissionService permissionService)
    {
        _context = context;
        _logger = logger;
        _currentUserService = currentUserService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Obtener todos los compartidos de una búsqueda guardada
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<SavedQuerySharesListResponse>> GetShares(Guid savedQueryId)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new SavedQuerySharesListResponse { Success = false, Message = "Usuario no autenticado", Data = new List<SavedQueryShareDto>() });
            }

            _logger.LogInformation("Obteniendo compartidos para búsqueda {SavedQueryId}", savedQueryId);

            // Verificar que la búsqueda existe y el usuario tiene permisos
            var savedQuery = await _context.Set<SystemSavedQueries>()
                .Where(sq => sq.Id == savedQueryId && sq.Active)
                .FirstOrDefaultAsync();

            if (savedQuery == null)
            {
                return NotFound(new SavedQuerySharesListResponse
                {
                    Success = false,
                    Message = "Búsqueda guardada no encontrada",
                    Data = new List<SavedQueryShareDto>()
                });
            }

            // Solo el propietario puede ver los compartidos
            if (savedQuery.CreadorId != user.Id)
            {
                return StatusCode(403, new SavedQuerySharesListResponse { Success = false, Message = "Solo el propietario puede ver los compartidos", Data = new List<SavedQueryShareDto>() });
            }

            var shares = await _context.Set<SystemSavedQueryShares>()
                .Include(s => s.SavedQuery)
                .Where(s => s.SavedQueryId == savedQueryId && s.Active)
                .Select(s => new SavedQueryShareDto
                {
                    Id = s.Id,
                    SavedQueryId = s.SavedQueryId,
                    SharedWithUserId = s.SharedWithUserId,
                    SharedWithRoleId = s.SharedWithRoleId,
                    SharedWithOrganizationId = s.SharedWithOrganizationId,
                    CanView = s.CanView,
                    CanEdit = s.CanEdit,
                    CanExecute = s.CanExecute,
                    CanShare = s.CanShare,
                    FechaCreacion = s.FechaCreacion,
                    // TODO: Obtener nombres de usuario/rol/organización para display
                    SharedWithName = s.SharedWithUserId != null ? "Usuario específico" :
                                   s.SharedWithRoleId != null ? "Rol específico" :
                                   "Organización específica"
                })
                .ToListAsync();

            return Ok(new SavedQuerySharesListResponse
            {
                Success = true,
                Message = $"Se encontraron {shares.Count} compartidos",
                Data = shares
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo compartidos para búsqueda {SavedQueryId}", savedQueryId);
            return StatusCode(500, new SavedQuerySharesListResponse
            {
                Success = false,
                Message = $"Error obteniendo compartidos: {ex.Message}",
                Data = new List<SavedQueryShareDto>()
            });
        }
    }

    /// <summary>
    /// Compartir una búsqueda guardada con un usuario
    /// </summary>
    [HttpPost("users/{targetUserId}")]
    public async Task<ActionResult<SavedQueryShareResponse>> ShareWithUser(
        Guid savedQueryId,
        Guid targetUserId,
        [FromBody] CreateShareRequest request)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new SavedQueryShareResponse { Success = false, Message = "Usuario no autenticado", Data = null });
            }

            _logger.LogInformation("Compartiendo búsqueda {SavedQueryId} con usuario {TargetUserId}",
                savedQueryId, targetUserId);

            // Verificar que la búsqueda existe y el usuario tiene permisos para compartir
            var savedQuery = await _context.Set<SystemSavedQueries>()
                .Include(sq => sq.SystemSavedQueryShares)
                .Where(sq => sq.Id == savedQueryId && sq.Active)
                .FirstOrDefaultAsync();

            if (savedQuery == null)
            {
                return NotFound(new SavedQueryShareResponse
                {
                    Success = false,
                    Message = "Búsqueda guardada no encontrada",
                    Data = null
                });
            }

            var canShare = savedQuery.CreadorId == user.Id ||
                          savedQuery.SystemSavedQueryShares.Any(s =>
                              s.Active && s.SharedWithUserId == user.Id && s.CanShare);

            if (!canShare)
            {
                return StatusCode(403, new SavedQueryShareResponse { Success = false, Message = "No tienes permisos para esta operación", Data = null });
            }

            // Verificar que el usuario objetivo existe y pertenece a la misma organización
            var targetUser = await _context.Set<SystemUsers>()
                .Where(u => u.Id == targetUserId && u.Active && u.OrganizationId == user.OrganizationId)
                .FirstOrDefaultAsync();

            if (targetUser == null)
            {
                return BadRequest(new SavedQueryShareResponse
                {
                    Success = false,
                    Message = "Usuario objetivo no encontrado o no pertenece a la misma organización",
                    Data = null
                });
            }

            // Verificar que no exista un compartido existente
            var existingShare = await _context.Set<SystemSavedQueryShares>()
                .Where(s => s.SavedQueryId == savedQueryId &&
                           s.SharedWithUserId == targetUserId &&
                           s.Active)
                .FirstOrDefaultAsync();

            if (existingShare != null)
            {
                return BadRequest(new SavedQueryShareResponse
                {
                    Success = false,
                    Message = "La búsqueda ya está compartida con este usuario",
                    Data = null
                });
            }

            var share = new SystemSavedQueryShares
            {
                Id = Guid.NewGuid(),
                SavedQueryId = savedQueryId,
                SharedWithUserId = targetUserId,
                SharedWithRoleId = null,
                SharedWithOrganizationId = null,
                CanView = request.CanView,
                CanEdit = request.CanEdit,
                CanExecute = request.CanExecute,
                CanShare = request.CanShare,
                OrganizationId = user.OrganizationId,
                CreadorId = user.Id,
                ModificadorId = user.Id,
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow,
                Active = true
            };

            _context.Set<SystemSavedQueryShares>().Add(share);
            await _context.SaveChangesAsync();

            var shareDto = new SavedQueryShareDto
            {
                Id = share.Id,
                SavedQueryId = share.SavedQueryId,
                SharedWithUserId = share.SharedWithUserId,
                SharedWithRoleId = share.SharedWithRoleId,
                SharedWithOrganizationId = share.SharedWithOrganizationId,
                CanView = share.CanView,
                CanEdit = share.CanEdit,
                CanExecute = share.CanExecute,
                CanShare = share.CanShare,
                FechaCreacion = share.FechaCreacion,
                SharedWithName = targetUser.Nombre
            };

            return CreatedAtAction("GetShares", new { savedQueryId }, new SavedQueryShareResponse
            {
                Success = true,
                Message = "Búsqueda compartida exitosamente",
                Data = shareDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compartiendo búsqueda {SavedQueryId} con usuario {TargetUserId}",
                savedQueryId, targetUserId);
            return StatusCode(500, new SavedQueryShareResponse
            {
                Success = false,
                Message = $"Error compartiendo búsqueda: {ex.Message}",
                Data = null
            });
        }
    }

    /// <summary>
    /// Compartir una búsqueda guardada con un rol
    /// </summary>
    [HttpPost("roles/{targetRoleId}")]
    public async Task<ActionResult<SavedQueryShareResponse>> ShareWithRole(
        Guid savedQueryId,
        Guid targetRoleId,
        [FromBody] CreateShareRequest request)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new SavedQueryShareResponse { Success = false, Message = "Usuario no autenticado", Data = null });
            }

            _logger.LogInformation("Compartiendo búsqueda {SavedQueryId} con rol {TargetRoleId}",
                savedQueryId, targetRoleId);

            // Verificar que la búsqueda existe y el usuario tiene permisos para compartir
            var savedQuery = await _context.Set<SystemSavedQueries>()
                .Include(sq => sq.SystemSavedQueryShares)
                .Where(sq => sq.Id == savedQueryId && sq.Active)
                .FirstOrDefaultAsync();

            if (savedQuery == null)
            {
                return NotFound(new SavedQueryShareResponse
                {
                    Success = false,
                    Message = "Búsqueda guardada no encontrada",
                    Data = null
                });
            }

            var canShare = savedQuery.CreadorId == user.Id ||
                          savedQuery.SystemSavedQueryShares.Any(s =>
                              s.Active && s.SharedWithUserId == user.Id && s.CanShare);

            if (!canShare)
            {
                return StatusCode(403, new SavedQueryShareResponse { Success = false, Message = "No tienes permisos para esta operación", Data = null });
            }

            // Verificar que el rol existe y pertenece a la misma organización
            var targetRole = await _context.Set<SystemRoles>()
                .Where(r => r.Id == targetRoleId && r.Active && r.OrganizationId == user.OrganizationId)
                .FirstOrDefaultAsync();

            if (targetRole == null)
            {
                return BadRequest(new SavedQueryShareResponse
                {
                    Success = false,
                    Message = "Rol objetivo no encontrado o no pertenece a la misma organización",
                    Data = null
                });
            }

            // Verificar que no exista un compartido existente
            var existingShare = await _context.Set<SystemSavedQueryShares>()
                .Where(s => s.SavedQueryId == savedQueryId &&
                           s.SharedWithRoleId == targetRoleId &&
                           s.Active)
                .FirstOrDefaultAsync();

            if (existingShare != null)
            {
                return BadRequest(new SavedQueryShareResponse
                {
                    Success = false,
                    Message = "La búsqueda ya está compartida con este rol",
                    Data = null
                });
            }

            var share = new SystemSavedQueryShares
            {
                Id = Guid.NewGuid(),
                SavedQueryId = savedQueryId,
                SharedWithUserId = null,
                SharedWithRoleId = targetRoleId,
                SharedWithOrganizationId = null,
                CanView = request.CanView,
                CanEdit = request.CanEdit,
                CanExecute = request.CanExecute,
                CanShare = request.CanShare,
                OrganizationId = user.OrganizationId,
                CreadorId = user.Id,
                ModificadorId = user.Id,
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow,
                Active = true
            };

            _context.Set<SystemSavedQueryShares>().Add(share);
            await _context.SaveChangesAsync();

            var shareDto = new SavedQueryShareDto
            {
                Id = share.Id,
                SavedQueryId = share.SavedQueryId,
                SharedWithUserId = share.SharedWithUserId,
                SharedWithRoleId = share.SharedWithRoleId,
                SharedWithOrganizationId = share.SharedWithOrganizationId,
                CanView = share.CanView,
                CanEdit = share.CanEdit,
                CanExecute = share.CanExecute,
                CanShare = share.CanShare,
                FechaCreacion = share.FechaCreacion,
                SharedWithName = targetRole.Nombre
            };

            return CreatedAtAction("GetShares", new { savedQueryId }, new SavedQueryShareResponse
            {
                Success = true,
                Message = "Búsqueda compartida exitosamente",
                Data = shareDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compartiendo búsqueda {SavedQueryId} con rol {TargetRoleId}",
                savedQueryId, targetRoleId);
            return StatusCode(500, new SavedQueryShareResponse
            {
                Success = false,
                Message = $"Error compartiendo búsqueda: {ex.Message}",
                Data = null
            });
        }
    }

    /// <summary>
    /// Actualizar permisos de un compartido existente
    /// </summary>
    [HttpPut("{shareId}")]
    public async Task<ActionResult<SavedQueryShareResponse>> UpdateShare(
        Guid savedQueryId,
        Guid shareId,
        [FromBody] UpdateShareRequest request)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new SavedQueryShareResponse { Success = false, Message = "Usuario no autenticado", Data = null });
            }

            _logger.LogInformation("Actualizando compartido {ShareId} para búsqueda {SavedQueryId}",
                shareId, savedQueryId);

            var share = await _context.Set<SystemSavedQueryShares>()
                .Include(s => s.SavedQuery)
                .Where(s => s.Id == shareId &&
                           s.SavedQueryId == savedQueryId &&
                           s.Active)
                .FirstOrDefaultAsync();

            if (share == null)
            {
                return NotFound(new SavedQueryShareResponse
                {
                    Success = false,
                    Message = "Compartido no encontrado",
                    Data = null
                });
            }

            // Solo el propietario de la búsqueda puede actualizar los compartidos
            if (share.SavedQuery.CreadorId != user.Id)
            {
                return StatusCode(403, new SavedQueryShareResponse { Success = false, Message = "No tienes permisos para esta operación", Data = null });
            }

            share.CanView = request.CanView;
            share.CanEdit = request.CanEdit;
            share.CanExecute = request.CanExecute;
            share.CanShare = request.CanShare;
            share.ModificadorId = user.Id;
            share.FechaModificacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var shareDto = new SavedQueryShareDto
            {
                Id = share.Id,
                SavedQueryId = share.SavedQueryId,
                SharedWithUserId = share.SharedWithUserId,
                SharedWithRoleId = share.SharedWithRoleId,
                SharedWithOrganizationId = share.SharedWithOrganizationId,
                CanView = share.CanView,
                CanEdit = share.CanEdit,
                CanExecute = share.CanExecute,
                CanShare = share.CanShare,
                FechaCreacion = share.FechaCreacion,
                SharedWithName = "Actualizado"
            };

            return Ok(new SavedQueryShareResponse
            {
                Success = true,
                Message = "Permisos de compartido actualizados exitosamente",
                Data = shareDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando compartido {ShareId}", shareId);
            return StatusCode(500, new SavedQueryShareResponse
            {
                Success = false,
                Message = $"Error actualizando compartido: {ex.Message}",
                Data = null
            });
        }
    }

    /// <summary>
    /// Revocar un compartido
    /// </summary>
    [HttpDelete("{shareId}")]
    public async Task<ActionResult<SavedQueryShareResponse>> RevokeShare(Guid savedQueryId, Guid shareId)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new SavedQueryShareResponse { Success = false, Message = "Usuario no autenticado", Data = null });
            }

            _logger.LogInformation("Revocando compartido {ShareId} para búsqueda {SavedQueryId}",
                shareId, savedQueryId);

            var share = await _context.Set<SystemSavedQueryShares>()
                .Include(s => s.SavedQuery)
                .Where(s => s.Id == shareId &&
                           s.SavedQueryId == savedQueryId &&
                           s.Active)
                .FirstOrDefaultAsync();

            if (share == null)
            {
                return NotFound(new SavedQueryShareResponse
                {
                    Success = false,
                    Message = "Compartido no encontrado",
                    Data = null
                });
            }

            // Solo el propietario de la búsqueda puede revocar compartidos
            if (share.SavedQuery.CreadorId != user.Id)
            {
                return StatusCode(403, new SavedQueryShareResponse { Success = false, Message = "No tienes permisos para esta operación", Data = null });
            }

            // Soft delete
            share.Active = false;
            share.ModificadorId = user.Id;
            share.FechaModificacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new SavedQueryShareResponse
            {
                Success = true,
                Message = "Compartido revocado exitosamente",
                Data = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revocando compartido {ShareId}", shareId);
            return StatusCode(500, new SavedQueryShareResponse
            {
                Success = false,
                Message = $"Error revocando compartido: {ex.Message}",
                Data = null
            });
        }
    }

    /// <summary>
    /// Obtener usuarios disponibles para compartir
    /// </summary>
    [HttpGet("available-users")]
    public async Task<ActionResult<AvailableUsersResponse>> GetAvailableUsers(Guid savedQueryId)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new AvailableUsersResponse { Success = false, Message = "Usuario no autenticado", Data = new List<AvailableUserDto>() });
            }

            _logger.LogInformation("Obteniendo usuarios disponibles para compartir búsqueda {SavedQueryId}", savedQueryId);

            // Verificar que la búsqueda existe y el usuario tiene permisos
            var savedQuery = await _context.Set<SystemSavedQueries>()
                .Where(sq => sq.Id == savedQueryId && sq.Active && sq.CreadorId == user.Id)
                .FirstOrDefaultAsync();

            if (savedQuery == null)
            {
                return NotFound(new AvailableUsersResponse
                {
                    Success = false,
                    Message = "Búsqueda guardada no encontrada o sin permisos",
                    Data = new List<AvailableUserDto>()
                });
            }

            // Obtener usuarios ya compartidos
            var sharedUserIds = await _context.Set<SystemSavedQueryShares>()
                .Where(s => s.SavedQueryId == savedQueryId && s.Active && s.SharedWithUserId != null)
                .Select(s => s.SharedWithUserId)
                .ToListAsync();

            // Obtener usuarios disponibles (misma organización, activos, no el propietario, no ya compartidos)
            var availableUsers = await _context.Set<SystemUsers>()
                .Where(u => u.OrganizationId == user.OrganizationId &&
                           u.Active &&
                           u.Id != user.Id &&
                           !sharedUserIds.Contains(u.Id))
                .Select(u => new AvailableUserDto
                {
                    Id = u.Id,
                    Name = u.Nombre,
                    Email = u.Email
                })
                .OrderBy(u => u.Name)
                .ToListAsync();

            return Ok(new AvailableUsersResponse
            {
                Success = true,
                Message = $"Se encontraron {availableUsers.Count} usuarios disponibles",
                Data = availableUsers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo usuarios disponibles para búsqueda {SavedQueryId}", savedQueryId);
            return StatusCode(500, new AvailableUsersResponse
            {
                Success = false,
                Message = $"Error obteniendo usuarios disponibles: {ex.Message}",
                Data = new List<AvailableUserDto>()
            });
        }
    }

    /// <summary>
    /// Obtener roles disponibles para compartir
    /// </summary>
    [HttpGet("available-roles")]
    public async Task<ActionResult<AvailableRolesResponse>> GetAvailableRoles(Guid savedQueryId)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new AvailableRolesResponse { Success = false, Message = "Usuario no autenticado", Data = new List<AvailableRoleDto>() });
            }

            _logger.LogInformation("Obteniendo roles disponibles para compartir búsqueda {SavedQueryId}", savedQueryId);

            // Verificar que la búsqueda existe y el usuario tiene permisos
            var savedQuery = await _context.Set<SystemSavedQueries>()
                .Where(sq => sq.Id == savedQueryId && sq.Active && sq.CreadorId == user.Id)
                .FirstOrDefaultAsync();

            if (savedQuery == null)
            {
                return NotFound(new AvailableRolesResponse
                {
                    Success = false,
                    Message = "Búsqueda guardada no encontrada o sin permisos",
                    Data = new List<AvailableRoleDto>()
                });
            }

            // Obtener roles ya compartidos
            var sharedRoleIds = await _context.Set<SystemSavedQueryShares>()
                .Where(s => s.SavedQueryId == savedQueryId && s.Active && s.SharedWithRoleId != null)
                .Select(s => s.SharedWithRoleId)
                .ToListAsync();

            // Obtener roles disponibles (misma organización, activos, no ya compartidos)
            var availableRoles = await _context.Set<SystemRoles>()
                .Where(r => r.OrganizationId == user.OrganizationId &&
                           r.Active &&
                           !sharedRoleIds.Contains(r.Id))
                .Select(r => new AvailableRoleDto
                {
                    Id = r.Id,
                    Name = r.Nombre,
                    Description = r.Descripcion
                })
                .OrderBy(r => r.Name)
                .ToListAsync();

            return Ok(new AvailableRolesResponse
            {
                Success = true,
                Message = $"Se encontraron {availableRoles.Count} roles disponibles",
                Data = availableRoles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo roles disponibles para búsqueda {SavedQueryId}", savedQueryId);
            return StatusCode(500, new AvailableRolesResponse
            {
                Success = false,
                Message = $"Error obteniendo roles disponibles: {ex.Message}",
                Data = new List<AvailableRoleDto>()
            });
        }
    }

    /// <summary>
    /// Función ValidarUsuario para validación de usuario
    /// </summary>
    private async Task<SessionDataDto?> ValidarUsuario()
    {
        try
        {
            return await _permissionService.ValidateUserFromHeadersAsync(Request.Headers);
        }
        catch (SessionExpiredException ex)
        {
            _logger.LogWarning("Session expired: {ErrorCode}", ex.ErrorCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user");
            return null;
        }
    }
}

#region DTOs

/// <summary>
/// DTO para compartido de búsqueda guardada
/// </summary>
public class SavedQueryShareDto
{
    public Guid Id { get; set; }
    public Guid SavedQueryId { get; set; }
    public Guid? SharedWithUserId { get; set; }
    public Guid? SharedWithRoleId { get; set; }
    public Guid? SharedWithOrganizationId { get; set; }
    public bool CanView { get; set; }
    public bool CanEdit { get; set; }
    public bool CanExecute { get; set; }
    public bool CanShare { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string SharedWithName { get; set; } = "";
}

/// <summary>
/// Request para crear compartido
/// </summary>
public class CreateShareRequest
{
    public bool CanView { get; set; } = true;
    public bool CanEdit { get; set; } = false;
    public bool CanExecute { get; set; } = true;
    public bool CanShare { get; set; } = false;
}

/// <summary>
/// Request para actualizar compartido
/// </summary>
public class UpdateShareRequest
{
    public bool CanView { get; set; } = true;
    public bool CanEdit { get; set; } = false;
    public bool CanExecute { get; set; } = true;
    public bool CanShare { get; set; } = false;
}

/// <summary>
/// DTO para usuario disponible
/// </summary>
public class AvailableUserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

/// <summary>
/// DTO para rol disponible
/// </summary>
public class AvailableRoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

/// <summary>
/// Respuesta para compartido individual
/// </summary>
public class SavedQueryShareResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public SavedQueryShareDto? Data { get; set; }
}

/// <summary>
/// Respuesta para lista de compartidos
/// </summary>
public class SavedQuerySharesListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public List<SavedQueryShareDto> Data { get; set; } = new();
}

/// <summary>
/// Respuesta para usuarios disponibles
/// </summary>
public class AvailableUsersResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public List<AvailableUserDto> Data { get; set; } = new();
}

/// <summary>
/// Respuesta para roles disponibles
/// </summary>
public class AvailableRolesResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public List<AvailableRoleDto> Data { get; set; } = new();
}

#endregion