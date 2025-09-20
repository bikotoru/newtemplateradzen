-- ========================================
-- 🔧 MIGRACIÓN PRODUCCIÓN: Agregar tipos de campos de referencia
-- ========================================
-- Base de datos: AgendaGesV3
-- Puerto: 1333
-- ========================================

USE AgendaGesV3;
GO

PRINT '🔧 Iniciando migración: Agregar tipos de campos de referencia...';
PRINT 'Base de datos: AgendaGesV3';
PRINT 'Fecha: ' + CONVERT(VARCHAR, GETDATE(), 120);

-- ========================================
-- 1. VERIFICAR CONSTRAINT ACTUAL
-- ========================================
PRINT '';
PRINT '🔍 Verificando constraint actual...';

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_system_custom_field_definitions_field_type')
BEGIN
    SELECT
        'ACTUAL' as Estado,
        cc.name AS ConstraintName,
        cc.definition AS ConstraintDefinition
    FROM sys.check_constraints cc
    INNER JOIN sys.tables t ON cc.parent_object_id = t.object_id
    WHERE cc.name = 'CK_system_custom_field_definitions_field_type'
      AND t.name = 'system_custom_field_definitions';
END
ELSE
BEGIN
    PRINT '❌ ERROR: Constraint CK_system_custom_field_definitions_field_type no existe';
    SELECT 'ERROR' as Estado, 'Constraint no encontrado' as Mensaje;
END

-- ========================================
-- 2. ELIMINAR CONSTRAINT EXISTENTE
-- ========================================
PRINT '';
PRINT '📋 Eliminando constraint CK_system_custom_field_definitions_field_type...';

BEGIN TRY
    IF EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = 'CK_system_custom_field_definitions_field_type')
    BEGIN
        ALTER TABLE system_custom_field_definitions
        DROP CONSTRAINT CK_system_custom_field_definitions_field_type;

        PRINT '✅ Constraint CK_system_custom_field_definitions_field_type eliminado exitosamente';
    END
    ELSE
    BEGIN
        PRINT '📄 Constraint CK_system_custom_field_definitions_field_type no existe (ya eliminado)';
    END
END TRY
BEGIN CATCH
    PRINT '❌ ERROR eliminando constraint: ' + ERROR_MESSAGE();
    THROW;
END CATCH

-- ========================================
-- 3. CREAR NUEVO CONSTRAINT CON TIPOS ADICIONALES
-- ========================================
PRINT '';
PRINT '📋 Creando nuevo constraint con tipos de referencia...';

BEGIN TRY
    ALTER TABLE system_custom_field_definitions
    ADD CONSTRAINT CK_system_custom_field_definitions_field_type
        CHECK (FieldType IN (
            'text', 'textarea', 'number', 'date', 'boolean', 'select', 'multiselect',
            'entity_reference', 'user_reference', 'file_reference'
        ));

    PRINT '✅ Nuevo constraint CK_system_custom_field_definitions_field_type creado exitosamente';
END TRY
BEGIN CATCH
    PRINT '❌ ERROR creando constraint: ' + ERROR_MESSAGE();
    THROW;
END CATCH

-- ========================================
-- 4. VERIFICAR NUEVO CONSTRAINT
-- ========================================
PRINT '';
PRINT '🔍 Verificando nuevo constraint...';

IF EXISTS (SELECT 1 FROM sys.check_constraints
           WHERE name = 'CK_system_custom_field_definitions_field_type')
BEGIN
    PRINT '✅ Constraint verificado exitosamente';

    -- Mostrar definición del nuevo constraint
    SELECT
        'NUEVO' as Estado,
        cc.name AS ConstraintName,
        cc.definition AS ConstraintDefinition
    FROM sys.check_constraints cc
    INNER JOIN sys.tables t ON cc.parent_object_id = t.object_id
    WHERE cc.name = 'CK_system_custom_field_definitions_field_type'
      AND t.name = 'system_custom_field_definitions';

    PRINT '📊 Definición del nuevo constraint mostrada arriba';
END
ELSE
BEGIN
    PRINT '❌ ERROR: El constraint no fue creado correctamente';
    SELECT 'ERROR' as Estado, 'Constraint no creado' as Mensaje;
    RAISERROR('La migración falló al crear el constraint', 16, 1);
END

-- ========================================
-- 5. PRUEBAS DE VALIDACIÓN
-- ========================================
PRINT '';
PRINT '🧪 Ejecutando pruebas de validación...';

-- Variables de prueba
DECLARE @TestEntityName NVARCHAR(100) = 'TestEntity_Migration_' + REPLACE(CONVERT(VARCHAR, GETDATE(), 120), ':', '');
DECLARE @TestFieldName NVARCHAR(100) = 'TestField_Migration_' + CAST(NEWID() AS NVARCHAR(36));

