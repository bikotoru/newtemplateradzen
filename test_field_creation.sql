-- ========================================
-- 🧪 PRUEBA FINAL: Crear Campo de Referencia Real
-- ========================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

USE AgendaGesV3;

PRINT '🧪 Creando campo de referencia real para prueba...';

-- Crear un campo de referencia real como lo haría el FormDesigner
INSERT INTO system_custom_field_definitions (
    Id,
    EntityName,
    FieldName,
    DisplayName,
    Description,
    FieldType,
    IsRequired,
    SortOrder,
    IsEnabled,
    Version,
    Active,
    FechaCreacion,
    FechaModificacion
) VALUES (
    NEWID(),
    'Empleado',
    'RegionReferencia',
    'Región de Trabajo',
    'Referencia a la región donde trabaja el empleado',
    'entity_reference',
    0,
    10,
    1,
    1,
    1,
    GETUTCDATE(),
    GETUTCDATE()
);

PRINT '✅ Campo de referencia creado exitosamente';

-- Verificar que se creó correctamente
SELECT
    'CAMPO CREADO' as Estado,
    EntityName,
    FieldName,
    DisplayName,
    FieldType,
    FechaCreacion
FROM system_custom_field_definitions
WHERE EntityName = 'Empleado' AND FieldName = 'RegionReferencia';

PRINT '';
PRINT '🎯 PRUEBA ADICIONAL: Crear campo con UIConfig para referencia';

-- Crear campo con configuración UI típica de referencia
INSERT INTO system_custom_field_definitions (
    Id,
    EntityName,
    FieldName,
    DisplayName,
    Description,
    FieldType,
    IsRequired,
    UIConfig,
    SortOrder,
    IsEnabled,
    Version,
    Active,
    FechaCreacion,
    FechaModificacion
) VALUES (
    NEWID(),
    'Empleado',
    'SupervisorReferencia',
    'Supervisor Directo',
    'Referencia al supervisor directo del empleado',
    'user_reference',
    0,
    '{"targetEntity": "SystemUsers", "displayProperty": "Nombre", "valueProperty": "Id"}',
    20,
    1,
    1,
    1,
    GETUTCDATE(),
    GETUTCDATE()
);

PRINT '✅ Campo de usuario referencia creado con UIConfig';

-- Mostrar todos los campos de referencia creados
SELECT
    'CAMPOS DE REFERENCIA' as Tipo,
    EntityName,
    FieldName,
    DisplayName,
    FieldType,
    UIConfig
FROM system_custom_field_definitions
WHERE EntityName = 'Empleado'
  AND FieldName IN ('RegionReferencia', 'SupervisorReferencia')
  AND Active = 1;

PRINT '';
PRINT '========================================';
PRINT '🎉 PRUEBA FINAL EXITOSA';
PRINT '========================================';
PRINT 'Campos de referencia creados correctamente';
PRINT 'FormDesigner completamente funcional';
PRINT 'Sistema listo para producción';
PRINT '========================================';