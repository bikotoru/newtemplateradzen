using Microsoft.AspNetCore.Mvc;
using Forms.Models.DTOs;
using Backend.Utils.Data;
using Shared.Models.Entities.SystemEntities;
using Microsoft.EntityFrameworkCore;
using Backend.Utils.Security;
using Shared.Models.DTOs.Auth;
using Shared.Models.Responses;
using System.Text.Json;
using System.Text.Json.Serialization;
using Forms.Models.Configurations;

namespace CustomFields.API.Controllers;

[ApiController]
[Route("api/form-designer")]
public class FormDesignerController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<FormDesignerController> _logger;
    private readonly IServiceProvider _serviceProvider;

    public FormDesignerController(
        AppDbContext context,
        ILogger<FormDesignerController> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
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

            _logger.LogInformation($"[FormDesignerController] Getting form layout for entity {entityName} by user {user.Id}");

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
                _logger.LogInformation($"[FormDesignerController] Found existing layout, deserializing JSON:");
                _logger.LogInformation($"[FormDesignerController] {existingLayout.LayoutConfig}");

                // Configurar opciones de deserialización para manejar valores null
                var deserializerOptions = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                // Deserializar las secciones desde JSON
                var sections = JsonSerializer.Deserialize<List<FormSectionDto>>(existingLayout.LayoutConfig, deserializerOptions);

                // Enriquecer los campos con información de uiConfig y validationConfig
                if (sections != null)
                {
                    await EnrichFieldsWithCustomFieldData(sections, user.OrganizationId);

                    // Log después del enriquecimiento
                    _logger.LogInformation($"[FormDesignerController] After enrichment:");
                    foreach (var section in sections)
                    {
                        foreach (var field in section.Fields)
                        {
                            _logger.LogInformation($"[FormDesignerController] Field: {field.FieldName}");
                            if (field.UIConfig != null)
                            {
                                _logger.LogInformation($"[FormDesignerController]   UIConfig after enrichment:");
                                _logger.LogInformation($"[FormDesignerController]     TrueLabel: {field.UIConfig.TrueLabel}");
                                _logger.LogInformation($"[FormDesignerController]     FalseLabel: {field.UIConfig.FalseLabel}");
                                _logger.LogInformation($"[FormDesignerController]     Format: {field.UIConfig.Format}");
                                _logger.LogInformation($"[FormDesignerController]     DecimalPlaces: {field.UIConfig.DecimalPlaces}");
                                _logger.LogInformation($"[FormDesignerController]     Prefix: {field.UIConfig.Prefix}");
                                _logger.LogInformation($"[FormDesignerController]     Suffix: {field.UIConfig.Suffix}");
                            }
                            else
                            {
                                _logger.LogWarning($"[FormDesignerController]   UIConfig is NULL after enrichment for field {field.FieldName}");
                            }
                        }
                    }
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

            _logger.LogInformation($"[FormDesignerController] Saving form layout for entity {request.EntityName} by user {user.Id}");

            // Log de los datos recibidos para debug de UIConfig
            if (request.Sections != null)
            {
                foreach (var section in request.Sections)
                {
                    _logger.LogInformation($"[FormDesignerController] Section: {section.Title}");
                    if (section.Fields != null)
                    {
                        foreach (var field in section.Fields)
                        {
                            _logger.LogInformation($"[FormDesignerController] Field: {field.FieldName}");
                            _logger.LogInformation($"[FormDesignerController]   FieldType: {field.FieldType}");
                            _logger.LogInformation($"[FormDesignerController]   DisplayName: {field.DisplayName}");

                            if (field.UIConfig != null)
                            {
                                _logger.LogInformation($"[FormDesignerController]   UIConfig received:");
                                _logger.LogInformation($"[FormDesignerController]     TrueLabel: {field.UIConfig.TrueLabel}");
                                _logger.LogInformation($"[FormDesignerController]     FalseLabel: {field.UIConfig.FalseLabel}");
                                _logger.LogInformation($"[FormDesignerController]     Format: {field.UIConfig.Format}");
                                _logger.LogInformation($"[FormDesignerController]     DecimalPlaces: {field.UIConfig.DecimalPlaces}");
                                _logger.LogInformation($"[FormDesignerController]     Prefix: {field.UIConfig.Prefix}");
                                _logger.LogInformation($"[FormDesignerController]     Suffix: {field.UIConfig.Suffix}");
                            }
                            else
                            {
                                _logger.LogWarning($"[FormDesignerController]   UIConfig is NULL for field {field.FieldName}");
                            }
                        }
                    }
                }
            }

            // Debug específico para UIConfig antes de serializar
            foreach (var section in request.Sections ?? new List<FormSectionDto>())
            {
                foreach (var field in section.Fields ?? new List<FormFieldLayoutDto>())
                {
                    if (field.FieldType.ToLowerInvariant() == "boolean")
                    {
                        _logger.LogInformation($"[FormDesignerController] BEFORE SERIALIZATION - Boolean field: {field.FieldName}");
                        _logger.LogInformation($"[FormDesignerController]   UIConfig object: {field.UIConfig != null}");
                        if (field.UIConfig != null)
                        {
                            _logger.LogInformation($"[FormDesignerController]   TrueLabel: '{field.UIConfig.TrueLabel}'");
                            _logger.LogInformation($"[FormDesignerController]   FalseLabel: '{field.UIConfig.FalseLabel}'");
                            _logger.LogInformation($"[FormDesignerController]   Style: '{field.UIConfig.Style}'");
                        }
                    }
                }
            }

            // Configurar opciones de serialización para omitir valores null
            var serializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };

            // Serializar las secciones a JSON
            var sectionsJson = JsonSerializer.Serialize(request.Sections ?? new List<FormSectionDto>(), serializerOptions);

            _logger.LogInformation($"[FormDesignerController] Serialized JSON to save:");
            _logger.LogInformation($"[FormDesignerController] {sectionsJson}");

            // Buscar si ya existe un layout para esta entidad y organización (sin importar si es default)
            _logger.LogInformation($"[FormDesignerController] Searching for existing layout:");
            _logger.LogInformation($"[FormDesignerController]   EntityName: {request.EntityName}");
            _logger.LogInformation($"[FormDesignerController]   OrganizationId: {user.OrganizationId}");
            _logger.LogInformation($"[FormDesignerController]   IsActive: true");

            var existingLayout = await _context.SystemFormLayouts
                .Where(l => l.EntityName == request.EntityName &&
                           l.OrganizationId == user.OrganizationId &&
                           l.IsActive)
                .FirstOrDefaultAsync();

            _logger.LogInformation($"[FormDesignerController] Existing layout found: {existingLayout != null}");

            // Debug: Mostrar todos los layouts existentes para esta entidad
            var allLayouts = await _context.SystemFormLayouts
                .Where(l => l.EntityName == request.EntityName)
                .ToListAsync();

            _logger.LogInformation($"[FormDesignerController] All layouts for entity '{request.EntityName}': {allLayouts.Count}");
            foreach (var layout in allLayouts)
            {
                _logger.LogInformation($"[FormDesignerController]   Layout: Id={layout.Id}, OrgId={layout.OrganizationId}, IsDefault={layout.IsDefault}, IsActive={layout.IsActive}");
            }

            SystemFormLayouts layoutToReturn;

            if (existingLayout != null)
            {
                // Actualizar layout existente
                _logger.LogInformation($"[FormDesignerController] Updating existing layout {existingLayout.Id}");

                // Si la request especifica que debe ser default, manejar otros layouts default
                if (request.IsDefault && !existingLayout.IsDefault)
                {
                    var otherDefaults = await _context.SystemFormLayouts
                        .Where(l => l.EntityName == request.EntityName &&
                                   l.OrganizationId == user.OrganizationId &&
                                   l.IsDefault &&
                                   l.Id != existingLayout.Id)
                        .ToListAsync();

                    foreach (var layout in otherDefaults)
                    {
                        layout.IsDefault = false;
                        layout.FechaModificacion = DateTime.UtcNow;
                        layout.ModificadorId = user.Id;
                    }
                }

                existingLayout.FormName = request.FormName;
                existingLayout.Description = request.Description;
                existingLayout.LayoutConfig = sectionsJson;
                existingLayout.IsDefault = request.IsDefault;
                existingLayout.FechaModificacion = DateTime.UtcNow;
                existingLayout.ModificadorId = user.Id;
                existingLayout.Version += 1;

                layoutToReturn = existingLayout;
            }
            else
            {
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
                _logger.LogInformation($"[FormDesignerController] Creating new layout for entity {request.EntityName}");

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
                layoutToReturn = newLayout;
            }

            await _context.SaveChangesAsync();

            // Crear DTO de respuesta
            var savedLayout = new FormLayoutDto
            {
                Id = layoutToReturn.Id,
                EntityName = layoutToReturn.EntityName,
                FormName = layoutToReturn.FormName,
                Description = layoutToReturn.Description,
                IsDefault = layoutToReturn.IsDefault,
                IsActive = layoutToReturn.IsActive,
                OrganizationId = layoutToReturn.OrganizationId,
                CreatedAt = layoutToReturn.FechaCreacion,
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

        // Enriquecer los campos en las secciones solo con datos faltantes
        foreach (var section in sections)
        {
            foreach (var field in section.Fields.Where(f => !f.IsSystemField))
            {
                if (fieldDataLookup.TryGetValue(field.FieldName, out var fieldData))
                {
                    // Solo establecer ValidationConfig si no existe en el layout
                    if (field.ValidationConfig == null && fieldData.ValidationConfig != null)
                    {
                        field.ValidationConfig = fieldData.ValidationConfig;
                    }

                    // Solo establecer UIConfig si no existe en el layout
                    if (field.UIConfig == null && fieldData.UIConfig != null)
                    {
                        field.UIConfig = fieldData.UIConfig;
                    }

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
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            return JsonSerializer.Deserialize<ValidationConfig>(json, options);
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
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            return JsonSerializer.Deserialize<UIConfig>(json, options);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Private Methods

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

    #endregion
}