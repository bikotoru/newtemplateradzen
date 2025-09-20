-- ========================================
-- 🔍 VERIFICAR MIGRACIÓN COMPLETADA
-- ========================================

USE AgendaGesV3;

PRINT '🔍 Verificando migración de tipos de campos de referencia...';
PRINT '';

-- ========================================
-- 1. VERIFICAR CONSTRAINT ACTUAL
-- ========================================
PRINT '📋 Constraint actual:';
SELECT
    cc.name AS ConstraintName,
    cc.definition AS ConstraintDefinition
FROM sys.check_constraints cc
INNER JOIN sys.tables t ON cc.parent_object_id = t.object_id
WHERE cc.name = 'CK_system_custom_field_definitions_field_type'
  AND t.name = 'system_custom_field_definitions';

-- ========================================
-- 2. PRUEBAS COMPLETAS DE TIPOS
-- ========================================
PRINT '';
PRINT '🧪 Ejecutando pruebas completas...';

DECLARE @TestEntity NVARCHAR(100) = 'TestVerify';
DECLARE @TestResults TABLE (
    TipoTested NVARCHAR(50),
    Resultado NVARCHAR(10),
    Mensaje NVARCHAR(200)
);

-- Prueba 1: entity_reference
BEGIN TRY
    INSERT INTO system_custom_field_definitions (EntityName, FieldName, DisplayName, FieldType, Active)
    VALUES (@TestEntity, 'test_entity_ref', 'Test Entity Reference', 'entity_reference', 1);

    INSERT INTO @TestResults VALUES ('entity_reference', 'SUCCESS', 'Campo creado exitosamente');
    DELETE FROM system_custom_field_definitions WHERE EntityName = @TestEntity AND FieldName = 'test_entity_ref';
END TRY
BEGIN CATCH
    INSERT INTO @TestResults VALUES ('entity_reference', 'FAILED', ERROR_MESSAGE());
END CATCH

-- Prueba 2: user_reference
BEGIN TRY
    INSERT INTO system_custom_field_definitions (EntityName, FieldName, DisplayName, FieldType, Active)
    VALUES (@TestEntity, 'test_user_ref', 'Test User Reference', 'user_reference', 1);

    INSERT INTO @TestResults VALUES ('user_reference', 'SUCCESS', 'Campo creado exitosamente');
    DELETE FROM system_custom_field_definitions WHERE EntityName = @TestEntity AND FieldName = 'test_user_ref';
END TRY
BEGIN CATCH
    INSERT INTO @TestResults VALUES ('user_reference', 'FAILED', ERROR_MESSAGE());
END CATCH

-- Prueba 3: file_reference
BEGIN TRY
    INSERT INTO system_custom_field_definitions (EntityName, FieldName, DisplayName, FieldType, Active)
    VALUES (@TestEntity, 'test_file_ref', 'Test File Reference', 'file_reference', 1);

    INSERT INTO @TestResults VALUES ('file_reference', 'SUCCESS', 'Campo creado exitosamente');
    DELETE FROM system_custom_field_definitions WHERE EntityName = @TestEntity AND FieldName = 'test_file_ref';
END TRY
BEGIN CATCH
    INSERT INTO @TestResults VALUES ('file_reference', 'FAILED', ERROR_MESSAGE());
END CATCH

-- Prueba 4: Tipo existente (text)
BEGIN TRY
    INSERT INTO system_custom_field_definitions (EntityName, FieldName, DisplayName, FieldType, Active)
    VALUES (@TestEntity, 'test_text', 'Test Text', 'text', 1);

    INSERT INTO @TestResults VALUES ('text', 'SUCCESS', 'Campo creado exitosamente');
    DELETE FROM system_custom_field_definitions WHERE EntityName = @TestEntity AND FieldName = 'test_text';
END TRY
BEGIN CATCH
    INSERT INTO @TestResults VALUES ('text', 'FAILED', ERROR_MESSAGE());
END CATCH

-- Prueba 5: Tipo inválido (debe fallar)
BEGIN TRY
    INSERT INTO system_custom_field_definitions (EntityName, FieldName, DisplayName, FieldType, Active)
    VALUES (@TestEntity, 'test_invalid', 'Test Invalid', 'invalid_type', 1);

    INSERT INTO @TestResults VALUES ('invalid_type', 'FAILED', 'PROBLEMA: Tipo inválido fue aceptado');
    DELETE FROM system_custom_field_definitions WHERE EntityName = @TestEntity AND FieldName = 'test_invalid';
END TRY
BEGIN CATCH
    INSERT INTO @TestResults VALUES ('invalid_type', 'SUCCESS', 'Correctamente rechazado: ' + ERROR_MESSAGE());
END CATCH

-- ========================================
-- 3. MOSTRAR RESULTADOS
-- ========================================
PRINT '';
PRINT '📊 Resultados de las pruebas:';
SELECT
    TipoTested as 'Tipo de Campo',
    Resultado,
    Mensaje
FROM @TestResults
ORDER BY TipoTested;

-- ========================================
-- 4. RESUMEN FINAL
-- ========================================
DECLARE @Exitosas INT = (SELECT COUNT(*) FROM @TestResults WHERE Resultado = 'SUCCESS');
DECLARE @Fallidas INT = (SELECT COUNT(*) FROM @TestResults WHERE Resultado = 'FAILED');

PRINT '';
PRINT '========================================';
PRINT '📊 RESUMEN DE VERIFICACIÓN';
PRINT '========================================';
PRINT 'Pruebas exitosas: ' + CAST(@Exitosas AS VARCHAR);
PRINT 'Pruebas fallidas: ' + CAST(@Fallidas AS VARCHAR);

IF @Exitosas = 5 AND @Fallidas = 0
BEGIN
    PRINT '';
    PRINT '✅ MIGRACIÓN COMPLETAMENTE EXITOSA';
    PRINT 'Todos los tipos de campo funcionan correctamente';
    PRINT 'FormDesigner listo para usar campos de referencia';
END
ELSE
BEGIN
    PRINT '';
    PRINT '⚠️ HAY PROBLEMAS CON LA MIGRACIÓN';
    PRINT 'Revisar los resultados arriba';
END

PRINT '========================================';