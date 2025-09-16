using Microsoft.AspNetCore.Mvc;
using Forms.Models.DTOs;
using Backend.Utils.Data;
using Shared.Models.Entities.SystemEntities;
using Microsoft.EntityFrameworkCore;

namespace CustomFields.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FormDesignerController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<FormDesignerController> _logger;

    public FormDesignerController(AppDbContext context, ILogger<FormDesignerController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("entities")]
    public async Task<IActionResult> GetAvailableEntities()
    {
        try
        {
            // Obtener organizationId desde contexto (por ahora usar temporal hasta implementar JWT)
            var organizationId = GetCurrentOrganizationId();

            // Obtener entidades del sistema (globales) y específicas de la organización
            var entities = await _context.SystemFormEntities
                .Where(e => e.Active && (e.OrganizationId == null || e.OrganizationId == organizationId))
                .OrderBy(e => e.SortOrder)
                .Select(e => new FormEntityDto
                {
                    Id = e.Id,
                    EntityName = e.EntityName,
                    DisplayName = e.DisplayName,
                    Description = e.Description,
                    IconName = e.IconName,
                    Category = e.Category,
                    AllowCustomFields = e.AllowCustomFields,
                    IsActive = e.Active
                })
                .ToListAsync();

            return Ok(new { success = true, data = entities });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("available-fields")]
    public async Task<IActionResult> GetAvailableFields([FromBody] GetAvailableFieldsRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = new GetAvailableFieldsResponse();

            // Obtener campos del sistema usando reflexión
            response.SystemFields = GetSystemFields(request.EntityName);

            // Obtener campos personalizados
            response.CustomFields = await GetCustomFields(request.EntityName, organizationId);

            return Ok(new { success = true, data = response });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("layout/{entityName}")]
    public async Task<IActionResult> GetFormLayout(string entityName)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();

            // Verificar que la entidad existe
            var entityExists = await _context.SystemFormEntities
                .AnyAsync(e => e.EntityName == entityName && e.Active &&
                         (e.OrganizationId == null || e.OrganizationId == organizationId));

            if (!entityExists)
            {
                return NotFound(new { success = false, message = $"Entidad '{entityName}' no encontrada" });
            }

            // Por ahora devolver un layout básico por defecto
            // En el futuro esto vendría de la base de datos
            var layout = new FormLayoutDto
            {
                EntityName = entityName,
                FormName = $"{entityName} - Formulario Principal",
                Description = $"Formulario principal para la entidad {entityName}",
                IsDefault = true,
                IsActive = true,
                Sections = new List<FormSectionDto>(),
                CreatedAt = DateTime.UtcNow,
                OrganizationId = organizationId
            };

            return Ok(new { success = true, data = layout });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("save-layout")]
    public async Task<IActionResult> SaveFormLayout([FromBody] SaveFormLayoutRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();

            // Verificar que la entidad existe
            var entityExists = await _context.SystemFormEntities
                .AnyAsync(e => e.EntityName == request.EntityName && e.Active &&
                         (e.OrganizationId == null || e.OrganizationId == organizationId));

            if (!entityExists)
            {
                return NotFound(new { success = false, message = $"Entidad '{request.EntityName}' no encontrada" });
            }

            // Aquí implementarías la lógica para guardar el layout
            // Por ahora solo devolvemos success
            var savedLayout = new FormLayoutDto
            {
                Id = Guid.NewGuid(),
                EntityName = request.EntityName,
                FormName = request.FormName,
                Description = request.Description,
                IsDefault = request.IsDefault,
                IsActive = true,
                Sections = request.Sections,
                CreatedAt = DateTime.UtcNow,
                OrganizationId = organizationId
            };

            return Ok(new { success = true, data = savedLayout });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    #region Métodos auxiliares

    private List<FormFieldItemDto> GetSystemFields(string entityName)
    {
        var fields = new List<FormFieldItemDto>();

        // Mapeo básico de campos comunes por entidad
        switch (entityName.ToLower())
        {
            case "empleado":
                fields.AddRange(new[]
                {
                    new FormFieldItemDto { FieldName = "Nombre", DisplayName = "Nombre", FieldType = "text", IsRequired = true, IsSystemField = true, Category = "System", IconName = "person" },
                    new FormFieldItemDto { FieldName = "Apellido", DisplayName = "Apellido", FieldType = "text", IsRequired = true, IsSystemField = true, Category = "System", IconName = "person" },
                    new FormFieldItemDto { FieldName = "Rut", DisplayName = "RUT", FieldType = "text", IsRequired = true, IsSystemField = true, Category = "System", IconName = "badge" },
                    new FormFieldItemDto { FieldName = "Email", DisplayName = "Email", FieldType = "text", IsRequired = false, IsSystemField = true, Category = "System", IconName = "email" },
                    new FormFieldItemDto { FieldName = "Telefono", DisplayName = "Teléfono", FieldType = "text", IsRequired = false, IsSystemField = true, Category = "System", IconName = "phone" },
                    new FormFieldItemDto { FieldName = "FechaNacimiento", DisplayName = "Fecha de Nacimiento", FieldType = "date", IsRequired = false, IsSystemField = true, Category = "System", IconName = "cake" },
                    new FormFieldItemDto { FieldName = "Activo", DisplayName = "Activo", FieldType = "boolean", IsRequired = false, IsSystemField = true, Category = "System", IconName = "toggle_on" }
                });
                break;

            case "empresa":
                fields.AddRange(new[]
                {
                    new FormFieldItemDto { FieldName = "RazonSocial", DisplayName = "Razón Social", FieldType = "text", IsRequired = true, IsSystemField = true, Category = "System", IconName = "business" },
                    new FormFieldItemDto { FieldName = "Rut", DisplayName = "RUT", FieldType = "text", IsRequired = true, IsSystemField = true, Category = "System", IconName = "badge" },
                    new FormFieldItemDto { FieldName = "Direccion", DisplayName = "Dirección", FieldType = "textarea", IsRequired = false, IsSystemField = true, Category = "System", IconName = "location_on" },
                    new FormFieldItemDto { FieldName = "Telefono", DisplayName = "Teléfono", FieldType = "text", IsRequired = false, IsSystemField = true, Category = "System", IconName = "phone" },
                    new FormFieldItemDto { FieldName = "Email", DisplayName = "Email", FieldType = "text", IsRequired = false, IsSystemField = true, Category = "System", IconName = "email" },
                    new FormFieldItemDto { FieldName = "Activo", DisplayName = "Activo", FieldType = "boolean", IsRequired = false, IsSystemField = true, Category = "System", IconName = "toggle_on" }
                });
                break;

            default:
                // Campos genéricos para otras entidades
                fields.AddRange(new[]
                {
                    new FormFieldItemDto { FieldName = "Nombre", DisplayName = "Nombre", FieldType = "text", IsRequired = true, IsSystemField = true, Category = "System", IconName = "text_fields" },
                    new FormFieldItemDto { FieldName = "Descripcion", DisplayName = "Descripción", FieldType = "textarea", IsRequired = false, IsSystemField = true, Category = "System", IconName = "notes" },
                    new FormFieldItemDto { FieldName = "Activo", DisplayName = "Activo", FieldType = "boolean", IsRequired = false, IsSystemField = true, Category = "System", IconName = "toggle_on" }
                });
                break;
        }

        return fields;
    }

    private async Task<List<FormFieldItemDto>> GetCustomFields(string entityName, Guid? organizationId)
    {
        var customFields = await _context.SystemCustomFieldDefinitions
            .Where(cf => cf.EntityName == entityName && cf.OrganizationId == organizationId && cf.IsEnabled)
            .OrderBy(cf => cf.SortOrder)
            .Select(cf => new FormFieldItemDto
            {
                Id = cf.Id,
                FieldName = cf.FieldName,
                DisplayName = cf.DisplayName,
                FieldType = cf.FieldType,
                Description = cf.Description,
                IsRequired = cf.IsRequired,
                IsSystemField = false,
                IsCustomField = true,
                Category = "Custom",
                IconName = GetFieldIcon(cf.FieldType)
            })
            .ToListAsync();

        return customFields;
    }

    private Guid? GetCurrentOrganizationId()
    {
        // TODO: Implementar extracción desde JWT/Claims cuando esté disponible
        // Por ahora devolver null para obtener entidades globales del sistema
        return null;
    }

    private string GetFieldIcon(string fieldType)
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

    #endregion
}