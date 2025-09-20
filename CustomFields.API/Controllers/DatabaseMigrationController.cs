using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Services;

namespace CustomFields.API.Controllers;

/// <summary>
/// API para ejecutar migraciones de base de datos
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DatabaseMigrationController : ControllerBase
{
    private readonly DatabaseMigrationService _migrationService;
    private readonly ILogger<DatabaseMigrationController> _logger;

    public DatabaseMigrationController(
        DatabaseMigrationService migrationService,
        ILogger<DatabaseMigrationController> logger)
    {
        _migrationService = migrationService;
        _logger = logger;
    }

    /// <summary>
    /// Ejecutar migración para agregar tipos de campos de referencia
    /// </summary>
    [HttpPost("execute-reference-fields-migration")]
    public async Task<ActionResult<MigrationResponse>> ExecuteReferenceFieldsMigration()
    {
        try
        {
            _logger.LogInformation("Iniciando migración de tipos de campos de referencia");

            await _migrationService.ExecuteReferenceFieldTypesMigrationAsync();

            var response = new MigrationResponse
            {
                Success = true,
                Message = "Migración ejecutada exitosamente",
                Details = "Los tipos de campos entity_reference, user_reference y file_reference han sido agregados al constraint de la base de datos"
            };

            _logger.LogInformation("Migración completada exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando migración");

            var errorResponse = new MigrationResponse
            {
                Success = false,
                Message = $"Error ejecutando migración: {ex.Message}",
                Details = ex.InnerException?.Message
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtener información del constraint actual
    /// </summary>
    [HttpGet("constraint-info")]
    public async Task<ActionResult<ConstraintInfoResponse>> GetConstraintInfo()
    {
        try
        {
            var definition = await _migrationService.GetConstraintDefinitionAsync();

            var response = new ConstraintInfoResponse
            {
                Success = true,
                ConstraintName = "CK_system_custom_field_definitions_field_type",
                Definition = definition,
                Message = "Información del constraint obtenida exitosamente"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo información del constraint");

            var errorResponse = new ConstraintInfoResponse
            {
                Success = false,
                Message = $"Error obteniendo información: {ex.Message}",
                ConstraintName = "CK_system_custom_field_definitions_field_type",
                Definition = "Error al obtener definición"
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Verificar si la migración ya fue ejecutada
    /// </summary>
    [HttpGet("check-migration-status")]
    public async Task<ActionResult<MigrationStatusResponse>> CheckMigrationStatus()
    {
        try
        {
            // Intentar obtener información del constraint
            var definition = await _migrationService.GetConstraintDefinitionAsync();

            // Verificar si incluye los nuevos tipos
            var includesReferenceTypes = definition.Contains("entity_reference") &&
                                       definition.Contains("user_reference") &&
                                       definition.Contains("file_reference");

            var response = new MigrationStatusResponse
            {
                Success = true,
                MigrationExecuted = includesReferenceTypes,
                ConstraintDefinition = definition,
                Message = includesReferenceTypes
                    ? "La migración ya fue ejecutada exitosamente"
                    : "La migración aún no ha sido ejecutada"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando estado de migración");

            var errorResponse = new MigrationStatusResponse
            {
                Success = false,
                MigrationExecuted = false,
                Message = $"Error verificando estado: {ex.Message}",
                ConstraintDefinition = "Error al obtener definición"
            };

            return StatusCode(500, errorResponse);
        }
    }
}

/// <summary>
/// Respuesta de migración
/// </summary>
public class MigrationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Details { get; set; }
}

/// <summary>
/// Respuesta de información del constraint
/// </summary>
public class ConstraintInfoResponse
{
    public bool Success { get; set; }
    public string ConstraintName { get; set; } = "";
    public string Definition { get; set; } = "";
    public string Message { get; set; } = "";
}

/// <summary>
/// Respuesta del estado de migración
/// </summary>
public class MigrationStatusResponse
{
    public bool Success { get; set; }
    public bool MigrationExecuted { get; set; }
    public string ConstraintDefinition { get; set; } = "";
    public string Message { get; set; } = "";
}