using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;

namespace Backend.Utils.Services;

/// <summary>
/// Servicio para ejecutar migraciones de base de datos
/// </summary>
public class DatabaseMigrationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(AppDbContext context, ILogger<DatabaseMigrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Ejecutar migración para agregar tipos de campos de referencia
    /// </summary>
    public async Task ExecuteReferenceFieldTypesMigrationAsync()
    {
        try
        {
            _logger.LogInformation("Ejecutando migración: Agregar tipos de campos de referencia");

            // 1. Verificar si ya se ejecutó la migración
            var constraintExists = await CheckConstraintSupportsReferenceTypesAsync();
            if (constraintExists)
            {
                _logger.LogInformation("La migración ya fue ejecutada anteriormente");
                return;
            }

            // 2. Eliminar constraint existente
            _logger.LogInformation("Eliminando constraint CK_system_custom_field_definitions_field_type");

            var dropConstraintSql = @"
                IF EXISTS (SELECT 1 FROM sys.check_constraints
                           WHERE name = 'CK_system_custom_field_definitions_field_type')
                BEGIN
                    ALTER TABLE system_custom_field_definitions
                    DROP CONSTRAINT CK_system_custom_field_definitions_field_type;
                END";

            await _context.Database.ExecuteSqlRawAsync(dropConstraintSql);
            _logger.LogInformation("Constraint eliminado exitosamente");

            // 3. Crear nuevo constraint con tipos de referencia
            _logger.LogInformation("Creando nuevo constraint con tipos de referencia");

            var createConstraintSql = @"
                ALTER TABLE system_custom_field_definitions
                ADD CONSTRAINT CK_system_custom_field_definitions_field_type
                    CHECK (FieldType IN (
                        'text', 'textarea', 'number', 'date', 'boolean', 'select', 'multiselect',
                        'entity_reference', 'user_reference', 'file_reference'
                    ))";

            await _context.Database.ExecuteSqlRawAsync(createConstraintSql);
            _logger.LogInformation("Nuevo constraint creado exitosamente");

            // 4. Verificar que la migración fue exitosa
            var newConstraintExists = await CheckConstraintSupportsReferenceTypesAsync();
            if (newConstraintExists)
            {
                _logger.LogInformation("✅ Migración completada exitosamente");
            }
            else
            {
                throw new InvalidOperationException("La migración falló: el constraint no fue creado correctamente");
            }

            // 5. Ejecutar pruebas de validación
            await ExecuteValidationTestsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando migración de tipos de campos de referencia");
            throw;
        }
    }

    /// <summary>
    /// Verificar si el constraint soporta los tipos de referencia
    /// </summary>
    private async Task<bool> CheckConstraintSupportsReferenceTypesAsync()
    {
        try
        {
            // Intentar insertar un registro con entity_reference y eliminarlo inmediatamente
            var testEntityName = $"TestEntity_{Guid.NewGuid():N}";
            var testFieldName = $"TestField_{Guid.NewGuid():N}";

            var testSql = @"
                INSERT INTO system_custom_field_definitions (
                    EntityName, FieldName, DisplayName, FieldType, Active
                ) VALUES (
                    @EntityName, @FieldName, 'Test Entity Reference', 'entity_reference', 1
                );

                DELETE FROM system_custom_field_definitions
                WHERE EntityName = @EntityName AND FieldName = @FieldName;";

            await _context.Database.ExecuteSqlRawAsync(testSql,
                new Microsoft.Data.SqlClient.SqlParameter("@EntityName", testEntityName),
                new Microsoft.Data.SqlClient.SqlParameter("@FieldName", testFieldName));

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Ejecutar pruebas de validación después de la migración
    /// </summary>
    private async Task ExecuteValidationTestsAsync()
    {
        _logger.LogInformation("Ejecutando pruebas de validación...");

        var testEntityName = $"TestEntity_{Guid.NewGuid():N}";

        try
        {
            // Prueba 1: entity_reference
            await TestFieldTypeAsync(testEntityName, "entity_reference", "Test Entity Reference");
            _logger.LogInformation("✅ Prueba entity_reference: exitosa");

            // Prueba 2: user_reference
            await TestFieldTypeAsync(testEntityName, "user_reference", "Test User Reference");
            _logger.LogInformation("✅ Prueba user_reference: exitosa");

            // Prueba 3: file_reference
            await TestFieldTypeAsync(testEntityName, "file_reference", "Test File Reference");
            _logger.LogInformation("✅ Prueba file_reference: exitosa");

            // Prueba 4: tipo inválido (debería fallar)
            try
            {
                await TestFieldTypeAsync(testEntityName, "invalid_type", "Test Invalid");
                _logger.LogWarning("❌ Prueba tipo inválido: falló (tipo inválido fue aceptado)");
            }
            catch (Exception)
            {
                _logger.LogInformation("✅ Prueba tipo inválido: exitosa (correctamente rechazado)");
            }
        }
        finally
        {
            // Limpiar datos de prueba
            var cleanupSql = "DELETE FROM system_custom_field_definitions WHERE EntityName LIKE @Pattern";
            await _context.Database.ExecuteSqlRawAsync(cleanupSql,
                new Microsoft.Data.SqlClient.SqlParameter("@Pattern", testEntityName + "%"));

            _logger.LogInformation("🧹 Datos de prueba eliminados");
        }
    }

    /// <summary>
    /// Probar un tipo de campo específico
    /// </summary>
    private async Task TestFieldTypeAsync(string entityName, string fieldType, string displayName)
    {
        var fieldName = $"TestField_{fieldType}_{Guid.NewGuid():N}";

        var testSql = @"
            INSERT INTO system_custom_field_definitions (
                EntityName, FieldName, DisplayName, FieldType, Active
            ) VALUES (
                @EntityName, @FieldName, @DisplayName, @FieldType, 1
            )";

        await _context.Database.ExecuteSqlRawAsync(testSql,
            new Microsoft.Data.SqlClient.SqlParameter("@EntityName", entityName),
            new Microsoft.Data.SqlClient.SqlParameter("@FieldName", fieldName),
            new Microsoft.Data.SqlClient.SqlParameter("@DisplayName", displayName),
            new Microsoft.Data.SqlClient.SqlParameter("@FieldType", fieldType));
    }

    /// <summary>
    /// Obtener información del constraint actual
    /// </summary>
    public async Task<string> GetConstraintDefinitionAsync()
    {
        try
        {
            var sql = @"
                SELECT cc.definition AS ConstraintDefinition
                FROM sys.check_constraints cc
                INNER JOIN sys.tables t ON cc.parent_object_id = t.object_id
                WHERE cc.name = 'CK_system_custom_field_definitions_field_type'
                  AND t.name = 'system_custom_field_definitions'";

            var result = await _context.Database
                .SqlQueryRaw<string>(sql)
                .FirstOrDefaultAsync();

            return result ?? "Constraint no encontrado";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo definición del constraint");
            return $"Error: {ex.Message}";
        }
    }
}