-- Prueba 1: entity_reference
PRINT '';
PRINT 'Prueba 1: entity_reference...';
BEGIN TRY
    INSERT INTO system_custom_field_definitions (
        EntityName, FieldName, DisplayName, FieldType, Active
    ) VALUES (
        @TestEntityName + '_1', @TestFieldName + '_1', 'Test Entity Reference', 'entity_reference', 1
    );
    PRINT '✅ Prueba 1 EXITOSA: entity_reference aceptado';
END TRY
BEGIN CATCH
    PRINT '❌ Prueba 1 FALLÓ: entity_reference rechazado - ' + ERROR_MESSAGE();
END CATCH

-- Prueba 2: user_reference
PRINT 'Prueba 2: user_reference...';
BEGIN TRY
    INSERT INTO system_custom_field_definitions (
        EntityName, FieldName, DisplayName, FieldType, Active
    ) VALUES (
        @TestEntityName + '_2', @TestFieldName + '_2', 'Test User Reference', 'user_reference', 1
    );
    PRINT '✅ Prueba 2 EXITOSA: user_reference aceptado';
END TRY
BEGIN CATCH
    PRINT '❌ Prueba 2 FALLÓ: user_reference rechazado - ' + ERROR_MESSAGE();
END CATCH

-- Prueba 3: file_reference
PRINT 'Prueba 3: file_reference...';
BEGIN TRY
    INSERT INTO system_custom_field_definitions (
        EntityName, FieldName, DisplayName, FieldType, Active
    ) VALUES (
        @TestEntityName + '_3', @TestFieldName + '_3', 'Test File Reference', 'file_reference', 1
    );
    PRINT '✅ Prueba 3 EXITOSA: file_reference aceptado';
END TRY
BEGIN CATCH
    PRINT '❌ Prueba 3 FALLÓ: file_reference rechazado - ' + ERROR_MESSAGE();
END CATCH

-- Prueba 4: Tipo inválido (debe fallar)
PRINT 'Prueba 4: invalid_type (debe fallar)...';
BEGIN TRY
    INSERT INTO system_custom_field_definitions (
        EntityName, FieldName, DisplayName, FieldType, Active
    ) VALUES (
        @TestEntityName + '_invalid', @TestFieldName + '_invalid', 'Test Invalid', 'invalid_type', 1
    );
    PRINT '❌ Prueba 4 FALLÓ: invalid_type fue aceptado (DEBERÍA ser rechazado)';
END TRY
BEGIN CATCH
    PRINT '✅ Prueba 4 EXITOSA: invalid_type correctamente rechazado';
END CATCH

-- ========================================
-- 6. LIMPIAR DATOS DE PRUEBA
-- ========================================
PRINT '';
PRINT '🧹 Limpiando datos de prueba...';

BEGIN TRY
    DELETE FROM system_custom_field_definitions
    WHERE EntityName LIKE @TestEntityName + '%';

    DECLARE @DeletedRows INT = @@ROWCOUNT;
    PRINT '✅ ' + CAST(@DeletedRows AS VARCHAR) + ' registros de prueba eliminados';
END TRY
BEGIN CATCH
    PRINT '⚠️ Warning limpiando datos: ' + ERROR_MESSAGE();
END CATCH

-- ========================================
-- 7. RESUMEN FINAL
-- ========================================
PRINT '';
PRINT '========================================';
PRINT '✅ MIGRACIÓN COMPLETADA EXITOSAMENTE';
PRINT '========================================';
PRINT '';
PRINT '🔧 Cambios realizados:';
PRINT '   • CHECK constraint actualizado en tabla system_custom_field_definitions';
PRINT '   • Tipos AGREGADOS: entity_reference, user_reference, file_reference';
PRINT '   • Tipos MANTENIDOS: text, textarea, number, date, boolean, select, multiselect';
PRINT '';
PRINT '🎯 Nuevos tipos disponibles:';
PRINT '   📎 entity_reference - Referencia a otra entidad del sistema';
PRINT '   👤 user_reference   - Referencia a usuario del sistema';
PRINT '   📁 file_reference   - Referencia a archivo/documento';
PRINT '';
PRINT '⚡ El FormDesigner ahora puede crear campos de referencia sin errores';
PRINT '🗃️ Base de datos: AgendaGesV3';
PRINT '⏰ Completado: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '========================================';

-- Mostrar resumen final
SELECT
    'MIGRACIÓN COMPLETA' as Estado,
    'AgendaGesV3' as BaseDatos,
    CONVERT(VARCHAR, GETDATE(), 120) as FechaHora,
    'entity_reference, user_reference, file_reference' as TiposAgregados;