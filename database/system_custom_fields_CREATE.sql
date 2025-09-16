-- ========================================
-- SYSTEM CUSTOM FIELDS - CREATE SCRIPT
-- ========================================
-- Este script crea todas las tablas y estructuras necesarias para el sistema de campos personalizados
-- Usar nomenclatura system_xxxxx para consistencia con el sistema existente

PRINT '🚀 Iniciando creación del sistema de campos personalizados...';

-- ========================================
-- 1. TABLA PRINCIPAL: system_custom_field_definitions
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_custom_field_definitions' AND xtype='U')
BEGIN
    PRINT '📋 Creando tabla system_custom_field_definitions...';

    CREATE TABLE system_custom_field_definitions (
        -- Identificador único
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,

        -- Información básica del campo
        EntityName NVARCHAR(100) NOT NULL,           -- "Empleado", "Empresa", etc.
        FieldName NVARCHAR(100) NOT NULL,            -- "telefono_emergencia", "nivel_ingles"
        DisplayName NVARCHAR(255) NOT NULL,          -- "Teléfono de Emergencia"
        Description NVARCHAR(500) NULL,              -- Descripción/ayuda

        -- Configuración del tipo de campo
        FieldType NVARCHAR(50) NOT NULL DEFAULT 'text', -- "text", "number", "date", "boolean", "select", "multiselect", "textarea"
        IsRequired BIT DEFAULT 0 NOT NULL,           -- Campo requerido u opcional
        DefaultValue NVARCHAR(MAX) NULL,             -- Valor por defecto (JSON)
        SortOrder INT DEFAULT 0 NOT NULL,            -- Orden de aparición

        -- Configuraciones avanzadas (JSON)
        ValidationConfig NVARCHAR(MAX) NULL,         -- JSON: validaciones específicas
        UIConfig NVARCHAR(MAX) NULL,                 -- JSON: configuración de UI
        ConditionsConfig NVARCHAR(MAX) NULL,         -- JSON: condiciones show_if, required_if, etc.

        -- Permisos granulares
        PermissionCreate NVARCHAR(255) NULL,         -- "EMPLEADO.TELEFONO_EMERGENCIA.CREATE"
        PermissionUpdate NVARCHAR(255) NULL,         -- "EMPLEADO.TELEFONO_EMERGENCIA.UPDATE"
        PermissionView NVARCHAR(255) NULL,           -- "EMPLEADO.TELEFONO_EMERGENCIA.VIEW"

        -- Metadatos
        IsEnabled BIT DEFAULT 1 NOT NULL,            -- Campo habilitado/deshabilitado
        Version INT DEFAULT 1 NOT NULL,              -- Versión del campo (para migraciones)
        Tags NVARCHAR(500) NULL,                     -- Tags para categorización

        -- Auditoría estándar (BaseEntity pattern)
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NULL,
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        Active BIT DEFAULT 1 NOT NULL,

        -- Constraints
        CONSTRAINT CK_system_custom_field_definitions_field_type
            CHECK (FieldType IN ('text', 'textarea', 'number', 'date', 'boolean', 'select', 'multiselect')),

        CONSTRAINT CK_system_custom_field_definitions_sort_order
            CHECK (SortOrder >= 0),

        CONSTRAINT CK_system_custom_field_definitions_version
            CHECK (Version > 0),

        -- Unique constraint: Un campo por entidad por organización
        CONSTRAINT UQ_system_custom_field_definitions_entity_field_org
            UNIQUE (EntityName, FieldName, OrganizationId)
    );

    PRINT '✅ Tabla system_custom_field_definitions creada exitosamente';
END
ELSE
BEGIN
    PRINT '⚠️ Tabla system_custom_field_definitions ya existe';
END
GO

-- ========================================
-- 2. ÍNDICES PARA PERFORMANCE
-- ========================================
PRINT '📊 Creando índices optimizados...';

-- Índice principal para consultas por entidad y organización
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_system_custom_field_definitions_entity_org_active')
BEGIN
    CREATE NONCLUSTERED INDEX IX_system_custom_field_definitions_entity_org_active
        ON system_custom_field_definitions(EntityName, OrganizationId, Active, IsEnabled)
        INCLUDE (Id, FieldName, DisplayName, FieldType, SortOrder, IsRequired);
    PRINT '✅ Índice IX_system_custom_field_definitions_entity_org_active creado';
END

-- Índice para búsquedas por nombre de campo
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_system_custom_field_definitions_field_name')
BEGIN
    CREATE NONCLUSTERED INDEX IX_system_custom_field_definitions_field_name
        ON system_custom_field_definitions(FieldName, EntityName, OrganizationId)
        WHERE Active = 1 AND IsEnabled = 1;
    PRINT '✅ Índice IX_system_custom_field_definitions_field_name creado';
END

