using Microsoft.AspNetCore.Mvc;
using Forms.Models.DTOs;
using Backend.Utils.Data;
using Shared.Models.Entities.SystemEntities;
using Microsoft.EntityFrameworkCore;
using Forms.Models.Configurations;
using System.Text.Json;
using Backend.Utils.Security;
using Shared.Models.DTOs.Auth;

namespace CustomFields.API.Controllers;

[ApiController]
[Route("api/customfielddefinitions")]
public class CustomFieldsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<CustomFieldsController> _logger;
    private readonly PermissionService _permissionService;
    private readonly IServiceProvider _serviceProvider;

    public CustomFieldsController(AppDbContext context, ILogger<CustomFieldsController> logger, PermissionService permissionService, IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        _permissionService = permissionService;
        _serviceProvider = serviceProvider;
    }

    #region Entidades

    /// <summary>
    /// Obtiene todas las entidades disponibles para campos personalizados
    /// </summary>
    [HttpGet("entities")]
    public async Task<IActionResult> GetEntities()
    {
        try
        {
            // Por ahora obtener organizationId desde claims o usar uno temporal
            var organizationId = Guid.NewGuid(); // Temporal

            // Devolver entidades predefinidas
            var entities = new List<FormEntityDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    EntityName = "Empleado",
                    DisplayName = "Empleados",
                    Description = "Gestión de empleados de la organización",
                    IconName = "person",
                    Category = "RRHH",
                    AllowCustomFields = true,
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    EntityName = "Empresa",
                    DisplayName = "Empresas",
                    Description = "Gestión de empresas",
                    IconName = "business",
                    Category = "Core",
                    AllowCustomFields = true,
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    EntityName = "Cliente",
                    DisplayName = "Clientes",
                    Description = "Gestión de clientes",
                    IconName = "person_outline",
                    Category = "Ventas",
                    AllowCustomFields = true,
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    EntityName = "Proveedor",
                    DisplayName = "Proveedores",
                    Description = "Gestión de proveedores",
                    IconName = "local_shipping",
                    Category = "Compras",
                    AllowCustomFields = true,
                    IsActive = true
                }
            };

            return Ok(new { success = true, data = entities });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener entidades");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Agrega una nueva entidad para campos personalizados
    /// </summary>
    [HttpPost("entities")]
    public async Task<IActionResult> AddEntity([FromBody] CreateEntityRequest request)
    {
        try
        {
            // Por ahora obtener organizationId desde claims o usar uno temporal
            var organizationId = Guid.NewGuid(); // Temporal

            // Validar que no exista una entidad con el mismo nombre
            // Por ahora simular la validación, en el futuro usar SystemFormEntities
            var existingEntity = false; // await _context.SystemFormEntities.AnyAsync(e => e.EntityName == request.EntityName && e.OrganizationId == organizationId);

            if (existingEntity)
            {
                return BadRequest(new { success = false, message = "Ya existe una entidad con ese nombre" });
            }

            // Crear nueva entidad (cuando tengamos la tabla SystemFormEntity)
            var newEntity = new FormEntityDto
            {
                Id = Guid.NewGuid(),
                EntityName = request.EntityName,
                DisplayName = request.DisplayName,
                Description = request.Description,
                IconName = request.IconName,
                Category = request.Category,
                AllowCustomFields = true,
                IsActive = true
            };

            return Ok(new { success = true, data = newEntity });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar entidad");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene las tablas de la base de datos disponibles para agregar como entidades
    /// </summary>
    [HttpGet("available-tables")]
    public async Task<IActionResult> GetAvailableTables()
    {
        try
        {
            // Por ahora obtener organizationId desde claims o usar uno temporal
            var organizationId = Guid.NewGuid(); // Temporal

            // Consulta SQL para obtener tablas del esquema dbo que no comiencen con 'system'
            var sqlQuery = @"
                SELECT
                    TABLE_NAME as TableName,
                    TABLE_SCHEMA as SchemaName
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                AND TABLE_SCHEMA = 'dbo'
                AND TABLE_NAME NOT LIKE 'system%'
                AND TABLE_NAME NOT LIKE 'System%'
                AND TABLE_NAME NOT LIKE '__EFMigrationsHistory'
                ORDER BY TABLE_NAME";

            var availableTables = await _context.Database.SqlQueryRaw<DatabaseTableInfo>(sqlQuery).ToListAsync();

            // Obtener las entidades ya agregadas (actualmente las hardcodeadas)
            var existingEntities = new List<string> { "Empleado", "Empresa", "Cliente", "Proveedor" };

            // Filtrar las tablas que no están ya agregadas como entidades
            var filteredTables = availableTables
                .Where(t => !existingEntities.Contains(t.TableName, StringComparer.OrdinalIgnoreCase))
                .Select(t => new AvailableTableDto
                {
                    TableName = t.TableName,
                    SchemaName = t.SchemaName,
                    DisplayName = FormatTableNameForDisplay(t.TableName),
                    Description = $"Tabla {t.TableName} del sistema",
                    Category = "Database"
                })
                .ToList();

            return Ok(new { success = true, data = filteredTables });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tablas disponibles");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Campos Personalizados

    /// <summary>
    /// Obtiene todos los campos personalizados
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCustomFields()
    {
        // Validar usuario y permiso
        var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
        if (errorResult != null) return errorResult;

        try
        {
            _logger.LogInformation($"Getting all custom fields for user {user!.Id}");

            // Usar la organización del usuario autenticado
            var organizationId = user.OrganizationId;

            // Query optimizada con proyección directa para mejor performance
            var customFields = await _context.SystemCustomFieldDefinitions
                .Where(cf => cf.OrganizationId == organizationId && cf.IsEnabled)
                .OrderBy(cf => cf.EntityName)
                .ThenBy(cf => cf.SortOrder)
                .AsNoTracking() // Mejora performance al no trackear cambios
                .Select(cf => new CustomFieldDefinitionDto
                {
                    Id = cf.Id,
                    EntityName = cf.EntityName,
                    FieldName = cf.FieldName,
                    DisplayName = cf.DisplayName,
                    FieldType = cf.FieldType,
                    Description = cf.Description,
                    IsRequired = cf.IsRequired,
                    DefaultValue = cf.DefaultValue,
                    SortOrder = cf.SortOrder,
                    // Evitar deserialización en memoria al proyectar directamente
                    ValidationConfigJson = cf.ValidationConfig,
                    UIConfigJson = cf.Uiconfig,
                    IsEnabled = cf.IsEnabled,
                    OrganizationId = cf.OrganizationId ?? Guid.Empty,
                    FechaCreacion = cf.FechaCreacion,
                    FechaModificacion = cf.FechaModificacion
                })
                .ToListAsync();

            // Deserializar configuraciones solo para los campos que las necesiten
            foreach (var field in customFields)
            {
                field.ValidationConfig = DeserializeValidationConfig(field.ValidationConfigJson);
                field.UIConfig = DeserializeUIConfig(field.UIConfigJson);
            }

            return Ok(new { success = true, data = customFields });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener campos personalizados");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene un campo personalizado por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomFieldById(Guid id)
    {
        // Validar usuario y permiso
        var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
        if (errorResult != null) return errorResult;

        try
        {
            _logger.LogInformation($"Getting custom field {id} by user {user!.Id}");

            // Usar la organización del usuario autenticado
            var organizationId = user.OrganizationId;

            var customFieldRaw = await _context.SystemCustomFieldDefinitions
                .FirstOrDefaultAsync(cf => cf.Id == id && cf.OrganizationId == organizationId && cf.IsEnabled);

            if (customFieldRaw == null)
            {
                return NotFound(new { success = false, message = "Campo personalizado no encontrado" });
            }

            var customField = new CustomFieldDefinitionDto
            {
                Id = customFieldRaw.Id,
                EntityName = customFieldRaw.EntityName,
                FieldName = customFieldRaw.FieldName,
                DisplayName = customFieldRaw.DisplayName,
                FieldType = customFieldRaw.FieldType,
                Description = customFieldRaw.Description,
                IsRequired = customFieldRaw.IsRequired,
                DefaultValue = customFieldRaw.DefaultValue,
                SortOrder = customFieldRaw.SortOrder,
                ValidationConfig = DeserializeValidationConfig(customFieldRaw.ValidationConfig),
                UIConfig = DeserializeUIConfig(customFieldRaw.Uiconfig),
                IsEnabled = customFieldRaw.IsEnabled,
                OrganizationId = customFieldRaw.OrganizationId ?? Guid.Empty,
                FechaCreacion = customFieldRaw.FechaCreacion,
                FechaModificacion = customFieldRaw.FechaModificacion
            };

            return Ok(new { success = true, data = customField });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener campo personalizado {Id}", id);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene campos personalizados de una entidad específica
    /// </summary>
    [HttpGet("entity/{entityName}")]
    public async Task<IActionResult> GetCustomFieldsByEntity(string entityName)
    {
        // Validar usuario y permiso
        var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
        if (errorResult != null) return errorResult;

        try
        {
            _logger.LogInformation($"Getting custom fields for entity {entityName} by user {user!.Id}");

            // Usar la organización del usuario autenticado
            var organizationId = user.OrganizationId;

            // Query optimizada para entidad específica
            var customFields = await _context.SystemCustomFieldDefinitions
                .Where(cf => cf.EntityName == entityName && cf.OrganizationId == organizationId && cf.IsEnabled)
                .OrderBy(cf => cf.SortOrder)
                .AsNoTracking()
                .Select(cf => new CustomFieldDefinitionDto
                {
                    Id = cf.Id,
                    EntityName = cf.EntityName,
                    FieldName = cf.FieldName,
                    DisplayName = cf.DisplayName,
                    FieldType = cf.FieldType,
                    Description = cf.Description,
                    IsRequired = cf.IsRequired,
                    DefaultValue = cf.DefaultValue,
                    SortOrder = cf.SortOrder,
                    ValidationConfigJson = cf.ValidationConfig,
                    UIConfigJson = cf.Uiconfig,
                    IsEnabled = cf.IsEnabled,
                    OrganizationId = cf.OrganizationId ?? Guid.Empty,
                    FechaCreacion = cf.FechaCreacion,
                    FechaModificacion = cf.FechaModificacion
                })
                .ToListAsync();

            // Post-procesar configuraciones
            foreach (var field in customFields)
            {
                field.ValidationConfig = DeserializeValidationConfig(field.ValidationConfigJson);
                field.UIConfig = DeserializeUIConfig(field.UIConfigJson);
            }

            return Ok(new { success = true, data = customFields });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener campos personalizados de la entidad {EntityName}", entityName);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Crea un nuevo campo personalizado
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCustomField([FromBody] CreateCustomFieldRequest request)
    {
        // Validar usuario y permiso
        var (user, hasPermission, errorResult) = await ValidatePermissionAsync("create");
        if (errorResult != null) return errorResult;

        try
        {
            _logger.LogInformation($"Creating custom field for user {user!.Id}");

            // Usar la organización del usuario autenticado
            var organizationId = user.OrganizationId;

            // Validar que no exista un campo con el mismo nombre en la entidad
            var existingField = await _context.SystemCustomFieldDefinitions
                .AnyAsync(cf => cf.EntityName == request.EntityName &&
                               cf.FieldName == request.FieldName &&
                               cf.OrganizationId == organizationId);

            if (existingField)
            {
                return BadRequest(new { success = false, message = "Ya existe un campo con ese nombre en la entidad" });
            }

            var customField = new SystemCustomFieldDefinitions
            {
                Id = Guid.NewGuid(),
                EntityName = request.EntityName,
                FieldName = request.FieldName,
                DisplayName = request.DisplayName,
                FieldType = request.FieldType,
                Description = request.Description,
                IsRequired = request.IsRequired,
                DefaultValue = request.DefaultValue,
                SortOrder = request.SortOrder,
                ValidationConfig = SerializeValidationConfig(request.ValidationConfig),
                Uiconfig = SerializeUIConfig(request.UIConfig),
                IsEnabled = true,
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow,
                OrganizationId = organizationId,
                Active = true,
                Version = 1
            };

            _context.SystemCustomFieldDefinitions.Add(customField);
            await _context.SaveChangesAsync();

            var result = new CustomFieldDefinitionDto
            {
                Id = customField.Id,
                EntityName = customField.EntityName,
                FieldName = customField.FieldName,
                DisplayName = customField.DisplayName,
                FieldType = customField.FieldType,
                Description = customField.Description,
                IsRequired = customField.IsRequired,
                DefaultValue = customField.DefaultValue,
                SortOrder = customField.SortOrder,
                ValidationConfig = DeserializeValidationConfig(customField.ValidationConfig),
                UIConfig = DeserializeUIConfig(customField.Uiconfig),
                IsEnabled = customField.IsEnabled,
                OrganizationId = customField.OrganizationId ?? Guid.Empty,
                FechaCreacion = customField.FechaCreacion,
                FechaModificacion = customField.FechaModificacion
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear campo personalizado");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un campo personalizado existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomField(Guid id, [FromBody] UpdateCustomFieldRequest request)
    {
        // Validar usuario y permiso
        var (user, hasPermission, errorResult) = await ValidatePermissionAsync("update");
        if (errorResult != null) return errorResult;

        try
        {
            _logger.LogInformation($"Updating custom field {id} by user {user!.Id}");

            // Usar la organización del usuario autenticado
            var organizationId = user.OrganizationId;

            var customField = await _context.SystemCustomFieldDefinitions
                .FirstOrDefaultAsync(cf => cf.Id == id && cf.OrganizationId == organizationId);

            if (customField == null)
            {
                return NotFound(new { success = false, message = "Campo personalizado no encontrado" });
            }

            // Actualizar campos (solo los campos permitidos para actualización)
            if (request.DisplayName != null)
                customField.DisplayName = request.DisplayName;

            if (request.Description != null)
                customField.Description = request.Description;

            if (request.IsRequired.HasValue)
                customField.IsRequired = request.IsRequired.Value;

            if (request.DefaultValue != null)
                customField.DefaultValue = request.DefaultValue;

            if (request.SortOrder.HasValue)
                customField.SortOrder = request.SortOrder.Value;

            if (request.IsEnabled.HasValue)
                customField.IsEnabled = request.IsEnabled.Value;

            if (request.ValidationConfig != null)
                customField.ValidationConfig = SerializeValidationConfig(request.ValidationConfig);

            if (request.UIConfig != null)
                customField.Uiconfig = SerializeUIConfig(request.UIConfig);
            customField.FechaModificacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var result = new CustomFieldDefinitionDto
            {
                Id = customField.Id,
                EntityName = customField.EntityName,
                FieldName = customField.FieldName,
                DisplayName = customField.DisplayName,
                FieldType = customField.FieldType,
                Description = customField.Description,
                IsRequired = customField.IsRequired,
                DefaultValue = customField.DefaultValue,
                SortOrder = customField.SortOrder,
                ValidationConfig = DeserializeValidationConfig(customField.ValidationConfig),
                UIConfig = DeserializeUIConfig(customField.Uiconfig),
                IsEnabled = customField.IsEnabled,
                OrganizationId = customField.OrganizationId ?? Guid.Empty,
                FechaCreacion = customField.FechaCreacion,
                FechaModificacion = customField.FechaModificacion
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar campo personalizado");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Elimina (deshabilita) un campo personalizado
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomField(Guid id)
    {
        // Validar usuario y permiso
        var (user, hasPermission, errorResult) = await ValidatePermissionAsync("delete");
        if (errorResult != null) return errorResult;

        try
        {
            _logger.LogInformation($"Deleting custom field {id} by user {user!.Id}");

            // Usar la organización del usuario autenticado
            var organizationId = user.OrganizationId;

            var customField = await _context.SystemCustomFieldDefinitions
                .FirstOrDefaultAsync(cf => cf.Id == id && cf.OrganizationId == organizationId);

            if (customField == null)
            {
                return NotFound(new { success = false, message = "Campo personalizado no encontrado" });
            }

            // Deshabilitar en lugar de eliminar
            customField.IsEnabled = false;
            customField.FechaModificacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Campo personalizado eliminado correctamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar campo personalizado");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    private static string FormatTableNameForDisplay(string tableName)
    {
        // Convertir nombres de tabla como "empleado_documentos" a "Empleado Documentos"
        return string.Join(" ", tableName
            .Split('_', '-', ' ')
            .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
    }

    #endregion

    #region Helpers

    private static ValidationConfig? DeserializeValidationConfig(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ValidationConfig>(json);
        }
        catch
        {
            return null;
        }
    }

    private static UIConfig? DeserializeUIConfig(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<UIConfig>(json);
        }
        catch
        {
            return null;
        }
    }

    private static string? SerializeValidationConfig(ValidationConfig? config)
    {
        if (config == null)
            return null;

        try
        {
            return JsonSerializer.Serialize(config);
        }
        catch
        {
            return null;
        }
    }

    private static string? SerializeUIConfig(UIConfig? config)
    {
        if (config == null)
            return null;

        try
        {
            return JsonSerializer.Serialize(config);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Authentication and Authorization

    /// <summary>
    /// Valida permisos para custom fields
    /// </summary>
    private async Task<(SessionDataDto? user, bool hasPermission, IActionResult? errorResult)> ValidatePermissionAsync(string action)
    {
        try
        {
            var (user, hasPermission) = await CheckPermissionAsync(action);

            if (user == null)
                return (null, false, Unauthorized(new { success = false, message = "Usuario no autenticado" }));

            if (!hasPermission)
                return (user, false, StatusCode(403, new { success = false, message = "No tienes permisos para realizar esta acción" }));

            return (user, true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating permission for action: {Action}", action);
            return (null, false, StatusCode(500, new { success = false, message = "Error interno del servidor" }));
        }
    }

    /// <summary>
    /// Verifica permisos específicos para custom fields
    /// </summary>
    private async Task<(SessionDataDto? user, bool hasPermission)> CheckPermissionAsync(string action)
    {
        try
        {
            var user = await ValidarUsuario();
            if (user == null)
                return (null, false);

            // Usar permisos específicos de custom fields
            var permissionKey = $"FORMDESIGNER.{action.ToUpperInvariant()}";
            var hasPermission = user.Permisos.Contains(permissionKey);

            _logger.LogDebug("Permission check - User: {UserId}, Permission: {Permission}, HasAccess: {HasAccess}",
                user.Id, permissionKey, hasPermission);

            if (!hasPermission)
                _logger.LogWarning("Usuario {UserId} no tiene permiso {Permission}", user.Id, permissionKey);

            return (user, hasPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission for action: {Action}", action);
            return (null, false);
        }
    }

    /// <summary>
    /// Valida el usuario actual desde el contexto HTTP
    /// </summary>
    private async Task<SessionDataDto?> ValidarUsuario()
    {
        try
        {
            return await _permissionService.ValidateUserFromHeadersAsync(Request.Headers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user");
            return null;
        }
    }

    #endregion
}

// DTOs adicionales
public class CreateEntityRequest
{
    public string EntityName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string IconName { get; set; } = "";
    public string Category { get; set; } = "";
}

public class DatabaseTableInfo
{
    public string TableName { get; set; } = "";
    public string SchemaName { get; set; } = "";
}

public class AvailableTableDto
{
    public string TableName { get; set; } = "";
    public string SchemaName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
}

