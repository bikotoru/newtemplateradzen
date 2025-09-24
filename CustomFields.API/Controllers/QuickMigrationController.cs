using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Backend.Utils.Data;

namespace CustomFields.API.Controllers;

/// <summary>
/// API para ejecutar migración rápida del CHECK constraint
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QuickMigrationController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<QuickMigrationController> _logger;

    public QuickMigrationController(AppDbContext context, ILogger<QuickMigrationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Ejecutar migración del CHECK constraint inmediatamente
    /// </summary>
    [HttpPost("execute-now")]
    public async Task<ActionResult<QuickMigrationResponse>> ExecuteMigrationNow()
    {
        try
        {
            _logger.LogInformation("🔧 Iniciando migración rápida del CHECK constraint");

            var steps = new List<string>();

            // 1. Verificar constraint actual
            steps.Add("🔍 Verificando constraint actual...");
            var currentConstraint = await GetCurrentConstraintDefinition();
            steps.Add($"Constraint actual: {currentConstraint}");

            // 2. Eliminar constraint existente
            steps.Add("📋 Eliminando constraint existente...");
            var dropSql = @"
                IF EXISTS (SELECT 1 FROM sys.check_constraints
                           WHERE name = 'CK_system_custom_field_definitions_field_type')
                BEGIN
                    ALTER TABLE system_custom_field_definitions
                    DROP CONSTRAINT CK_system_custom_field_definitions_field_type;
                END";

            await _context.Database.ExecuteSqlRawAsync(dropSql);
            steps.Add("✅ Constraint eliminado");

            // 3. Crear nuevo constraint
            steps.Add("📋 Creando nuevo constraint...");
            var createSql = @"
                ALTER TABLE system_custom_field_definitions
                ADD CONSTRAINT CK_system_custom_field_definitions_field_type
                    CHECK (FieldType IN (
                        'text', 'textarea', 'number', 'date', 'boolean', 'select', 'multiselect',
                        'entity_reference', 'user_reference', 'file_reference'
                    ))";

            await _context.Database.ExecuteSqlRawAsync(createSql);
            steps.Add("✅ Nuevo constraint creado");

            // 4. Verificar nuevo constraint
            steps.Add("🔍 Verificando nuevo constraint...");
            var newConstraint = await GetCurrentConstraintDefinition();
            steps.Add($"Nuevo constraint: {newConstraint}");

            // 5. Probar tipos nuevos
            steps.Add("🧪 Probando tipos de campo nuevos...");
            var testResults = await TestNewFieldTypes();
            steps.AddRange(testResults);

            var response = new QuickMigrationResponse
            {
                Success = true,
                Message = "Migración ejecutada exitosamente",
                Steps = steps,
                NewConstraintDefinition = newConstraint
            };

            _logger.LogInformation("✅ Migración completada exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error ejecutando migración rápida");

            var errorResponse = new QuickMigrationResponse
            {
                Success = false,
                Message = $"Error ejecutando migración: {ex.Message}",
                Steps = new List<string> { $"❌ Error: {ex.Message}" },
                NewConstraintDefinition = "Error"
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Verificar si la migración ya fue ejecutada
    /// </summary>
    [HttpGet("check-status")]
    public async Task<ActionResult<QuickMigrationStatusResponse>> CheckStatus()
    {
        try
        {
            var constraintDef = await GetCurrentConstraintDefinition();
            var isExecuted = constraintDef.Contains("entity_reference") &&
                           constraintDef.Contains("user_reference") &&
                           constraintDef.Contains("file_reference");

            var response = new QuickMigrationStatusResponse
            {
                Success = true,
                MigrationExecuted = isExecuted,
                ConstraintDefinition = constraintDef,
                Message = isExecuted ? "Migración ya ejecutada" : "Migración pendiente"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando estado de migración");

            var errorResponse = new QuickMigrationStatusResponse
            {
                Success = false,
                MigrationExecuted = false,
                ConstraintDefinition = "Error",
                Message = $"Error: {ex.Message}"
            };

            return StatusCode(500, errorResponse);
        }
    }

    private async Task<string> GetCurrentConstraintDefinition()
    {
        try
        {
            var sql = @"
                SELECT ISNULL(cc.definition, 'No existe') AS Definition
                FROM sys.check_constraints cc
                INNER JOIN sys.tables t ON cc.parent_object_id = t.object_id
                WHERE cc.name = 'CK_system_custom_field_definitions_field_type'
                  AND t.name = 'system_custom_field_definitions'";

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            var result = await command.ExecuteScalarAsync();
            return result?.ToString() ?? "No existe";
        }
        catch
        {
            return "Error al obtener definición";
        }
    }

    private async Task<List<string>> TestNewFieldTypes()
    {
        var results = new List<string>();
        var testEntityName = $"TestEntity_{Guid.NewGuid():N}";

        try
        {
            // Test entity_reference
            await TestFieldType(testEntityName, "entity_reference", "Test Entity Reference");
            results.Add("✅ entity_reference: OK");
        }
        catch (Exception ex)
        {
            results.Add($"❌ entity_reference: {ex.Message}");
        }

        try
        {
            // Test user_reference
            await TestFieldType(testEntityName, "user_reference", "Test User Reference");
            results.Add("✅ user_reference: OK");
        }
        catch (Exception ex)
        {
            results.Add($"❌ user_reference: {ex.Message}");
        }

        try
        {
            // Test file_reference
            await TestFieldType(testEntityName, "file_reference", "Test File Reference");
            results.Add("✅ file_reference: OK");
        }
        catch (Exception ex)
        {
            results.Add($"❌ file_reference: {ex.Message}");
        }

        // Cleanup test data
        try
        {
            var cleanupSql = "DELETE FROM system_custom_field_definitions WHERE EntityName LIKE @pattern";
            await _context.Database.ExecuteSqlRawAsync(cleanupSql,
                new SqlParameter("@pattern", testEntityName + "%"));
            results.Add("🧹 Datos de prueba eliminados");
        }
        catch (Exception ex)
        {
            results.Add($"⚠️ Cleanup warning: {ex.Message}");
        }

        return results;
    }

    private async Task TestFieldType(string entityName, string fieldType, string displayName)
    {
        var fieldName = $"TestField_{fieldType}_{Guid.NewGuid():N}";

        var sql = @"
            INSERT INTO system_custom_field_definitions (
                EntityName, FieldName, DisplayName, FieldType, Active
            ) VALUES (@entityName, @fieldName, @displayName, @fieldType, 1)";

        await _context.Database.ExecuteSqlRawAsync(sql,
            new SqlParameter("@entityName", entityName),
            new SqlParameter("@fieldName", fieldName),
            new SqlParameter("@displayName", displayName),
            new SqlParameter("@fieldType", fieldType));
    }
}

public class QuickMigrationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public List<string> Steps { get; set; } = new();
    public string NewConstraintDefinition { get; set; } = "";
}

public class QuickMigrationStatusResponse
{
    public bool Success { get; set; }
    public bool MigrationExecuted { get; set; }
    public string ConstraintDefinition { get; set; } = "";
    public string Message { get; set; } = "";
}