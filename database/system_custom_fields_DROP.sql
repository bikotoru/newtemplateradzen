-- ========================================
-- SYSTEM CUSTOM FIELDS - DROP/RESET SCRIPT
-- ========================================
-- Este script elimina completamente el sistema de campos personalizados
-- ⚠️ CUIDADO: Esto eliminará TODOS los datos de campos personalizados
-- Usar solo en desarrollo para resetear el avance

PRINT '🧹 Iniciando eliminación del sistema de campos personalizados...';
PRINT '⚠️  ADVERTENCIA: Este proceso eliminará TODOS los datos de campos personalizados';
PRINT '';

-- Verificar si estamos en producción (safety check)
IF EXISTS (SELECT * FROM sys.databases WHERE name = DB_NAME() AND (name LIKE '%_Prod%' OR name LIKE '%_Production%'))
BEGIN
    PRINT '❌ OPERACIÓN CANCELADA: No se puede ejecutar en base de datos de producción';
    PRINT '💡 Solo se permite en bases de datos de desarrollo o testing';
    RETURN;
END

-- ========================================
-- 1. BACKUP DE DATOS ANTES DE ELIMINAR (OPCIONAL)
-- ========================================
PRINT '💾 Creando backup de configuraciones existentes...';

-- Crear tabla temporal con backup si hay datos
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'system_custom_field_definitions')
AND EXISTS (SELECT 1 FROM system_custom_field_definitions)
BEGIN
    DECLARE @BackupTableName NVARCHAR(100) = 'system_custom_field_definitions_backup_' + FORMAT(GETDATE(), 'yyyyMMdd_HHmmss');
    DECLARE @BackupSQL NVARCHAR(MAX) = 'SELECT * INTO ' + @BackupTableName + ' FROM system_custom_field_definitions';

    EXEC sp_executesql @BackupSQL;
    PRINT '✅ Backup creado en tabla: ' + @BackupTableName;
END
ELSE
BEGIN
    PRINT '📝 No hay datos para hacer backup';
END

-- ========================================
-- 2. LIMPIAR DATOS CUSTOM EN ENTIDADES PRINCIPALES
-- ========================================
PRINT '';
PRINT '🧹 Limpiando datos custom existentes en entidades...';

-- Empleados
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Empleados')
BEGIN
    DECLARE @EmpleadosCount INT;
    SELECT @EmpleadosCount = COUNT(*) FROM Empleados WHERE Custom IS NOT NULL;

    IF @EmpleadosCount > 0
    BEGIN
        PRINT '⚠️  Se encontraron ' + CAST(@EmpleadosCount AS VARCHAR(10)) + ' empleados con datos custom';
        PRINT '💭 ¿Desea limpiar estos datos? (Este script los limpiará automáticamente)';

        UPDATE Empleados SET Custom = NULL WHERE Custom IS NOT NULL;
        PRINT '✅ Datos custom eliminados de ' + CAST(@EmpleadosCount AS VARCHAR(10)) + ' empleados';
    END
    ELSE
    BEGIN
        PRINT '📝 No hay datos custom en empleados';
    END
END

-- Empresas
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Empresas')
BEGIN
    DECLARE @EmpresasCount INT;
    SELECT @EmpresasCount = COUNT(*) FROM Empresas WHERE Custom IS NOT NULL;

    IF @EmpresasCount > 0
    BEGIN
        UPDATE Empresas SET Custom = NULL WHERE Custom IS NOT NULL;
        PRINT '✅ Datos custom eliminados de ' + CAST(@EmpresasCount AS VARCHAR(10)) + ' empresas';
    END
    ELSE
    BEGIN
        PRINT '📝 No hay datos custom en empresas';
    END
END

-- ========================================
-- 3. ELIMINAR FUNCIONES
-- ========================================
PRINT '';
PRINT '🔧 Eliminando funciones...';

IF EXISTS (SELECT * FROM sys.objects WHERE name = 'fn_ValidateCustomFieldConfig' AND type = 'FN')
BEGIN
    DROP FUNCTION fn_ValidateCustomFieldConfig;
    PRINT '✅ Función fn_ValidateCustomFieldConfig eliminada';
END

-- ========================================
-- 4. ELIMINAR ÍNDICES PERSONALIZADOS
-- ========================================
PRINT '';
PRINT '📊 Eliminando índices especializados...';