-- Índice para ordenamiento en formularios
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_system_custom_field_definitions_sort_order')
BEGIN
    CREATE NONCLUSTERED INDEX IX_system_custom_field_definitions_sort_order
        ON system_custom_field_definitions(EntityName, OrganizationId, SortOrder, Active)
        INCLUDE (FieldName, DisplayName, FieldType);
    PRINT '✅ Índice IX_system_custom_field_definitions_sort_order creado';
END

-- ========================================
-- 3. TABLA DE AUDITORÍA: system_custom_field_audit_log
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_custom_field_audit_log' AND xtype='U')
BEGIN
    PRINT '📋 Creando tabla system_custom_field_audit_log...';

    CREATE TABLE system_custom_field_audit_log (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,

        -- Referencia al campo modificado
        CustomFieldDefinitionId UNIQUEIDENTIFIER NOT NULL,
        EntityName NVARCHAR(100) NOT NULL,
        FieldName NVARCHAR(100) NOT NULL,

        -- Tipo de cambio
        ChangeType NVARCHAR(50) NOT NULL,            -- "CREATE", "UPDATE", "DELETE", "ENABLE", "DISABLE"

        -- Datos del cambio
        OldDefinition NVARCHAR(MAX) NULL,            -- JSON de la definición anterior
        NewDefinition NVARCHAR(MAX) NULL,            -- JSON de la nueva definición
        ChangedProperties NVARCHAR(1000) NULL,       -- Lista de propiedades que cambiaron
        ChangeReason NVARCHAR(500) NULL,             -- Razón del cambio

        -- Impacto del cambio
        ImpactAssessment NVARCHAR(MAX) NULL,         -- JSON: {"affectedRecords": 1500, "migrationRequired": true}

        -- Auditoría estándar
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NULL,
        CreadorId UNIQUEIDENTIFIER NULL,

        -- Constraints
        CONSTRAINT CK_system_custom_field_audit_log_change_type
            CHECK (ChangeType IN ('CREATE', 'UPDATE', 'DELETE', 'ENABLE', 'DISABLE', 'MIGRATE')),

        -- Foreign Key
        CONSTRAINT FK_system_custom_field_audit_definition
            FOREIGN KEY (CustomFieldDefinitionId)
            REFERENCES system_custom_field_definitions(Id)
            ON DELETE CASCADE
    );

    PRINT '✅ Tabla system_custom_field_audit_log creada exitosamente';
END
ELSE
BEGIN
    PRINT '⚠️ Tabla system_custom_field_audit_log ya existe';
END
GO

-- Índices para auditoría
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_system_custom_field_audit_log_definition')
BEGIN
    CREATE NONCLUSTERED INDEX IX_system_custom_field_audit_log_definition
        ON system_custom_field_audit_log(CustomFieldDefinitionId, FechaCreacion DESC);
    PRINT '✅ Índice IX_system_custom_field_audit_log_definition creado';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_system_custom_field_audit_log_entity_date')
BEGIN
    CREATE NONCLUSTERED INDEX IX_system_custom_field_audit_log_entity_date
        ON system_custom_field_audit_log(EntityName, OrganizationId, FechaCreacion DESC);
    PRINT '✅ Índice IX_system_custom_field_audit_log_entity_date creado';
END

-- ========================================
-- 4. TABLA DE TEMPLATES (FASE FUTURA)
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_custom_field_templates' AND xtype='U')
BEGIN
    PRINT '📋 Creando tabla system_custom_field_templates...';

    CREATE TABLE system_custom_field_templates (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,

        -- Información del template
        TemplateName NVARCHAR(255) NOT NULL,         -- "Datos de Contacto Extendidos"
        Description NVARCHAR(1000) NULL,             -- Descripción del template
        Category NVARCHAR(100) NULL,                 -- "RRHH", "Ventas", "Administración"

        -- Configuración del template
        TargetEntityName NVARCHAR(100) NOT NULL,     -- Entidad objetivo
        FieldsDefinition NVARCHAR(MAX) NOT NULL,     -- JSON con array de definiciones

        -- Metadatos
        IsSystemTemplate BIT DEFAULT 0 NOT NULL,     -- Template del sistema vs usuario
        UsageCount INT DEFAULT 0 NOT NULL,           -- Contador de uso

        -- Auditoría estándar
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NULL,        -- NULL para templates globales
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        Active BIT DEFAULT 1 NOT NULL
    );

    PRINT '✅ Tabla system_custom_field_templates creada exitosamente';
END
GO

-- ========================================
-- 5. FUNCIONES DE UTILIDAD
-- ========================================
PRINT '🔧 Creando funciones de utilidad...';

