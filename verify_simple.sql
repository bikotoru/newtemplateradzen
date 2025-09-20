-- ========================================
-- 🔍 VERIFICACIÓN SIMPLE DE MIGRACIÓN
-- ========================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

USE AgendaGesV3;

PRINT '🔍 Verificando migración...';

-- Verificar constraint
SELECT
    'CONSTRAINT ACTUAL' as Info,
    cc.definition AS Definicion
FROM sys.check_constraints cc
INNER JOIN sys.tables t ON cc.parent_object_id = t.object_id
WHERE cc.name = 'CK_system_custom_field_definitions_field_type'
  AND t.name = 'system_custom_field_definitions';

-- Prueba entity_reference
PRINT '🧪 Probando entity_reference...';
BEGIN TRY
    INSERT INTO system_custom_field_definitions (EntityName, FieldName, DisplayName, FieldType, Active)
    VALUES ('TestVerify', 'test_entity_ref', 'Test Entity Reference', 'entity_reference', 1);
    PRINT '✅ entity_reference: OK';
    DELETE FROM system_custom_field_definitions WHERE EntityName = 'TestVerify' AND FieldName = 'test_entity_ref';
END TRY
BEGIN CATCH
    PRINT '❌ entity_reference FALLÓ: ' + ERROR_MESSAGE();
END CATCH

-- Prueba user_reference
PRINT 'Probando user_reference...';
BEGIN TRY
    INSERT INTO system_custom_field_definitions (EntityName, FieldName, DisplayName, FieldType, Active)
    VALUES ('TestVerify', 'test_user_ref', 'Test User Reference', 'user_reference', 1);
    PRINT '✅ user_reference: OK';
    DELETE FROM system_custom_field_definitions WHERE EntityName = 'TestVerify' AND FieldName = 'test_user_ref';
END TRY
BEGIN CATCH
    PRINT '❌ user_reference FALLÓ: ' + ERROR_MESSAGE();
END CATCH

-- Prueba file_reference
PRINT 'Probando file_reference...';
BEGIN TRY
    INSERT INTO system_custom_field_definitions (EntityName, FieldName, DisplayName, FieldType, Active)
    VALUES ('TestVerify', 'test_file_ref', 'Test File Reference', 'file_reference', 1);
    PRINT '✅ file_reference: OK';
    DELETE FROM system_custom_field_definitions WHERE EntityName = 'TestVerify' AND FieldName = 'test_file_ref';
END TRY
BEGIN CATCH
    PRINT '❌ file_reference FALLÓ: ' + ERROR_MESSAGE();
END CATCH

-- Prueba tipo inválido (debe fallar)
PRINT 'Probando tipo inválido...';
BEGIN TRY
    INSERT INTO system_custom_field_definitions (EntityName, FieldName, DisplayName, FieldType, Active)
    VALUES ('TestVerify', 'test_invalid', 'Test Invalid', 'invalid_type', 1);
    PRINT '❌ PROBLEMA: invalid_type fue aceptado';
END TRY
BEGIN CATCH
    PRINT '✅ invalid_type correctamente rechazado';
END CATCH

PRINT '';
PRINT '========================================';
PRINT '✅ VERIFICACIÓN COMPLETADA';
PRINT '========================================';