-- ========================================
-- 🔧 MIGRACIÓN: Agregar tipos de campos de referencia
-- ========================================
-- Descripción: Actualiza el CHECK constraint para permitir los nuevos tipos:
--              entity_reference, user_reference, file_reference
-- Fecha: 2025-09-19
-- Autor: Sistema FormDesigner
-- ========================================

PRINT '🔧 Iniciando migración: Agregar tipos de campos de referencia...';

-- ========================================
-- 1. ELIMINAR CONSTRAINT EXISTENTE
-- ========================================
PRINT '📋 Eliminando constraint CK_system_custom_field_definitions_field_type...';

IF EXISTS (SELECT 1 FROM sys.check_constraints
           WHERE name = 'CK_system_custom_field_definitions_field_type')
BEGIN
    ALTER TABLE system_custom_field_definitions
    DROP CONSTRAINT CK_system_custom_field_definitions_field_type;

    PRINT '✅ Constraint CK_system_custom_field_definitions_field_type eliminado';
END
ELSE
BEGIN
    PRINT '📄 Constraint CK_system_custom_field_definitions_field_type no existe';
END

-- ========================================
-- 2. CREAR NUEVO CONSTRAINT CON TIPOS ADICIONALES
-- ========================================
PRINT '📋 Creando nuevo constraint con tipos de referencia...';

ALTER TABLE system_custom_field_definitions
ADD CONSTRAINT CK_system_custom_field_definitions_field_type
    CHECK (FieldType IN (
        'text', 'textarea', 'number', 'date', 'boolean', 'select', 'multiselect',
        'entity_reference', 'user_reference', 'file_reference'
    ));

PRINT '✅ Nuevo constraint CK_system_custom_field_definitions_field_type creado';

-- ========================================
-- 3. VERIFICAR MIGRACIÓN
-- ========================================
PRINT '🔍 Verificando migración...';

-- Verificar que el constraint existe
IF EXISTS (SELECT 1 FROM sys.check_constraints
           WHERE name = 'CK_system_custom_field_definitions_field_type')
BEGIN
    PRINT '✅ Constraint verificado exitosamente';

    -- Mostrar definición del constraint
    SELECT
        cc.name AS ConstraintName,
        cc.definition AS ConstraintDefinition
    FROM sys.check_constraints cc
    INNER JOIN sys.tables t ON cc.parent_object_id = t.object_id
    WHERE cc.name = 'CK_system_custom_field_definitions_field_type'
      AND t.name = 'system_custom_field_definitions';

    PRINT '📊 Definición del constraint mostrada arriba';
END
ELSE
BEGIN
    PRINT '❌ ERROR: El constraint no fue creado correctamente';
    RAISERROR('La migración falló al crear el constraint', 16, 1);
END

-- ========================================
-- 4. PRUEBA DE VALIDACIÓN
-- ========================================
PRINT '🧪 Ejecutando pruebas de validación...';

-- Variables de prueba
DECLARE @TestEntityName NVARCHAR(100) = 'TestEntity_' + CAST(NEWID() AS NVARCHAR(36));
DECLARE @TestFieldName NVARCHAR(100) = 'TestField_' + CAST(NEWID() AS NVARCHAR(36));

-- Prueba 1: Insertar tipo entity_reference
BEGIN TRY
    INSERT INTO system_custom_field_definitions (
        EntityName, FieldName, DisplayName, FieldType, Active
    ) VALUES (
        @TestEntityName + '_1', @TestFieldName + '_1', 'Test Entity Reference', 'entity_reference', 1
    );
    PRINT '✅ Prueba 1 exitosa: entity_reference aceptado';
END TRY
BEGIN CATCH
    PRINT '❌ Prueba 1 falló: entity_reference rechazado - ' + ERROR_MESSAGE();
END CATCH

-- Prueba 2: Insertar tipo user_reference
BEGIN TRY
    INSERT INTO system_custom_field_definitions (
        EntityName, FieldName, DisplayName, FieldType, Active
    ) VALUES (
        @TestEntityName + '_2', @TestFieldName + '_2', 'Test User Reference', 'user_reference', 1
    );
    PRINT '✅ Prueba 2 exitosa: user_reference aceptado';
END TRY
BEGIN CATCH
    PRINT '❌ Prueba 2 falló: user_reference rechazado - ' + ERROR_MESSAGE();
END CATCH

-- Prueba 3: Insertar tipo file_reference
BEGIN TRY
    INSERT INTO system_custom_field_definitions (
        EntityName, FieldName, DisplayName, FieldType, Active
    ) VALUES (
        @TestEntityName + '_3', @TestFieldName + '_3', 'Test File Reference', 'file_reference', 1
    );
    PRINT '✅ Prueba 3 exitosa: file_reference aceptado';
END TRY
BEGIN CATCH
    PRINT '❌ Prueba 3 falló: file_reference rechazado - ' + ERROR_MESSAGE();
END CATCH

-- Prueba 4: Rechazar tipo inválido
BEGIN TRY
    INSERT INTO system_custom_field_definitions (
        EntityName, FieldName, DisplayName, FieldType, Active
    ) VALUES (
        @TestEntityName + '_invalid', @TestFieldName + '_invalid', 'Test Invalid', 'invalid_type', 1
    );
    PRINT '❌ Prueba 4 falló: invalid_type fue aceptado (debería rechazarse)';
END TRY
BEGIN CATCH
    PRINT '✅ Prueba 4 exitosa: invalid_type correctamente rechazado';
END CATCH

-- Limpiar datos de prueba
DELETE FROM system_custom_field_definitions
WHERE EntityName LIKE @TestEntityName + '%';

PRINT '🧹 Datos de prueba eliminados';

-- ========================================
-- 5. RESUMEN DE MIGRACIÓN
-- ========================================
PRINT '';
PRINT '========================================';
PRINT '✅ MIGRACIÓN COMPLETADA EXITOSAMENTE';
PRINT '========================================';
PRINT '';
PRINT '🔧 Cambios realizados:';
PRINT '   • CHECK constraint CK_system_custom_field_definitions_field_type actualizado';
PRINT '   • Tipos agregados: entity_reference, user_reference, file_reference';
PRINT '   • Tipos existentes mantenidos: text, textarea, number, date, boolean, select, multiselect';
PRINT '';
PRINT '🎯 Nuevos tipos disponibles:';
PRINT '   📎 entity_reference - Referencia a otra entidad del sistema';
PRINT '   👤 user_reference   - Referencia a usuario del sistema';
PRINT '   📁 file_reference   - Referencia a archivo/documento';
PRINT '';
PRINT '⚡ El FormDesigner ahora puede crear campos de referencia sin errores de constraint';
PRINT '========================================';