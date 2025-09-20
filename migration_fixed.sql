-- ========================================
-- 🔧 MIGRACIÓN CORREGIDA: Agregar tipos de campos de referencia
-- ========================================

-- Configurar opciones SET necesarias
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

USE AgendaGesV3;

PRINT '🔧 Iniciando migración: Agregar tipos de campos de referencia...';

-- ========================================
-- 1. VERIFICAR CONSTRAINT ACTUAL
-- ========================================
PRINT '🔍 Verificando constraint actual...';

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_system_custom_field_definitions_field_type')
BEGIN
    PRINT 'Constraint actual encontrado';
END
ELSE
BEGIN
    PRINT 'ERROR: Constraint no existe';
END

-- ========================================
-- 2. ELIMINAR CONSTRAINT EXISTENTE
-- ========================================
PRINT '📋 Eliminando constraint existente...';

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_system_custom_field_definitions_field_type')
BEGIN
    ALTER TABLE system_custom_field_definitions DROP CONSTRAINT CK_system_custom_field_definitions_field_type;
    PRINT '✅ Constraint eliminado exitosamente';
END

-- ========================================
-- 3. CREAR NUEVO CONSTRAINT
-- ========================================
PRINT '📋 Creando nuevo constraint...';

ALTER TABLE system_custom_field_definitions
ADD CONSTRAINT CK_system_custom_field_definitions_field_type
    CHECK (FieldType IN (
        'text', 'textarea', 'number', 'date', 'boolean', 'select', 'multiselect',
        'entity_reference', 'user_reference', 'file_reference'
    ));

PRINT '✅ Nuevo constraint creado exitosamente';

-- ========================================
-- 4. VERIFICAR NUEVO CONSTRAINT
-- ========================================
PRINT '🔍 Verificando nuevo constraint...';

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_system_custom_field_definitions_field_type')
BEGIN
    PRINT '✅ Constraint verificado exitosamente';
END
ELSE
BEGIN
    PRINT '❌ ERROR: Constraint no creado';
END

-- ========================================
-- 5. PRUEBA SIMPLE
-- ========================================
PRINT '🧪 Ejecutando prueba simple...';

DECLARE @TestEntity NVARCHAR(100) = 'TestMigration';
DECLARE @TestField NVARCHAR(100) = 'TestField';

-- Prueba entity_reference
BEGIN TRY
    INSERT INTO system_custom_field_definitions (EntityName, FieldName, DisplayName, FieldType, Active)
    VALUES (@TestEntity, @TestField, 'Test Entity Reference', 'entity_reference', 1);
    PRINT '✅ Prueba entity_reference: OK';

    -- Limpiar
    DELETE FROM system_custom_field_definitions WHERE EntityName = @TestEntity AND FieldName = @TestField;
END TRY
BEGIN CATCH
    PRINT '❌ Prueba entity_reference falló: ' + ERROR_MESSAGE();
END CATCH

PRINT '';
PRINT '========================================';
PRINT '✅ MIGRACIÓN COMPLETADA';
PRINT '========================================';
PRINT 'Tipos agregados: entity_reference, user_reference, file_reference';
PRINT 'FormDesigner ahora puede crear campos de referencia';
PRINT '========================================';