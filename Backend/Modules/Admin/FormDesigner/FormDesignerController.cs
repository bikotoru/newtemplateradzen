using Microsoft.AspNetCore.Mvc;
using Forms.Models.DTOs;
using Backend.Utils.Data;
using Shared.Models.Entities.SystemEntities;
using Microsoft.EntityFrameworkCore;
using Backend.Utils.Security;
using Shared.Models.DTOs.Auth;
using Shared.Models.Responses;
using System.Text.Json;
using Forms.Models.Configurations;

namespace Backend.Modules.Admin.FormDesigner;

[ApiController]
[Route("api/form-designer")]
public class FormDesignerController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<FormDesignerController> _logger;
    private readonly PermissionService _permissionService;
    private readonly IServiceProvider _serviceProvider;

    public FormDesignerController(AppDbContext context, ILogger<FormDesignerController> logger,
        PermissionService permissionService, IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        _permissionService = permissionService;
        _serviceProvider = serviceProvider;
    }

    #region Available Fields

    /// <summary>
    /// Obtiene todos los campos disponibles para una entidad (campos del sistema + campos personalizados)
    /// </summary>
    [HttpPost("formulario/available-fields")]
    public async Task<IActionResult> GetAvailableFields([FromBody] GetAvailableFieldsRequest request)
    {
        try
        {
            // Validar usuario y permisos
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "Usuario no autenticado" });
            }

            var hasPermission = user.Permisos.Contains("FORMDESIGNER.VIEW");
            if (!hasPermission)
            {
                return StatusCode(403, new { success = false, message = "No tienes permisos para ver el diseñador de formularios" });
            }

            _logger.LogInformation($"Getting available fields for entity {request.EntityName} by user {user.Id}");

            var response = new GetAvailableFieldsResponse();

            // 1. Obtener campos del sistema basados en la entidad
            response.SystemFields = GetSystemFieldsForEntity(request.EntityName);

            // 2. Obtener campos personalizados desde la base de datos
            var organizationId = request.OrganizationId ?? user.OrganizationId;

            var customFieldsQuery = await _context.SystemCustomFieldDefinitions
                .Where(f => f.EntityName == request.EntityName &&
                           f.IsEnabled &&
                           (f.OrganizationId == organizationId || f.OrganizationId == null))
                .OrderBy(f => f.SortOrder)
                .Select(f => new
                {
                    f.Id,
                    f.FieldName,
                    f.DisplayName,
                    f.FieldType,
                    f.Description,
                    f.IsRequired
                })
                .ToListAsync();

            var customFields = customFieldsQuery.Select(f => new FormFieldItemDto
            {
                Id = f.Id,
                FieldName = f.FieldName,
                DisplayName = f.DisplayName,
                FieldType = f.FieldType,
                Description = f.Description,
                IsRequired = f.IsRequired,
                IsSystemField = false,
                IsCustomField = true,
                IconName = GetFieldTypeIcon(f.FieldType),
                Category = "Custom"
            }).ToList();

            response.CustomFields = customFields;

            // 3. Campos relacionados (por ahora vacío, se puede expandir después)
            response.RelatedFields = new List<FormFieldItemDto>();

            return Ok(ApiResponse<GetAvailableFieldsResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available fields for entity: {EntityName}", request.EntityName);
            return StatusCode(500, ApiResponse<GetAvailableFieldsResponse>.ErrorResponse("Error interno del servidor"));
        }
    }

    #endregion

    #region Form Layout

    /// <summary>
    /// Obtiene el layout de formulario para una entidad específica
    /// </summary>
    [HttpGet("formulario/layout/{entityName}")]
    public async Task<IActionResult> GetFormLayout(string entityName)
    {
        try
        {
            // Validar usuario y permisos
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "Usuario no autenticado" });
            }

            var hasPermission = user.Permisos.Contains("FORMDESIGNER.VIEW");
            if (!hasPermission)
            {
                return StatusCode(403, new { success = false, message = "No tienes permisos para ver el diseñador de formularios" });
            }

            _logger.LogInformation($"Getting form layout for entity {entityName} by user {user.Id}");

            // Buscar layout guardado en la base de datos
            var existingLayout = await _context.SystemFormLayouts
                .Where(l => l.EntityName == entityName &&
                           l.OrganizationId == user.OrganizationId &&
                           l.IsDefault &&
                           l.IsActive)
                .FirstOrDefaultAsync();

            FormLayoutDto layout;

            if (existingLayout != null)
            {
                // Deserializar las secciones desde JSON
                var sections = JsonSerializer.Deserialize<List<FormSectionDto>>(existingLayout.LayoutConfig);

                // Enriquecer los campos con información de uiConfig y validationConfig
                if (sections != null)
                {
                    await EnrichFieldsWithCustomFieldData(sections, user.OrganizationId);
                }

                layout = new FormLayoutDto
                {
                    Id = existingLayout.Id,
                    EntityName = existingLayout.EntityName,
                    FormName = existingLayout.FormName,
                    Description = existingLayout.Description,
                    IsDefault = existingLayout.IsDefault,
                    IsActive = existingLayout.IsActive,
                    OrganizationId = existingLayout.OrganizationId,
                    CreatedAt = existingLayout.FechaCreacion,
                    Sections = sections ?? new List<FormSectionDto>()
                };
            }
            else
            {
                // Si no existe, crear layout por defecto
                layout = CreateDefaultLayout(entityName, user.OrganizationId);
            }

            return Ok(ApiResponse<FormLayoutDto>.SuccessResponse(layout));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting form layout for entity: {EntityName}", entityName);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Error interno del servidor"));
        }
    }

    #endregion

    #region Save Layout

    /// <summary>
    /// Guarda el layout de formulario para una entidad específica
    /// </summary>
    [HttpPost("formulario/save-layout")]
    public async Task<IActionResult> SaveFormLayout([FromBody] SaveFormLayoutRequest request)
    {
        try
        {
            // Validar usuario y permisos
            var user = await ValidarUsuario();
            if (user == null)
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Usuario no autenticado"));
            }

            var hasPermission = user.Permisos.Contains("FORMDESIGNER.CREATE") || user.Permisos.Contains("FORMDESIGNER.UPDATE");
            if (!hasPermission)
            {
                return StatusCode(403, ApiResponse<object>.ErrorResponse("No tienes permisos para guardar layouts de formularios"));
            }

            _logger.LogInformation($"Saving form layout for entity {request.EntityName} by user {user.Id}");

            // Serializar las secciones a JSON
            var sectionsJson = JsonSerializer.Serialize(request.Sections ?? new List<FormSectionDto>());

            // Si es default, desactivar otros layouts default para la misma entidad
            if (request.IsDefault)
            {
                var existingDefaults = await _context.SystemFormLayouts
                    .Where(l => l.EntityName == request.EntityName &&
                               l.OrganizationId == user.OrganizationId &&
                               l.IsDefault)
                    .ToListAsync();

                foreach (var layout in existingDefaults)
                {
                    layout.IsDefault = false;
                    layout.FechaModificacion = DateTime.UtcNow;
                    layout.ModificadorId = user.Id;
                }
            }

            // Crear nuevo layout
            var newLayout = new SystemFormLayouts
            {
                Id = Guid.NewGuid(),
                EntityName = request.EntityName,
                FormName = request.FormName,
                Description = request.Description,
                IsDefault = request.IsDefault,
                IsActive = true,
                Version = 1,
                LayoutConfig = sectionsJson,
                OrganizationId = user.OrganizationId,
                CreadorId = user.Id,
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow
            };

            _context.SystemFormLayouts.Add(newLayout);
            await _context.SaveChangesAsync();

            // Crear DTO de respuesta
            var savedLayout = new FormLayoutDto
            {
                Id = newLayout.Id,
                EntityName = newLayout.EntityName,
                FormName = newLayout.FormName,
                Description = newLayout.Description,
                IsDefault = newLayout.IsDefault,
                IsActive = newLayout.IsActive,
                OrganizationId = newLayout.OrganizationId,
                CreatedAt = newLayout.FechaCreacion,
                Sections = request.Sections ?? new List<FormSectionDto>()
            };

            return Ok(ApiResponse<FormLayoutDto>.SuccessResponse(savedLayout, "Layout guardado exitosamente"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving form layout for entity: {EntityName}", request.EntityName);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Error interno del servidor"));
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Obtiene los campos del sistema para una entidad específica
    /// </summary>
    private List<FormFieldItemDto> GetSystemFieldsForEntity(string entityName)
    {
        return entityName.ToLowerInvariant() switch
        {
            "empleado" => GetEmpleadoSystemFields(),
            "empresa" => GetEmpresaSystemFields(),
            "cliente" => GetClienteSystemFields(),
            "proveedor" => GetProveedorSystemFields(),
            _ => new List<FormFieldItemDto>()
        };
    }

    private List<FormFieldItemDto> GetEmpleadoSystemFields()
    {
        return new List<FormFieldItemDto>
        {
            new() { FieldName = "Nombre", DisplayName = "Nombre", FieldType = "text", IsSystemField = true, Category = "System", IconName = "person" },
            new() { FieldName = "Apellido", DisplayName = "Apellido", FieldType = "text", IsSystemField = true, Category = "System", IconName = "person" },
            new() { FieldName = "Email", DisplayName = "Email", FieldType = "text", IsSystemField = true, Category = "System", IconName = "email" },
            new() { FieldName = "Telefono", DisplayName = "Teléfono", FieldType = "text", IsSystemField = true, Category = "System", IconName = "phone" },
            new() { FieldName = "FechaNacimiento", DisplayName = "Fecha de Nacimiento", FieldType = "date", IsSystemField = true, Category = "System", IconName = "calendar_today" },
            new() { FieldName = "FechaIngreso", DisplayName = "Fecha de Ingreso", FieldType = "date", IsSystemField = true, Category = "System", IconName = "work" },
            new() { FieldName = "Rut", DisplayName = "RUT", FieldType = "text", IsSystemField = true, Category = "System", IconName = "badge" },
            new() { FieldName = "Cargo", DisplayName = "Cargo", FieldType = "select", IsSystemField = true, Category = "System", IconName = "work_outline" },
            new() { FieldName = "Sueldo", DisplayName = "Sueldo", FieldType = "number", IsSystemField = true, Category = "System", IconName = "attach_money" }
        };
    }

    private List<FormFieldItemDto> GetEmpresaSystemFields()
    {
        return new List<FormFieldItemDto>
        {
            new() { FieldName = "RazonSocial", DisplayName = "Razón Social", FieldType = "text", IsSystemField = true, Category = "System", IconName = "business" },
            new() { FieldName = "Rut", DisplayName = "RUT", FieldType = "text", IsSystemField = true, Category = "System", IconName = "badge" },
            new() { FieldName = "Giro", DisplayName = "Giro", FieldType = "text", IsSystemField = true, Category = "System", IconName = "category" },
            new() { FieldName = "Direccion", DisplayName = "Dirección", FieldType = "text", IsSystemField = true, Category = "System", IconName = "location_on" },
            new() { FieldName = "Telefono", DisplayName = "Teléfono", FieldType = "text", IsSystemField = true, Category = "System", IconName = "phone" },
            new() { FieldName = "Email", DisplayName = "Email", FieldType = "text", IsSystemField = true, Category = "System", IconName = "email" },
            new() { FieldName = "Activa", DisplayName = "Activa", FieldType = "boolean", IsSystemField = true, Category = "System", IconName = "toggle_on" }
        };
    }

    private List<FormFieldItemDto> GetClienteSystemFields()
    {
        return new List<FormFieldItemDto>
        {
            new() { FieldName = "Nombre", DisplayName = "Nombre", FieldType = "text", IsSystemField = true, Category = "System", IconName = "person" },
            new() { FieldName = "Apellido", DisplayName = "Apellido", FieldType = "text", IsSystemField = true, Category = "System", IconName = "person" },
            new() { FieldName = "Email", DisplayName = "Email", FieldType = "text", IsSystemField = true, Category = "System", IconName = "email" },
            new() { FieldName = "Telefono", DisplayName = "Teléfono", FieldType = "text", IsSystemField = true, Category = "System", IconName = "phone" },
            new() { FieldName = "Direccion", DisplayName = "Dirección", FieldType = "text", IsSystemField = true, Category = "System", IconName = "location_on" },
            new() { FieldName = "Activo", DisplayName = "Activo", FieldType = "boolean", IsSystemField = true, Category = "System", IconName = "toggle_on" }
        };
    }

    private List<FormFieldItemDto> GetProveedorSystemFields()
    {
        return new List<FormFieldItemDto>
        {
            new() { FieldName = "RazonSocial", DisplayName = "Razón Social", FieldType = "text", IsSystemField = true, Category = "System", IconName = "business" },
            new() { FieldName = "Rut", DisplayName = "RUT", FieldType = "text", IsSystemField = true, Category = "System", IconName = "badge" },
            new() { FieldName = "Contacto", DisplayName = "Contacto", FieldType = "text", IsSystemField = true, Category = "System", IconName = "person" },
            new() { FieldName = "Telefono", DisplayName = "Teléfono", FieldType = "text", IsSystemField = true, Category = "System", IconName = "phone" },
            new() { FieldName = "Email", DisplayName = "Email", FieldType = "text", IsSystemField = true, Category = "System", IconName = "email" },
            new() { FieldName = "Direccion", DisplayName = "Dirección", FieldType = "text", IsSystemField = true, Category = "System", IconName = "location_on" },
            new() { FieldName = "Activo", DisplayName = "Activo", FieldType = "boolean", IsSystemField = true, Category = "System", IconName = "toggle_on" }
        };
    }

    private static string GetFieldTypeIcon(string fieldType)
    {
        return fieldType switch
        {
            "text" => "text_fields",
            "textarea" => "notes",
            "number" => "pin",
            "date" => "calendar_today",
            "boolean" => "toggle_on",
            "select" => "list",
            "multiselect" => "checklist",
            _ => "help"
        };
    }

    /// <summary>
    /// Crea un layout por defecto para una entidad
    /// </summary>
    private FormLayoutDto CreateDefaultLayout(string entityName, Guid? organizationId)
    {
        return new FormLayoutDto
        {
            Id = Guid.NewGuid(),
            EntityName = entityName,
            FormName = $"Formulario {entityName}",
            Description = $"Formulario por defecto para {entityName}",
            IsDefault = true,
            IsActive = true,
            OrganizationId = organizationId,
            CreatedAt = DateTime.UtcNow,
            Sections = new List<FormSectionDto>
            {
                new FormSectionDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Información General",
                    Description = "Datos básicos del formulario",
                    GridSize = 12,
                    SortOrder = 1,
                    IsCollapsible = true,
                    IsExpanded = true,
                    Fields = new List<FormFieldLayoutDto>()
                }
            }
        };
    }

    /// <summary>
    /// Valida que el usuario esté autenticado
    /// </summary>
    private async Task<SessionDataDto?> ValidarUsuario()
    {
        try
        {
            var currentUserService = _serviceProvider.GetRequiredService<Backend.Utils.Security.ICurrentUserService>();
            var sessionData = await currentUserService.GetCurrentUserAsync();

            if (sessionData == null)
            {
                _logger.LogWarning("No se pudo obtener la sesión del usuario");
                return null;
            }

            return sessionData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validando usuario");
            return null;
        }
    }

    /// <summary>
    /// Enriquece los campos de las secciones con información de uiConfig y validationConfig desde SystemCustomFieldDefinitions
    /// </summary>
    private async Task EnrichFieldsWithCustomFieldData(List<FormSectionDto> sections, Guid? organizationId)
    {
        // Obtener todos los nombres de campos personalizados de las secciones
        var customFieldNames = sections
            .SelectMany(s => s.Fields)
            .Where(f => !f.IsSystemField)
            .Select(f => f.FieldName)
            .Distinct()
            .ToList();

        if (!customFieldNames.Any())
            return;

        // Obtener la información completa de los campos personalizados
        var customFieldsData = await _context.SystemCustomFieldDefinitions
            .Where(cf => customFieldNames.Contains(cf.FieldName) &&
                        cf.OrganizationId == organizationId &&
                        cf.IsEnabled)
            .Select(cf => new
            {
                cf.FieldName,
                cf.ValidationConfig,
                cf.Uiconfig,
                cf.Description,
                cf.DefaultValue
            })
            .ToListAsync();

        // Crear un diccionario para búsqueda rápida
        var fieldDataLookup = customFieldsData.ToDictionary(
            cf => cf.FieldName,
            cf => new
            {
                ValidationConfig = DeserializeValidationConfig(cf.ValidationConfig),
                UIConfig = DeserializeUIConfig(cf.Uiconfig),
                cf.Description,
                cf.DefaultValue
            }
        );

        // Enriquecer los campos en las secciones
        foreach (var section in sections)
        {
            foreach (var field in section.Fields.Where(f => !f.IsSystemField))
            {
                if (fieldDataLookup.TryGetValue(field.FieldName, out var fieldData))
                {
                    field.ValidationConfig = fieldData.ValidationConfig;
                    field.UIConfig = fieldData.UIConfig;

                    // Actualizar descripción y valor por defecto si no están establecidos
                    if (string.IsNullOrEmpty(field.Description))
                        field.Description = fieldData.Description;

                    if (string.IsNullOrEmpty(field.DefaultValue))
                        field.DefaultValue = fieldData.DefaultValue;
                }
            }
        }
    }

    /// <summary>
    /// Deserializa la configuración de validación desde JSON
    /// </summary>
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

    /// <summary>
    /// Deserializa la configuración de UI desde JSON
    /// </summary>
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

    #endregion
}