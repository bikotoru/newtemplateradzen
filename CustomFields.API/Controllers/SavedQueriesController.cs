using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Utils.Data;
using Shared.Models.Entities.SystemEntities;
using System.Text.Json;
using Backend.Utils.Security;
using Shared.Models.DTOs.Auth;
using Shared.Models.Responses;
using Shared.Models.Requests;
using Shared.Models.QueryModels;

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
    /// Obtener todas las búsquedas guardadas del usuario actual (compatible con BaseApiService)
    /// </summary>
    [HttpGet("all")]
    public async Task<ActionResult<ApiResponse<PagedResponse<SavedQueryDto>>>> GetAllPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool all = false,
        [FromQuery] string? entityName = null,
        [FromQuery] bool includePublic = true,
        [FromQuery] bool includeShared = true)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(ApiResponse<PagedResponse<SavedQueryDto>>.ErrorResponse("Usuario no autenticado"));
            }

            var hasPermission = user.Permisos.Contains("SAVEDQUERIES.VIEW");
            if (!hasPermission)
            {
                return StatusCode(403, ApiResponse<PagedResponse<SavedQueryDto>>.ErrorResponse("No tienes permisos para ver búsquedas guardadas"));
            }

            var userId = user.Id;
            var organizationId = user.OrganizationId;

            _logger.LogInformation("Obteniendo búsquedas guardadas para usuario {UserId}, página: {Page}, tamaño: {PageSize}",
                userId, page, pageSize);

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

            // Crear query para búsquedas compartidas
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

            List<SavedQueryDto> savedQueries;
            
            if (all)
            {
                savedQueries = await combinedQuery
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
            }
            else
            {
                var skip = (page - 1) * pageSize;
                savedQueries = await combinedQuery
                    .Skip(skip)
                    .Take(pageSize)
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
            }

            var pagedData = PagedResponse<SavedQueryDto>.Create(savedQueries, page, pageSize, totalCount);
            return Ok(ApiResponse<PagedResponse<SavedQueryDto>>.SuccessResponse(pagedData, $"Se encontraron {savedQueries.Count} búsquedas guardadas"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo búsquedas guardadas");
            return StatusCode(500, ApiResponse<PagedResponse<SavedQueryDto>>.ErrorResponse($"Error obteniendo búsquedas guardadas: {ex.Message}"));
        }
    }

    /// <summary>
    /// Obtener todas las búsquedas guardadas del usuario actual (método original)
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
    /// Ejecutar query dinámica con paginación (compatible con BaseApiService)
    /// </summary>
    [HttpPost("paged")]
    public async Task<ActionResult<ApiResponse<PagedResult<SavedQueryDto>>>> QueryPaged([FromBody] QueryRequest queryRequest)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(ApiResponse<PagedResult<SavedQueryDto>>.ErrorResponse("Usuario no autenticado"));
            }

            var hasPermission = user.Permisos.Contains("SAVEDQUERIES.VIEW");
            if (!hasPermission)
            {
                return StatusCode(403, ApiResponse<PagedResult<SavedQueryDto>>.ErrorResponse("No tienes permisos para ver búsquedas guardadas"));
            }

            var userId = user.Id;
            var organizationId = user.OrganizationId;

            _logger.LogInformation("Ejecutando query paginada para SavedQueries del usuario {UserId}", userId);

            // Crear la query base con filtros de seguridad
            var query = _context.Set<SystemSavedQueries>()
                .Where(sq => sq.Active);

            // Aplicar filtros de acceso del usuario
            var userQueries = query.Where(sq => sq.CreadorId == userId);
            var publicQueries = query.Where(sq => sq.IsPublic && sq.OrganizationId == organizationId);
            var sharedQueries = query.Where(sq => sq.SystemSavedQueryShares.Any(share =>
                share.Active &&
                (share.SharedWithUserId == userId ||
                 share.SharedWithOrganizationId == organizationId)));

            var combinedQuery = userQueries
                .Union(publicQueries)
                .Union(sharedQueries)
                .Distinct();

            // Aplicar filtros dinámicos del QueryRequest si existen
            // Nota: Aquí podrías implementar la lógica de filtros dinámicos
            // Por ahora solo ordenamos por fecha de creación
            var finalQuery = combinedQuery.OrderByDescending(sq => sq.FechaCreacion);

            // Calcular paginación
            var totalCount = await finalQuery.CountAsync();
            var skip = queryRequest.Skip ?? 0;
            var take = queryRequest.Take ?? 10;
            var page = (skip / take) + 1;

            var savedQueries = await finalQuery
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

            var pagedResult = new PagedResult<SavedQueryDto>
            {
                Data = savedQueries,
                Page = page,
                PageSize = take,
                TotalCount = totalCount
            };

            return Ok(ApiResponse<PagedResult<SavedQueryDto>>.SuccessResponse(pagedResult, $"Query paginada ejecutada exitosamente"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando query paginada para SavedQueries");
            return StatusCode(500, ApiResponse<PagedResult<SavedQueryDto>>.ErrorResponse($"Error ejecutando query paginada: {ex.Message}"));
        }
    }

    /// <summary>
    /// Crear una nueva búsqueda guardada (compatible con BaseApiService)
    /// </summary>
    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<SavedQueryDto>>> Create([FromBody] CreateRequest<SavedQueryDto> request)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new ApiResponse<SavedQueryDto> { Success = false, Message = "Usuario no autenticado", Data = null });
            }

            var hasPermission = user.Permisos.Contains("SAVEDQUERIES.CREATE");
            if (!hasPermission)
            {
                return StatusCode(403, new ApiResponse<SavedQueryDto> { Success = false, Message = "No tienes permisos para crear búsquedas guardadas", Data = null });
            }

            if (request?.Entity == null)
            {
                return BadRequest(new ApiResponse<SavedQueryDto> { Success = false, Message = "Datos de la búsqueda guardada requeridos", Data = null });
            }

            var userId = user.Id;
            var organizationId = user.OrganizationId;

            _logger.LogInformation("Creando búsqueda guardada: {Name} para entidad {EntityName}",
                request.Entity.Name, request.Entity.EntityName);

            // Validar que no exista una búsqueda con el mismo nombre para el usuario
            var existingQuery = await _context.Set<SystemSavedQueries>()
                .Where(sq => sq.Name == request.Entity.Name &&
                            sq.CreadorId == userId &&
                            sq.Active)
                .FirstOrDefaultAsync();

            if (existingQuery != null)
            {
                return BadRequest(new ApiResponse<SavedQueryDto>
                {
                    Success = false,
                    Message = $"Ya tienes una búsqueda guardada con el nombre '{request.Entity.Name}'",
                    Data = null
                });
            }

            // Validar JSON
            if (!IsValidJson(request.Entity.SelectedFields))
            {
                return BadRequest(new ApiResponse<SavedQueryDto>
                {
                    Success = false,
                    Message = "El campo SelectedFields debe ser un JSON válido",
                    Data = null
                });
            }

            if (!string.IsNullOrEmpty(request.Entity.FilterConfiguration) && !IsValidJson(request.Entity.FilterConfiguration))
            {
                return BadRequest(new ApiResponse<SavedQueryDto>
                {
                    Success = false,
                    Message = "El campo FilterConfiguration debe ser un JSON válido",
                    Data = null
                });
            }

            var savedQuery = new SystemSavedQueries
            {
                Id = Guid.NewGuid(),
                Name = request.Entity.Name,
                Description = request.Entity.Description,
                EntityName = request.Entity.EntityName,
                SelectedFields = request.Entity.SelectedFields,
                FilterConfiguration = request.Entity.FilterConfiguration,
                LogicalOperator = request.Entity.LogicalOperator,
                TakeLimit = request.Entity.TakeLimit,
                IsPublic = request.Entity.IsPublic,
                IsTemplate = request.Entity.IsTemplate,
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

            return Ok(new ApiResponse<SavedQueryDto>
            {
                Success = true,
                Message = "Búsqueda guardada creada exitosamente",
                Data = savedQueryDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando búsqueda guardada");
            return StatusCode(500, new ApiResponse<SavedQueryDto>
            {
                Success = false,
                Message = $"Error creando búsqueda guardada: {ex.Message}",
                Data = null
            });
        }
    }

    /// <summary>
    /// Actualizar una búsqueda guardada (compatible con BaseApiService)
    /// </summary>
    [HttpPut("update")]
    public async Task<ActionResult<ApiResponse<SavedQueryDto>>> Update([FromBody] UpdateRequest<SavedQueryDto> request)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new ApiResponse<SavedQueryDto> { Success = false, Message = "Usuario no autenticado", Data = null });
            }

            if (request?.Entity?.Id == null)
            {
                return BadRequest(new ApiResponse<SavedQueryDto> { Success = false, Message = "ID de la búsqueda guardada requerido", Data = null });
            }

            var userId = user.Id;

            _logger.LogInformation("Actualizando búsqueda guardada {SavedQueryId}", request.Entity.Id);

            var savedQuery = await _context.Set<SystemSavedQueries>()
                .Include(sq => sq.SystemSavedQueryShares)
                .Where(sq => sq.Id == request.Entity.Id && sq.Active)
                .FirstOrDefaultAsync();

            if (savedQuery == null)
            {
                return NotFound(new ApiResponse<SavedQueryDto>
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
                return StatusCode(403, new ApiResponse<SavedQueryDto>
                {
                    Success = false,
                    Message = "No tienes permisos para editar esta búsqueda guardada",
                    Data = null
                });
            }

            // Validar JSON
            if (!IsValidJson(request.Entity.SelectedFields))
            {
                return BadRequest(new ApiResponse<SavedQueryDto>
                {
                    Success = false,
                    Message = "El campo SelectedFields debe ser un JSON válido",
                    Data = null
                });
            }

            if (!string.IsNullOrEmpty(request.Entity.FilterConfiguration) && !IsValidJson(request.Entity.FilterConfiguration))
            {
                return BadRequest(new ApiResponse<SavedQueryDto>
                {
                    Success = false,
                    Message = "El campo FilterConfiguration debe ser un JSON válido",
                    Data = null
                });
            }

            // Actualizar campos
            savedQuery.Name = request.Entity.Name;
            savedQuery.Description = request.Entity.Description;
            savedQuery.SelectedFields = request.Entity.SelectedFields;
            savedQuery.FilterConfiguration = request.Entity.FilterConfiguration;
            savedQuery.LogicalOperator = request.Entity.LogicalOperator;
            savedQuery.TakeLimit = request.Entity.TakeLimit;
            savedQuery.IsPublic = request.Entity.IsPublic;
            savedQuery.IsTemplate = request.Entity.IsTemplate;
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

            return Ok(new ApiResponse<SavedQueryDto>
            {
                Success = true,
                Message = "Búsqueda guardada actualizada exitosamente",
                Data = savedQueryDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando búsqueda guardada");
            return StatusCode(500, new ApiResponse<SavedQueryDto>
            {
                Success = false,
                Message = $"Error actualizando búsqueda guardada: {ex.Message}",
                Data = null
            });
        }
    }

    /// <summary>
    /// Eliminar una búsqueda guardada (compatible con BaseApiService)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new ApiResponse<bool> { Success = false, Message = "Usuario no autenticado", Data = false });
            }

            var userId = user.Id;

            _logger.LogInformation("Eliminando búsqueda guardada {SavedQueryId}", id);

            var savedQuery = await _context.Set<SystemSavedQueries>()
                .Where(sq => sq.Id == id && sq.Active)
                .FirstOrDefaultAsync();

            if (savedQuery == null)
            {
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Búsqueda guardada no encontrada",
                    Data = false
                });
            }

            // Solo el creador puede eliminar
            if (savedQuery.CreadorId != userId)
            {
                return StatusCode(403, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Solo el propietario puede eliminar esta búsqueda guardada",
                    Data = false
                });
            }

            // Soft delete
            savedQuery.Active = false;
            savedQuery.ModificadorId = userId;
            savedQuery.FechaModificacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "Búsqueda guardada eliminada exitosamente",
                Data = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando búsqueda guardada {SavedQueryId}", id);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = $"Error eliminando búsqueda guardada: {ex.Message}",
                Data = false
            });
        }
    }

    /// <summary>
    /// Crear una nueva búsqueda guardada (método original)
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