-- ========================================
-- 🔍 VERIFICAR CAMPOS PERSONALIZADOS
-- ========================================

USE AgendaGesV3;

PRINT '🔍 Verificando campos personalizados para Region...';

-- ========================================
-- 1. CAMPOS PERSONALIZADOS EN Region
-- ========================================
PRINT '';
PRINT '📋 Campos personalizados definidos para Region:';
SELECT
    EntityName,
    FieldName,
    DisplayName,
    FieldType,
    IsRequired,
    SortOrder,
    Active,
    FechaCreacion
FROM system_custom_field_definitions
WHERE EntityName = 'Region'
  AND Active = 1
ORDER BY SortOrder, FieldName;

-- ========================================
-- 2. TODOS LOS CAMPOS PERSONALIZADOS
-- ========================================
PRINT '';
PRINT '📋 Todas las entidades con campos personalizados:';
SELECT
    EntityName,
    COUNT(*) as CantidadCampos,
    STRING_AGG(FieldName, ', ') as Campos
FROM system_custom_field_definitions
WHERE Active = 1
GROUP BY EntityName
ORDER BY EntityName;

-- ========================================
-- 3. CAMPOS DE REFERENCIA CREADOS
-- ========================================
PRINT '';
PRINT '📋 Campos de referencia existentes:';
SELECT
    EntityName,
    FieldName,
    DisplayName,
    FieldType,
    UIConfig,
    FechaCreacion
FROM system_custom_field_definitions
WHERE FieldType IN ('entity_reference', 'user_reference', 'file_reference')
  AND Active = 1
ORDER BY EntityName, FieldName;

-- ========================================
-- 4. VERIFICAR FORMULARIOS
-- ========================================
PRINT '';
PRINT '📋 Verificando formularios existentes:';

-- Esta query puede fallar si la tabla no existe
BEGIN TRY
    SELECT
        EntityName,
        FormName,
        IsDefault,
        IsActive
    FROM form_layouts
    WHERE EntityName = 'Region'
    ORDER BY IsDefault DESC;
END TRY
BEGIN CATCH
    PRINT 'Tabla form_layouts no existe o no es accesible';
END CATCH

PRINT '';
PRINT '========================================';
PRINT '🎯 RESUMEN';
PRINT '========================================';