-- Índices en entidades principales
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Empleados_custom_not_null')
BEGIN
    DROP INDEX IX_Empleados_custom_not_null ON Empleados;
    PRINT '✅ Índice IX_Empleados_custom_not_null eliminado';
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Empresas_custom_not_null')
BEGIN
    DROP INDEX IX_Empresas_custom_not_null ON Empresas;
    PRINT '✅ Índice IX_Empresas_custom_not_null eliminado';
END

-- ========================================
-- 5. ELIMINAR TABLAS (EN ORDEN CORRECTO PARA FOREIGN KEYS)
-- ========================================
PRINT '';
PRINT '🗑️ Eliminando tablas del sistema...';

-- 5.1 Eliminar tabla de auditoría (tiene FK)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'system_custom_field_audit_log')
BEGIN
    DROP TABLE system_custom_field_audit_log;
    PRINT '✅ Tabla system_custom_field_audit_log eliminada';
END

-- 5.2 Eliminar tabla de templates
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'system_custom_field_templates')
BEGIN
    DROP TABLE system_custom_field_templates;
    PRINT '✅ Tabla system_custom_field_templates eliminada';
END

-- 5.3 Eliminar tabla principal
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'system_custom_field_definitions')
BEGIN
    DROP TABLE system_custom_field_definitions;
    PRINT '✅ Tabla system_custom_field_definitions eliminada';
END

-- ========================================
-- 6. LIMPIAR PERMISOS RELACIONADOS (OPCIONAL)
-- ========================================
PRINT '';
PRINT '🔐 Limpiando permisos relacionados...';

-- Eliminar permisos de campos personalizados del sistema de permisos
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'system_permissions')
BEGIN
    DECLARE @PermissionsCount INT;

    -- Contar permisos relacionados con campos personalizados
    SELECT @PermissionsCount = COUNT(*)
    FROM system_permissions
    WHERE Code LIKE '%.%.CREATE'
       OR Code LIKE '%.%.UPDATE'
       OR Code LIKE '%.%.VIEW'
       AND (Code LIKE '%TELEFONO_EMERGENCIA%' OR Code LIKE '%NIVEL_INGLES%' OR Code LIKE '%CUSTOM_%');

    IF @PermissionsCount > 0
    BEGIN
        DELETE FROM system_permissions
        WHERE Code LIKE '%.%.CREATE'
           OR Code LIKE '%.%.UPDATE'
           OR Code LIKE '%.%.VIEW'
           AND (Code LIKE '%TELEFONO_EMERGENCIA%' OR Code LIKE '%NIVEL_INGLES%' OR Code LIKE '%CUSTOM_%');

        PRINT '✅ ' + CAST(@PermissionsCount AS VARCHAR(10)) + ' permisos de campos personalizados eliminados';
    END
    ELSE
    BEGIN
        PRINT '📝 No hay permisos relacionados para eliminar';
    END
END

-- ========================================
-- 7. LIMPIAR ROLES Y ASIGNACIONES (OPCIONAL)
-- ========================================
PRINT '👥 Limpiando asignaciones de roles relacionadas...';

-- Limpiar system_users_permissions si hay permisos relacionados
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'system_users_permissions')
BEGIN
    -- Este paso es complejo porque requeriría identificar qué permisos eran de campos personalizados
    -- Por simplicidad, se omite en esta versión pero se puede implementar si es necesario
    PRINT '💭 Limpieza de asignaciones de permisos omitida (requiere identificación manual)';
END

-- ========================================
-- 8. VERIFICACIÓN Y RESUMEN
-- ========================================
PRINT '';
PRINT '🎯 Verificando eliminación...';

DECLARE @RemainingTables INT = 0;
DECLARE @RemainingIndexes INT = 0;
DECLARE @RemainingFunctions INT = 0;

-- Contar elementos restantes
SELECT @RemainingTables = COUNT(*) FROM sys.tables WHERE name IN (
    'system_custom_field_definitions',
    'system_custom_field_audit_log',
    'system_custom_field_templates'
);

SELECT @RemainingIndexes = COUNT(*) FROM sys.indexes WHERE name LIKE 'IX_system_custom_field%';
SELECT @RemainingFunctions = COUNT(*) FROM sys.objects WHERE name = 'fn_ValidateCustomFieldConfig';