-- Función para validar JSON de configuración
CREATE OR ALTER FUNCTION fn_ValidateCustomFieldConfig(
    @FieldType NVARCHAR(50),
    @ValidationConfig NVARCHAR(MAX),
    @UIConfig NVARCHAR(MAX),
    @ConditionsConfig NVARCHAR(MAX)
)
RETURNS BIT
AS
BEGIN
    DECLARE @IsValid BIT = 1;

    -- Validar que los JSON sean válidos
    IF @ValidationConfig IS NOT NULL AND ISJSON(@ValidationConfig) = 0
        SET @IsValid = 0;

    IF @UIConfig IS NOT NULL AND ISJSON(@UIConfig) = 0
        SET @IsValid = 0;

    IF @ConditionsConfig IS NOT NULL AND ISJSON(@ConditionsConfig) = 0
        SET @IsValid = 0;

    -- Validaciones específicas por tipo de campo
    IF @FieldType = 'select' OR @FieldType = 'multiselect'
    BEGIN
        -- Verificar que UIConfig tenga options
        IF @UIConfig IS NULL OR JSON_VALUE(@UIConfig, '$.options') IS NULL
            SET @IsValid = 0;
    END

    RETURN @IsValid;
END
GO

PRINT '✅ Función fn_ValidateCustomFieldConfig creada';

-- ========================================
-- 6. ÍNDICES PARA CAMPO CUSTOM EN ENTIDADES PRINCIPALES
-- ========================================
PRINT '📊 Creando índices optimizados para campo Custom...';

-- Empleados - Índice para consultas JSON
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Empleados') AND
   NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Empleados_custom_not_null')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Empleados_custom_not_null
        ON Empleados(Id)
        INCLUDE (Custom)
        WHERE Custom IS NOT NULL AND Active = 1;
    PRINT '✅ Índice IX_Empleados_custom_not_null creado';
END

-- Empresas - Índice para consultas JSON
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Empresas') AND
   NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Empresas_custom_not_null')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Empresas_custom_not_null
        ON Empresas(Id)
        INCLUDE (Custom)
        WHERE Custom IS NOT NULL AND Active = 1;
    PRINT '✅ Índice IX_Empresas_custom_not_null creado';
END

-- ========================================
-- 7. DATOS DE PRUEBA (SOLO DESARROLLO)
-- ========================================
-- Solo ejecutar en desarrollo para testing
IF EXISTS (SELECT * FROM sys.databases WHERE name = DB_NAME() AND name LIKE '%_Dev%')
BEGIN
    PRINT '🧪 Insertando datos de prueba (solo en desarrollo)...';

    -- Ejemplo de campo personalizado para empleados
    IF NOT EXISTS (SELECT * FROM system_custom_field_definitions WHERE FieldName = 'telefono_emergencia')
    BEGIN
        INSERT INTO system_custom_field_definitions
        (EntityName, FieldName, DisplayName, Description, FieldType, IsRequired, SortOrder, ValidationConfig, UIConfig, Active)
        VALUES
        ('Empleado', 'telefono_emergencia', 'Teléfono de Emergencia', 'Teléfono de contacto en caso de emergencia', 'text', 1, 1,
         '{"minLength": 9, "maxLength": 15, "pattern": "^[+]?[0-9]{9,15}$", "patternMessage": "Formato inválido. Ej: +56912345678"}',
         '{"placeholder": "Ej: +56912345678", "helpText": "Incluir código de país"}',
         1);

        PRINT '✅ Campo de prueba telefono_emergencia creado';
    END
END

-- ========================================
-- 8. RESUMEN Y VERIFICACIÓN
-- ========================================
PRINT '🎯 Verificando instalación...';

DECLARE @TablesCreated INT = 0;
DECLARE @IndexesCreated INT = 0;

-- Contar tablas creadas
SELECT @TablesCreated = COUNT(*) FROM sys.tables WHERE name IN (
    'system_custom_field_definitions',
    'system_custom_field_audit_log',
    'system_custom_field_templates'
);

-- Contar índices creados
SELECT @IndexesCreated = COUNT(*) FROM sys.indexes WHERE name LIKE 'IX_system_custom_field%';

PRINT '📊 RESUMEN DE INSTALACIÓN:';
PRINT '   📋 Tablas creadas: ' + CAST(@TablesCreated AS VARCHAR(10));
PRINT '   📊 Índices creados: ' + CAST(@IndexesCreated AS VARCHAR(10));
PRINT '   🔧 Funciones creadas: 1';

IF @TablesCreated >= 2 -- Mínimo las 2 tablas principales
BEGIN
    PRINT '✅ ¡Instalación del sistema de campos personalizados COMPLETADA exitosamente!';
    PRINT '';
    PRINT '🚀 PRÓXIMOS PASOS:';
    PRINT '   1. Configurar CustomFields.API';
    PRINT '   2. Implementar componentes Blazor';
    PRINT '   3. Integrar con formularios existentes';
    PRINT '   4. Probar con campo de texto simple';
    PRINT '';
    PRINT '📖 Ver documentación en: custom-fields/';
END
ELSE
BEGIN
    PRINT '❌ Error en la instalación. Verificar logs anteriores.';
END

PRINT '🏁 Script completado.';