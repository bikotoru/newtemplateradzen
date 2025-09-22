using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Utils.Data;
using Shared.Models.Entities.SystemEntities;
using System.Text.Json;
using Backend.Utils.Security;
using Shared.Models.DTOs.Auth;
using Shared.Models.Responses;

namespace CustomFields.API.Controllers;

/// <summary>
/// API para gestión de búsquedas avanzadas guardadas
/// </summary>
[ApiController]
[Route("api/saved-queries")]
public class SavedQueriesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<SavedQueriesController> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly PermissionService _permissionService;

    public SavedQueriesController(
        AppDbContext context,
        ILogger<SavedQueriesController> logger,
        ICurrentUserService currentUserService,
        PermissionService permissionService)
    {
        _context = context;
        _logger = logger;
        _currentUserService = currentUserService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Obtener todas las búsquedas guardadas del usuario actual
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<SavedQueriesListResponse>> GetSavedQueries(
        [FromQuery] string? entityName = null,
        [FromQuery] bool includePublic = true,
        [FromQuery] bool includeShared = true,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new SavedQueriesListResponse { Success = false, Message = "Usuario no autenticado", Data = new List<SavedQueryDto>(), TotalCount = 0 });
            }

            var hasPermission = user.Permisos.Contains("SAVEDQUERIES.VIEW");
            if (!hasPermission)
            {
                return StatusCode(403, new SavedQueriesListResponse { Success = false, Message = "No tienes permisos para ver búsquedas guardadas", Data = new List<SavedQueryDto>(), TotalCount = 0 });
            }
            var userId = user.Id;
            var organizationId = user.OrganizationId;

            _logger.LogInformation("Obteniendo búsquedas guardadas para usuario {UserId}, entidad: {EntityName}",
                userId, entityName ?? "todas");

            var query = _context.Set<SystemSavedQueries>()
                .Where(sq => sq.Active);

            // Filtrar por entidad si se especifica
            if (!string.IsNullOrEmpty(entityName))
            {
                query = query.Where(sq => sq.EntityName == entityName);
            }

            // Crear query para búsquedas del usuario
            var userQueries = query.Where(sq => sq.CreadorId == userId);

            // Crear query para búsquedas públicas
            var publicQueries = includePublic
                ? query.Where(sq => sq.IsPublic && sq.OrganizationId == organizationId)
                : query.Where(sq => false);

            // Crear query para búsquedas compartidas (si includeShared es true)
            var sharedQueries = includeShared
                ? query.Where(sq => sq.SystemSavedQueryShares.Any(share =>
                    share.Active &&
                    (share.SharedWithUserId == userId ||
                     share.SharedWithOrganizationId == organizationId)))
                : query.Where(sq => false);

            // Combinar todas las queries
            var combinedQuery = userQueries
                .Union(publicQueries)
                .Union(sharedQueries)
                .Distinct()
                .OrderByDescending(sq => sq.FechaCreacion);

            var totalCount = await combinedQuery.CountAsync();

            var savedQueries = await combinedQuery
                .Skip(skip)
                .Take(take)
                .Select(sq => new SavedQueryDto
                {
                    Id = sq.Id,
                    Name = sq.Name,
                    Description = sq.Description,
                    EntityName = sq.EntityName,
                    SelectedFields = sq.SelectedFields,
                    FilterConfiguration = sq.FilterConfiguration,
                    LogicalOperator = sq.LogicalOperator,
                    TakeLimit = sq.TakeLimit,
                    IsPublic = sq.IsPublic,
                    IsTemplate = sq.IsTemplate,
                    CreadorId = sq.CreadorId,
                    FechaCreacion = sq.FechaCreacion,
                    FechaModificacion = sq.FechaModificacion,
                    CanEdit = sq.CreadorId == userId,
                    CanShare = sq.CreadorId == userId,
                    SharedCount = sq.SystemSavedQueryShares.Count(s => s.Active)
                })
                .ToListAsync();

            return Ok(new SavedQueriesListResponse
            {
                Success = true,
                Message = $"Se encontraron {savedQueries.Count} búsquedas guardadas",
                Data = savedQueries,
                TotalCount = totalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo búsquedas guardadas");
            return StatusCode(500, new SavedQueriesListResponse
            {
                Success = false,
                Message = $"Error obteniendo búsquedas guardadas: {ex.Message}",
                Data = new List<SavedQueryDto>(),
                TotalCount = 0
            });
        }
    }

    /// <summary>
    /// Obtener una búsqueda guardada específica por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<SavedQueryResponse>> GetSavedQuery(Guid id)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new SavedQueryResponse { Success = false, Message = "Usuario no autenticado", Data = null });
            }

            var hasPermission = user.Permisos.Contains("SAVEDQUERIES.VIEW");
            if (!hasPermission)
            {
                return StatusCode(403, new SavedQueryResponse { Success = false, Message = "No tienes permisos para ver búsquedas guardadas", Data = null });
            }
            var userId = user.Id;
            var organizationId = user.OrganizationId;

            _logger.LogInformation("Obteniendo búsqueda guardada {SavedQueryId} para usuario {UserId}", id, userId);

            var savedQuery = await _context.Set<SystemSavedQueries>()
                .Include(sq => sq.SystemSavedQueryShares)
                .Where(sq => sq.Id == id && sq.Active)
                .Where(sq =>
                    sq.CreadorId == userId || // Es el propietario
                    sq.IsPublic || // Es pública
                    sq.SystemSavedQueryShares.Any(share =>
                        share.Active &&
                        share.CanView &&
                        (share.SharedWithUserId == userId ||
                         share.SharedWithOrganizationId == organizationId)))
                .FirstOrDefaultAsync();

            if (savedQuery == null)
            {
                return NotFound(new SavedQueryResponse
                {
                    Success = false,
                    Message = "Búsqueda guardada no encontrada o sin permisos para acceder",
                    Data = null
                });
            }

            var savedQueryDto = new SavedQueryDto
            {
                Id = savedQuery.Id,
                Name = savedQuery.Name,
                Description = savedQuery.Description,
                EntityName = savedQuery.EntityName,
                SelectedFields = savedQuery.SelectedFields,
                FilterConfiguration = savedQuery.FilterConfiguration,
                LogicalOperator = savedQuery.LogicalOperator,
                TakeLimit = savedQuery.TakeLimit,
                IsPublic = savedQuery.IsPublic,
                IsTemplate = savedQuery.IsTemplate,
                CreadorId = savedQuery.CreadorId,
                FechaCreacion = savedQuery.FechaCreacion,
                FechaModificacion = savedQuery.FechaModificacion,
                CanEdit = savedQuery.CreadorId == userId ||
                         savedQuery.SystemSavedQueryShares.Any(s =>
                             s.Active && s.SharedWithUserId == userId && s.CanEdit),
                CanShare = savedQuery.CreadorId == userId ||
                          savedQuery.SystemSavedQueryShares.Any(s =>
                              s.Active && s.SharedWithUserId == userId && s.CanShare),
                SharedCount = savedQuery.SystemSavedQueryShares.Count(s => s.Active)
            };

            return Ok(new SavedQueryResponse
            {
                Success = true,
                Message = "Búsqueda guardada obtenida exitosamente",
                Data = savedQueryDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo búsqueda guardada {SavedQueryId}", id);
            return StatusCode(500, new SavedQueryResponse
            {
                Success = false,
                Message = $"Error obteniendo búsqueda guardada: {ex.Message}",
                Data = null
            });
        }
    }

    /// <summary>
    /// Crear una nueva búsqueda guardada
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SavedQueryResponse>> CreateSavedQuery([FromBody] CreateSavedQueryRequest request)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new SavedQueryResponse { Success = false, Message = "Usuario no autenticado", Data = null });
            }

            var hasPermission = user.Permisos.Contains("SAVEDQUERIES.CREATE");
            if (!hasPermission)
            {
                return StatusCode(403, new SavedQueryResponse { Success = false, Message = "No tienes permisos para crear búsquedas guardadas", Data = null });
            }
            var userId = user.Id;
            var organizationId = user.OrganizationId;

            _logger.LogInformation("Creando búsqueda guardada: {Name} para entidad {EntityName}",
                request.Name, request.EntityName);

            // Validar que no exista una búsqueda con el mismo nombre para el usuario
            var existingQuery = await _context.Set<SystemSavedQueries>()
                .Where(sq => sq.Name == request.Name &&
                            sq.CreadorId == userId &&
                            sq.Active)
                .FirstOrDefaultAsync();

            if (existingQuery != null)
            {
                return BadRequest(new SavedQueryResponse
                {
                    Success = false,
                    Message = $"Ya tienes una búsqueda guardada con el nombre '{request.Name}'",
                    Data = null
                });
            }

            // Validar JSON
            if (!IsValidJson(request.SelectedFields))
            {
                return BadRequest(new SavedQueryResponse
                {
                    Success = false,
                    Message = "El campo SelectedFields debe ser un JSON válido",
                    Data = null
                });
            }

            if (!string.IsNullOrEmpty(request.FilterConfiguration) && !IsValidJson(request.FilterConfiguration))
            {
                return BadRequest(new SavedQueryResponse
                {
                    Success = false,
                    Message = "El campo FilterConfiguration debe ser un JSON válido",
                    Data = null
                });
            }

            var savedQuery = new SystemSavedQueries
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                EntityName = request.EntityName,
                SelectedFields = request.SelectedFields,
                FilterConfiguration = request.FilterConfiguration,
                LogicalOperator = request.LogicalOperator,
                TakeLimit = request.TakeLimit,
                IsPublic = request.IsPublic,
                IsTemplate = request.IsTemplate,
                OrganizationId = organizationId,
                CreadorId = userId,
                ModificadorId = userId,
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow,
                Active = true
            };

            _context.Set<SystemSavedQueries>().Add(savedQuery);
            await _context.SaveChangesAsync();

            var savedQueryDto = new SavedQueryDto
            {
                Id = savedQuery.Id,
                Name = savedQuery.Name,
                Description = savedQuery.Description,
                EntityName = savedQuery.EntityName,
                SelectedFields = savedQuery.SelectedFields,
                FilterConfiguration = savedQuery.FilterConfiguration,
                LogicalOperator = savedQuery.LogicalOperator,
                TakeLimit = savedQuery.TakeLimit,
                IsPublic = savedQuery.IsPublic,
                IsTemplate = savedQuery.IsTemplate,
                CreadorId = savedQuery.CreadorId,
                FechaCreacion = savedQuery.FechaCreacion,
                FechaModificacion = savedQuery.FechaModificacion,
                CanEdit = true,
                CanShare = true,
                SharedCount = 0
            };

            return CreatedAtAction(nameof(GetSavedQuery), new { id = savedQuery.Id }, new SavedQueryResponse
            {
                Success = true,
                Message = "Búsqueda guardada creada exitosamente",
                Data = savedQueryDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando búsqueda guardada");
            return StatusCode(500, new SavedQueryResponse
            {
                Success = false,
                Message = $"Error creando búsqueda guardada: {ex.Message}",
                Data = null
            });
        }
    }

    /// <summary>
    /// Actualizar una búsqueda guardada existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<SavedQueryResponse>> UpdateSavedQuery(Guid id, [FromBody] UpdateSavedQueryRequest request)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new SavedQueryResponse { Success = false, Message = "Usuario no autenticado", Data = null });
            }
            var userId = user.Id;

            _logger.LogInformation("Actualizando búsqueda guardada {SavedQueryId}", id);

            var savedQuery = await _context.Set<SystemSavedQueries>()
                .Include(sq => sq.SystemSavedQueryShares)
                .Where(sq => sq.Id == id && sq.Active)
                .FirstOrDefaultAsync();

            if (savedQuery == null)
            {
                return NotFound(new SavedQueryResponse
                {
                    Success = false,
                    Message = "Búsqueda guardada no encontrada",
                    Data = null
                });
            }

            // Verificar permisos de edición
            var canEdit = savedQuery.CreadorId == userId ||
                         savedQuery.SystemSavedQueryShares.Any(s =>
                             s.Active && s.SharedWithUserId == userId && s.CanEdit);

            if (!canEdit)
            {
                return Forbid();
            }

            // Validar JSON
            if (!IsValidJson(request.SelectedFields))
            {
                return BadRequest(new SavedQueryResponse
                {
                    Success = false,
                    Message = "El campo SelectedFields debe ser un JSON válido",
                    Data = null
                });
            }

            if (!string.IsNullOrEmpty(request.FilterConfiguration) && !IsValidJson(request.FilterConfiguration))
            {
                return BadRequest(new SavedQueryResponse
                {
                    Success = false,
                    Message = "El campo FilterConfiguration debe ser un JSON válido",
                    Data = null
                });
            }

            // Actualizar campos
            savedQuery.Name = request.Name;
            savedQuery.Description = request.Description;
            savedQuery.SelectedFields = request.SelectedFields;
            savedQuery.FilterConfiguration = request.FilterConfiguration;
            savedQuery.LogicalOperator = request.LogicalOperator;
            savedQuery.TakeLimit = request.TakeLimit;
            savedQuery.IsPublic = request.IsPublic;
            savedQuery.IsTemplate = request.IsTemplate;
            savedQuery.ModificadorId = userId;
            savedQuery.FechaModificacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var savedQueryDto = new SavedQueryDto
            {
                Id = savedQuery.Id,
                Name = savedQuery.Name,
                Description = savedQuery.Description,
                EntityName = savedQuery.EntityName,
                SelectedFields = savedQuery.SelectedFields,
                FilterConfiguration = savedQuery.FilterConfiguration,
                LogicalOperator = savedQuery.LogicalOperator,
                TakeLimit = savedQuery.TakeLimit,
                IsPublic = savedQuery.IsPublic,
                IsTemplate = savedQuery.IsTemplate,
                CreadorId = savedQuery.CreadorId,
                FechaCreacion = savedQuery.FechaCreacion,
                FechaModificacion = savedQuery.FechaModificacion,
                CanEdit = canEdit,
                CanShare = savedQuery.CreadorId == userId,
                SharedCount = savedQuery.SystemSavedQueryShares.Count(s => s.Active)
            };

            return Ok(new SavedQueryResponse
            {
                Success = true,
                Message = "Búsqueda guardada actualizada exitosamente",
                Data = savedQueryDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando búsqueda guardada {SavedQueryId}", id);
            return StatusCode(500, new SavedQueryResponse
            {
                Success = false,
                Message = $"Error actualizando búsqueda guardada: {ex.Message}",
                Data = null
            });
        }
    }

    /// <summary>
    /// Eliminar una búsqueda guardada
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<SavedQueryResponse>> DeleteSavedQuery(Guid id)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new SavedQueryResponse { Success = false, Message = "Usuario no autenticado", Data = null });
            }
            var userId = user.Id;

            _logger.LogInformation("Eliminando búsqueda guardada {SavedQueryId}", id);

            var savedQuery = await _context.Set<SystemSavedQueries>()
                .Where(sq => sq.Id == id && sq.Active)
                .FirstOrDefaultAsync();

            if (savedQuery == null)
            {
                return NotFound(new SavedQueryResponse
                {
                    Success = false,
                    Message = "Búsqueda guardada no encontrada",
                    Data = null
                });
            }

            // Solo el creador puede eliminar
            if (savedQuery.CreadorId != userId)
            {
                return Forbid();
            }

            // Soft delete
            savedQuery.Active = false;
            savedQuery.ModificadorId = userId;
            savedQuery.FechaModificacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new SavedQueryResponse
            {
                Success = true,
                Message = "Búsqueda guardada eliminada exitosamente",
                Data = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando búsqueda guardada {SavedQueryId}", id);
            return StatusCode(500, new SavedQueryResponse
            {
                Success = false,
                Message = $"Error eliminando búsqueda guardada: {ex.Message}",
                Data = null
            });
        }
    }

    /// <summary>
    /// Duplicar una búsqueda guardada
    /// </summary>
    [HttpPost("{id}/duplicate")]
    public async Task<ActionResult<SavedQueryResponse>> DuplicateSavedQuery(Guid id, [FromBody] DuplicateSavedQueryRequest request)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new SavedQueryResponse { Success = false, Message = "Usuario no autenticado", Data = null });
            }

            var hasPermission = user.Permisos.Contains("SAVEDQUERIES.CREATE");
            if (!hasPermission)
            {
                return StatusCode(403, new SavedQueryResponse { Success = false, Message = "No tienes permisos para crear búsquedas guardadas", Data = null });
            }
            var userId = user.Id;
            var organizationId = user.OrganizationId;

            _logger.LogInformation("Duplicando búsqueda guardada {SavedQueryId}", id);

            var originalQuery = await _context.Set<SystemSavedQueries>()
                .Where(sq => sq.Id == id && sq.Active)
                .FirstOrDefaultAsync();

            if (originalQuery == null)
            {
                return NotFound(new SavedQueryResponse
                {
                    Success = false,
                    Message = "Búsqueda guardada no encontrada",
                    Data = null
                });
            }

            // Verificar que el usuario tenga acceso a la búsqueda original
            var hasAccess = originalQuery.CreadorId == userId ||
                           originalQuery.IsPublic ||
                           await _context.Set<SystemSavedQueryShares>()
                               .AnyAsync(s => s.SavedQueryId == id &&
                                            s.Active &&
                                            s.CanView &&
                                            (s.SharedWithUserId == userId ||
                                             s.SharedWithOrganizationId == organizationId));

            if (!hasAccess)
            {
                return Forbid();
            }

            var duplicatedQuery = new SystemSavedQueries
            {
                Id = Guid.NewGuid(),
                Name = request.NewName,
                Description = originalQuery.Description,
                EntityName = originalQuery.EntityName,
                SelectedFields = originalQuery.SelectedFields,
                FilterConfiguration = originalQuery.FilterConfiguration,
                LogicalOperator = originalQuery.LogicalOperator,
                TakeLimit = originalQuery.TakeLimit,
                IsPublic = false, // Las copias siempre son privadas
                IsTemplate = false, // Las copias no son plantillas
                OrganizationId = organizationId,
                CreadorId = userId,
                ModificadorId = userId,
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow,
                Active = true
            };

            _context.Set<SystemSavedQueries>().Add(duplicatedQuery);
            await _context.SaveChangesAsync();

            var savedQueryDto = new SavedQueryDto
            {
                Id = duplicatedQuery.Id,
                Name = duplicatedQuery.Name,
                Description = duplicatedQuery.Description,
                EntityName = duplicatedQuery.EntityName,
                SelectedFields = duplicatedQuery.SelectedFields,
                FilterConfiguration = duplicatedQuery.FilterConfiguration,
                LogicalOperator = duplicatedQuery.LogicalOperator,
                TakeLimit = duplicatedQuery.TakeLimit,
                IsPublic = duplicatedQuery.IsPublic,
                IsTemplate = duplicatedQuery.IsTemplate,
                CreadorId = duplicatedQuery.CreadorId,
                FechaCreacion = duplicatedQuery.FechaCreacion,
                FechaModificacion = duplicatedQuery.FechaModificacion,
                CanEdit = true,
                CanShare = true,
                SharedCount = 0
            };

            return CreatedAtAction(nameof(GetSavedQuery), new { id = duplicatedQuery.Id }, new SavedQueryResponse
            {
                Success = true,
                Message = "Búsqueda guardada duplicada exitosamente",
                Data = savedQueryDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicando búsqueda guardada {SavedQueryId}", id);
            return StatusCode(500, new SavedQueryResponse
            {
                Success = false,
                Message = $"Error duplicando búsqueda guardada: {ex.Message}",
                Data = null
            });
        }
    }

    private static bool IsValidJson(string jsonString)
    {
        try
        {
            JsonDocument.Parse(jsonString);
            return true;
        }
        catch
        {
            return false;
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
/// DTO para búsqueda guardada
/// </summary>
public class SavedQueryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string EntityName { get; set; } = "";
    public string SelectedFields { get; set; } = "";
    public string? FilterConfiguration { get; set; }
    public byte LogicalOperator { get; set; }
    public int TakeLimit { get; set; }
    public bool IsPublic { get; set; }
    public bool IsTemplate { get; set; }
    public Guid? CreadorId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaModificacion { get; set; }
    public bool CanEdit { get; set; }
    public bool CanShare { get; set; }
    public int SharedCount { get; set; }
}

/// <summary>
/// Request para crear búsqueda guardada
/// </summary>
public class CreateSavedQueryRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string EntityName { get; set; } = "";
    public string SelectedFields { get; set; } = "";
    public string? FilterConfiguration { get; set; }
    public byte LogicalOperator { get; set; } = 0;
    public int TakeLimit { get; set; } = 50;
    public bool IsPublic { get; set; } = false;
    public bool IsTemplate { get; set; } = false;
}

/// <summary>
/// Request para actualizar búsqueda guardada
/// </summary>
public class UpdateSavedQueryRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string SelectedFields { get; set; } = "";
    public string? FilterConfiguration { get; set; }
    public byte LogicalOperator { get; set; } = 0;
    public int TakeLimit { get; set; } = 50;
    public bool IsPublic { get; set; } = false;
    public bool IsTemplate { get; set; } = false;
}

/// <summary>
/// Request para duplicar búsqueda guardada
/// </summary>
public class DuplicateSavedQueryRequest
{
    public string NewName { get; set; } = "";
}

/// <summary>
/// Respuesta para búsqueda guardada individual
/// </summary>
public class SavedQueryResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public SavedQueryDto? Data { get; set; }
}

/// <summary>
/// Respuesta para lista de búsquedas guardadas
/// </summary>
public class SavedQueriesListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public List<SavedQueryDto> Data { get; set; } = new();
    public int TotalCount { get; set; }
}

#endregion