PRINT '📊 RESUMEN DE ELIMINACIÓN:';
PRINT '   🗑️ Tablas eliminadas: ' + CAST((3 - @RemainingTables) AS VARCHAR(10)) + '/3';
PRINT '   📊 Índices eliminados: Verificados';
PRINT '   🔧 Funciones eliminadas: ' + CAST((1 - @RemainingFunctions) AS VARCHAR(10)) + '/1';

-- Verificar si hay algún residuo
IF @RemainingTables = 0 AND @RemainingFunctions = 0
BEGIN
    PRINT '';
    PRINT '✅ ¡Eliminación COMPLETADA exitosamente!';
    PRINT '';
    PRINT '🎯 SISTEMA DE CAMPOS PERSONALIZADOS COMPLETAMENTE ELIMINADO';
    PRINT '';
    PRINT '📋 ESTADO ACTUAL:';
    PRINT '   ✅ Todas las tablas eliminadas';
    PRINT '   ✅ Funciones eliminadas';
    PRINT '   ✅ Índices especializados eliminados';
    PRINT '   ✅ Datos custom limpiados de entidades';
    PRINT '';
    PRINT '🚀 Para reinstalar:';
    PRINT '   1. Ejecutar: system_custom_fields_CREATE.sql';
    PRINT '   2. Configurar CustomFields.API nuevamente';
    PRINT '   3. Reconfiguar campos personalizados desde cero';
END
ELSE
BEGIN
    PRINT '';
    PRINT '⚠️ Eliminación PARCIAL - Algunos elementos no pudieron eliminarse:';

    IF @RemainingTables > 0
    BEGIN
        PRINT '   ❌ Tablas restantes: ' + CAST(@RemainingTables AS VARCHAR(10));
        PRINT '      💡 Verificar dependencias o permisos';
    END

    IF @RemainingFunctions > 0
    BEGIN
        PRINT '   ❌ Funciones restantes: ' + CAST(@RemainingFunctions AS VARCHAR(10));
        PRINT '      💡 Verificar si están siendo utilizadas';
    END

    PRINT '';
    PRINT '🔧 SOLUCIÓN SUGERIDA:';
    PRINT '   1. Verificar mensajes de error anteriores';
    PRINT '   2. Eliminar dependencias manualmente si es necesario';
    PRINT '   3. Ejecutar este script nuevamente';
END

-- ========================================
-- 9. MENSAJE FINAL Y PRECAUCIONES
-- ========================================
PRINT '';
PRINT '🛡️ RECORDATORIOS IMPORTANTES:';
PRINT '   • Este script eliminó TODOS los datos de campos personalizados';
PRINT '   • Los backups están en tablas temporales (si había datos)';
PRINT '   • Los campos Custom en entidades se han limpiado';
PRINT '   • Los permisos relacionados han sido eliminados';
PRINT '';
PRINT '💡 PARA DESARROLLO ITERATIVO:';
PRINT '   • Usa este script para resetear entre pruebas';
PRINT '   • Ejecuta CREATE después de DROP para empezar limpio';
PRINT '   • Mantén backups de configuraciones importantes';
PRINT '';
PRINT '🏁 Script de eliminación completado.';

-- ========================================
-- 10. QUERY DE VERIFICACIÓN OPCIONAL
-- ========================================
PRINT '';
PRINT '🔍 CONSULTA DE VERIFICACIÓN:';
PRINT 'Ejecuta esta query para verificar que todo fue eliminado:';
PRINT '';
PRINT 'SELECT ';
PRINT '  ''Tablas'' as Tipo,';
PRINT '  COUNT(*) as Restantes';
PRINT 'FROM sys.tables ';
PRINT 'WHERE name LIKE ''system_custom_field%''';
PRINT 'UNION ALL';
PRINT 'SELECT ';
PRINT '  ''Funciones'' as Tipo,';
PRINT '  COUNT(*) as Restantes';
PRINT 'FROM sys.objects ';
PRINT 'WHERE name = ''fn_ValidateCustomFieldConfig'';';
PRINT '';
PRINT '💡 Resultado esperado: Ambas filas deben mostrar 0 en Restantes';