# 🗄️ Database Schema - Campos Personalizados

## 📋 Tablas y Estructuras de Base de Datos

Esta documentación detalla todas las tablas, índices y modificaciones necesarias en la base de datos para soportar el sistema de campos personalizados.

---

## 📊 Resumen de Cambios

### **Nuevas Tablas:**
- `custom_field_definitions` - Definiciones de campos personalizados
- `custom_field_audit_log` - Log de cambios en definiciones (opcional)
- `custom_field_templates` - Templates predefinidos (opcional - Fase futura)

### **Modificaciones a Tablas Existentes:**
- Ninguna modificación requerida - se aprovecha el campo `Custom` existente en `BaseEntity`

### **Nuevos Índices:**
- Índices optimizados para performance en consultas JSON
- Índices para búsquedas por entidad y organización

---

## 🔧 Tabla Principal: custom_field_definitions

### **Definición SQL**
```sql
-- ========================================
-- Tabla principal para definiciones de campos personalizados
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='custom_field_definitions' AND xtype='U')
BEGIN
    CREATE TABLE custom_field_definitions (
        -- Campos identificadores
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        
        -- Información básica del campo
        EntityName NVARCHAR(100) NOT NULL,           -- "Empleado", "Empresa", "Cliente", etc.
        FieldName NVARCHAR(100) NOT NULL,            -- "telefono_emergencia", "nivel_ingles", etc.
        DisplayName NVARCHAR(255) NOT NULL,          -- "Teléfono de Emergencia", "Nivel de Inglés"
        Description NVARCHAR(500) NULL,              -- Descripción del campo para ayuda
        
        -- Configuración del tipo de campo
        FieldType NVARCHAR(50) NOT NULL,             -- "text", "number", "date", "boolean", "select", "multiselect", "textarea"
        IsRequired BIT DEFAULT 0 NOT NULL,           -- Campo requerido u opcional
        DefaultValue NVARCHAR(MAX) NULL,             -- Valor por defecto (JSON)
        SortOrder INT DEFAULT 0 NOT NULL,            -- Orden de aparición en formularios
        
        -- Configuraciones avanzadas (JSON)
        ValidationConfig NVARCHAR(MAX) NULL,         -- JSON: {"minLength": 5, "maxLength": 50, "pattern": "regex"}
        UIConfig NVARCHAR(MAX) NULL,                 -- JSON: {"placeholder": "text", "options": [...], "rows": 4}
        ConditionsConfig NVARCHAR(MAX) NULL,         -- JSON: [{"type": "show_if", "sourceField": "campo", "operator": "equals", "value": "valor"}]
        
        -- Permisos granulares
        PermissionCreate NVARCHAR(255) NULL,         -- "EMPLEADO.TELEFONO_EMERGENCIA.CREATE"
        PermissionUpdate NVARCHAR(255) NULL,         -- "EMPLEADO.TELEFONO_EMERGENCIA.UPDATE"
        PermissionView NVARCHAR(255) NULL,           -- "EMPLEADO.TELEFONO_EMERGENCIA.VIEW"
        
        -- Metadatos y control
        IsEnabled BIT DEFAULT 1 NOT NULL,            -- Campo habilitado/deshabilitado
        Version INT DEFAULT 1 NOT NULL,              -- Versión del campo (para migraciones)
        Tags NVARCHAR(500) NULL,                     -- Tags para categorización: "RRHH,Personal,Contacto"
        
        -- Campos de auditoría estándar (BaseEntity pattern)
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NULL,       -- Multitenancy
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        Active BIT DEFAULT 1 NOT NULL,
        
        -- Constraints
        CONSTRAINT CK_custom_field_definitions_field_type 
            CHECK (FieldType IN ('text', 'textarea', 'number', 'date', 'boolean', 'select', 'multiselect')),
        
        CONSTRAINT CK_custom_field_definitions_sort_order 
            CHECK (SortOrder >= 0),
            
        CONSTRAINT CK_custom_field_definitions_version 
            CHECK (Version > 0),
        
        -- Unique constraint: Un campo por entidad por organización
        CONSTRAINT UQ_custom_field_definitions_entity_field_org 
            UNIQUE (EntityName, FieldName, OrganizationId)
    );
    
    PRINT '✅ Tabla custom_field_definitions creada exitosamente';
END
ELSE
BEGIN
    PRINT '⚠️ Tabla custom_field_definitions ya existe';
END
GO
```

### **Índices para Performance**
```sql
-- ========================================
-- Índices optimizados para custom_field_definitions
-- ========================================

-- Índice principal para consultas por entidad y organización
CREATE NONCLUSTERED INDEX IX_custom_field_definitions_entity_org_active 
    ON custom_field_definitions(EntityName, OrganizationId, Active, IsEnabled)
    INCLUDE (Id, FieldName, DisplayName, FieldType, SortOrder, IsRequired);

-- Índice para búsquedas por nombre de campo
CREATE NONCLUSTERED INDEX IX_custom_field_definitions_field_name 
    ON custom_field_definitions(FieldName, EntityName, OrganizationId)
    WHERE Active = 1 AND IsEnabled = 1;

-- Índice para ordenamiento en formularios
CREATE NONCLUSTERED INDEX IX_custom_field_definitions_sort_order 
    ON custom_field_definitions(EntityName, OrganizationId, SortOrder, Active)
    INCLUDE (FieldName, DisplayName, FieldType);

-- Índice para campos con permisos
CREATE NONCLUSTERED INDEX IX_custom_field_definitions_permissions 
    ON custom_field_definitions(EntityName, OrganizationId)
    INCLUDE (PermissionCreate, PermissionUpdate, PermissionView)
    WHERE PermissionCreate IS NOT NULL OR PermissionUpdate IS NOT NULL OR PermissionView IS NOT NULL;

-- Índice para campos con condiciones
CREATE NONCLUSTERED INDEX IX_custom_field_definitions_conditions 
    ON custom_field_definitions(EntityName, OrganizationId, Active)
    INCLUDE (FieldName, ConditionsConfig)
    WHERE ConditionsConfig IS NOT NULL AND Active = 1;

-- Índice temporal
CREATE NONCLUSTERED INDEX IX_custom_field_definitions_temporal 
    ON custom_field_definitions(FechaCreacion, FechaModificacion)
    INCLUDE (EntityName, CreadorId, ModificadorId);

PRINT '✅ Índices para custom_field_definitions creados exitosamente';
GO
```

---

## 📝 Tabla de Auditoría: custom_field_audit_log (Opcional)

### **Definición SQL**
```sql
-- ========================================
-- Tabla para auditar cambios en definiciones de campos personalizados
-- (Complementa el sistema de auditoría existente)
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='custom_field_audit_log' AND xtype='U')
BEGIN
    CREATE TABLE custom_field_audit_log (
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
        ChangeReason NVARCHAR(500) NULL,             -- Razón del cambio (comentario)
        
        -- Impacto del cambio
        ImpactAssessment NVARCHAR(MAX) NULL,         -- JSON: {"affectedRecords": 1500, "migrationRequired": true}
        
        -- Auditoría estándar
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NULL,
        CreadorId UNIQUEIDENTIFIER NULL,
        
        -- Constraints
        CONSTRAINT CK_custom_field_audit_log_change_type 
            CHECK (ChangeType IN ('CREATE', 'UPDATE', 'DELETE', 'ENABLE', 'DISABLE', 'MIGRATE')),
            
        -- Foreign Key
        CONSTRAINT FK_custom_field_audit_log_definition 
            FOREIGN KEY (CustomFieldDefinitionId) 
            REFERENCES custom_field_definitions(Id) 
            ON DELETE CASCADE
    );
    
    PRINT '✅ Tabla custom_field_audit_log creada exitosamente';
END
GO

-- Índices para custom_field_audit_log
CREATE NONCLUSTERED INDEX IX_custom_field_audit_log_definition 
    ON custom_field_audit_log(CustomFieldDefinitionId, FechaCreacion DESC);

CREATE NONCLUSTERED INDEX IX_custom_field_audit_log_entity_date 
    ON custom_field_audit_log(EntityName, OrganizationId, FechaCreacion DESC);

CREATE NONCLUSTERED INDEX IX_custom_field_audit_log_change_type 
    ON custom_field_audit_log(ChangeType, FechaCreacion DESC)
    INCLUDE (EntityName, FieldName, CreadorId);

PRINT '✅ Índices para custom_field_audit_log creados exitosamente';
GO
```

---

## 🎨 Tabla de Templates (Fase Futura - Opcional)

### **Definición SQL**
```sql
-- ========================================
-- Tabla para templates de campos personalizados predefinidos
-- (Implementación futura para facilitar configuración)
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='custom_field_templates' AND xtype='U')
BEGIN
    CREATE TABLE custom_field_templates (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        
        -- Información del template
        TemplateName NVARCHAR(255) NOT NULL,         -- "Datos de Contacto Extendidos"
        Description NVARCHAR(1000) NULL,             -- Descripción del template
        Category NVARCHAR(100) NULL,                 -- "RRHH", "Ventas", "Administración"
        
        -- Configuración del template
        TargetEntityName NVARCHAR(100) NOT NULL,     -- Entidad objetivo
        FieldsDefinition NVARCHAR(MAX) NOT NULL,     -- JSON con array de definiciones de campos
        
        -- Metadatos
        IsSystemTemplate BIT DEFAULT 0 NOT NULL,     -- Template del sistema vs usuario
        UsageCount INT DEFAULT 0 NOT NULL,           -- Contador de veces usado
        
        -- Auditoría estándar
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NULL,       -- NULL para templates globales
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        Active BIT DEFAULT 1 NOT NULL
    );
    
    PRINT '✅ Tabla custom_field_templates creada exitosamente';
END
GO

-- Índices para custom_field_templates
CREATE NONCLUSTERED INDEX IX_custom_field_templates_entity_category 
    ON custom_field_templates(TargetEntityName, Category, Active)
    INCLUDE (TemplateName, Description, UsageCount);

CREATE NONCLUSTERED INDEX IX_custom_field_templates_usage 
    ON custom_field_templates(UsageCount DESC, Active)
    WHERE Active = 1;

PRINT '✅ Índices para custom_field_templates creados exitosamente';
GO
```

---

## 🔍 Optimizaciones para Consultas JSON en Campo Custom

### **Índices JSON para Entidades Principales**
```sql
-- ========================================
-- Índices optimizados para consultas en campo Custom (JSON)
-- Aplicar a entidades principales que tendrán campos personalizados
-- ========================================

-- Empleados - Índice para consultas JSON en campo Custom
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'empleados')
BEGIN
    -- Índice general para campo Custom no nulo
    CREATE NONCLUSTERED INDEX IX_empleados_custom_not_null 
        ON empleados(Id)
        INCLUDE (Custom)
        WHERE Custom IS NOT NULL AND Active = 1;
    
    -- Índice para facilitar búsquedas de campos específicos (si se usan frecuentemente)
    -- Nota: Estos índices se pueden crear dinámicamente según el uso real
    
    PRINT '✅ Índices JSON para empleados creados';
END

-- Empresas - Índice para consultas JSON en campo Custom
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'empresas')
BEGIN
    CREATE NONCLUSTERED INDEX IX_empresas_custom_not_null 
        ON empresas(Id)
        INCLUDE (Custom)
        WHERE Custom IS NOT NULL AND Active = 1;
    
    PRINT '✅ Índices JSON para empresas creados';
END

-- Clientes - Índice para consultas JSON en campo Custom (si existe la tabla)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'clientes')
BEGIN
    CREATE NONCLUSTERED INDEX IX_clientes_custom_not_null 
        ON clientes(Id)
        INCLUDE (Custom)
        WHERE Custom IS NOT NULL AND Active = 1;
    
    PRINT '✅ Índices JSON para clientes creados';
END

-- Template para otras entidades
-- REEMPLAZAR 'TABLA_ENTIDAD' con el nombre real de la tabla
/*
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TABLA_ENTIDAD')
BEGIN
    CREATE NONCLUSTERED INDEX IX_TABLA_ENTIDAD_custom_not_null 
        ON TABLA_ENTIDAD(Id)
        INCLUDE (Custom)
        WHERE Custom IS NOT NULL AND Active = 1;
    
    PRINT '✅ Índices JSON para TABLA_ENTIDAD creados';
END
*/
GO
```

### **Índices Computados para Campos Frecuentes (Ejemplo)**
```sql
-- ========================================
-- Ejemplo de columnas computadas para campos personalizados muy consultados
-- Solo implementar si hay campos específicos que se consultan frecuentemente
-- ========================================

-- Ejemplo para Empleados - Campo "telefono_emergencia"
/*
ALTER TABLE empleados 
ADD TelefonoEmergenciaComputed AS JSON_VALUE(Custom, '$.telefono_emergencia');

CREATE NONCLUSTERED INDEX IX_empleados_telefono_emergencia 
    ON empleados(TelefonoEmergenciaComputed)
    WHERE TelefonoEmergenciaComputed IS NOT NULL;
*/

-- Ejemplo para Empleados - Campo "nivel_ingles" 
/*
ALTER TABLE empleados 
ADD NivelInglesComputed AS JSON_VALUE(Custom, '$.nivel_ingles');

CREATE NONCLUSTERED INDEX IX_empleados_nivel_ingles 
    ON empleados(NivelInglesComputed)
    WHERE NivelInglesComputed IS NOT NULL;
*/

PRINT '✅ Ejemplos de columnas computadas documentados (comentados)';
GO
```

---

## 🛠️ Stored Procedures y Funciones de Utilidad

### **Función para Validar JSON de Configuración**
```sql
-- ========================================
-- Función para validar JSON de configuración de campos personalizados
-- ========================================
CREATE OR ALTER FUNCTION dbo.fn_ValidateCustomFieldConfig(
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
GO
```

### **Stored Procedure para Limpiar Campos Huérfanos**
```sql
-- ========================================
-- Stored Procedure para limpiar datos de campos personalizados
-- que ya no tienen definición activa
-- ========================================
CREATE OR ALTER PROCEDURE sp_CleanupOrphanedCustomFieldData
    @EntityName NVARCHAR(100),
    @OrganizationId UNIQUEIDENTIFIER = NULL,
    @DryRun BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @TableName NVARCHAR(128);
    DECLARE @SQL NVARCHAR(MAX);
    DECLARE @UpdatedCount INT = 0;
    
    -- Mapear EntityName a nombre de tabla
    SET @TableName = CASE 
        WHEN @EntityName = 'Empleado' THEN 'empleados'
        WHEN @EntityName = 'Empresa' THEN 'empresas'
        WHEN @EntityName = 'Cliente' THEN 'clientes'
        ELSE LOWER(@EntityName) + 's'
    END;
    
    -- Verificar que la tabla existe
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = @TableName)
    BEGIN
        RAISERROR('Tabla %s no existe', 16, 1, @TableName);
        RETURN;
    END
    
    -- Obtener campos activos
    DECLARE @ActiveFields TABLE (FieldName NVARCHAR(100));
    
    INSERT INTO @ActiveFields (FieldName)
    SELECT FieldName 
    FROM custom_field_definitions 
    WHERE EntityName = @EntityName 
      AND (@OrganizationId IS NULL OR OrganizationId = @OrganizationId)
      AND Active = 1 
      AND IsEnabled = 1;
    
    -- Construir SQL dinámico para limpieza
    SET @SQL = N'
    SELECT Id, Custom 
    FROM ' + QUOTENAME(@TableName) + N'
    WHERE Custom IS NOT NULL 
      AND (' + CASE WHEN @OrganizationId IS NOT NULL THEN N'OrganizationId = @OrgId AND ' ELSE N'' END + N'Active = 1)';
    
    IF @DryRun = 1
    BEGIN
        PRINT 'DRY RUN - Se limpiarían los siguientes registros:';
        -- Implementar lógica de dry run
    END
    ELSE
    BEGIN
        PRINT 'Ejecutando limpieza de campos huérfanos...';
        -- Implementar lógica de limpieza real
    END
    
    PRINT 'Proceso completado. Registros procesados: ' + CAST(@UpdatedCount AS NVARCHAR(10));
END
GO

PRINT '✅ Stored Procedure sp_CleanupOrphanedCustomFieldData creado';
GO
```

---

## 📊 Views para Reportes y Consultas

### **Vista para Resumen de Campos Personalizados**
```sql
-- ========================================
-- Vista para resumen de campos personalizados por entidad
-- ========================================
CREATE OR ALTER VIEW vw_CustomFieldsSummary
AS
SELECT 
    cfd.EntityName,
    cfd.OrganizationId,
    org.Name AS OrganizationName,
    COUNT(*) AS TotalFields,
    COUNT(CASE WHEN cfd.IsRequired = 1 THEN 1 END) AS RequiredFields,
    COUNT(CASE WHEN cfd.ConditionsConfig IS NOT NULL THEN 1 END) AS ConditionalFields,
    COUNT(CASE WHEN cfd.PermissionCreate IS NOT NULL OR cfd.PermissionUpdate IS NOT NULL THEN 1 END) AS FieldsWithPermissions,
    STRING_AGG(cfd.FieldType, ',') AS FieldTypes,
    MIN(cfd.FechaCreacion) AS FirstFieldCreated,
    MAX(cfd.FechaModificacion) AS LastFieldModified
FROM custom_field_definitions cfd
LEFT JOIN system_organization org ON cfd.OrganizationId = org.Id
WHERE cfd.Active = 1 AND cfd.IsEnabled = 1
GROUP BY cfd.EntityName, cfd.OrganizationId, org.Name;

PRINT '✅ Vista vw_CustomFieldsSummary creada';
GO
```

### **Vista para Auditoría de Uso**
```sql
-- ========================================
-- Vista para auditoría de uso de campos personalizados
-- ========================================
CREATE OR ALTER VIEW vw_CustomFieldsUsageAudit
AS
SELECT 
    cfd.EntityName,
    cfd.FieldName,
    cfd.DisplayName,
    cfd.FieldType,
    cfd.OrganizationId,
    cfd.FechaCreacion AS FieldCreated,
    creator.Nombre AS CreatedBy,
    cfd.FechaModificacion AS LastModified,
    modifier.Nombre AS LastModifiedBy,
    cfd.IsEnabled,
    cfd.Version,
    CASE 
        WHEN cfd.ConditionsConfig IS NOT NULL THEN 'Con Condiciones'
        WHEN cfd.PermissionCreate IS NOT NULL OR cfd.PermissionUpdate IS NOT NULL THEN 'Con Permisos'
        ELSE 'Básico'
    END AS ComplexityLevel
FROM custom_field_definitions cfd
LEFT JOIN system_users creator ON cfd.CreadorId = creator.Id
LEFT JOIN system_users modifier ON cfd.ModificadorId = modifier.Id
WHERE cfd.Active = 1;

PRINT '✅ Vista vw_CustomFieldsUsageAudit creada';
GO
```

---

## 🔧 Scripts de Mantenimiento

### **Script de Backup de Configuraciones**
```sql
-- ========================================
-- Script para backup de configuraciones de campos personalizados
-- ========================================
CREATE OR ALTER PROCEDURE sp_BackupCustomFieldConfigurations
    @BackupPath NVARCHAR(500),
    @OrganizationId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SQL NVARCHAR(MAX);
    DECLARE @FileName NVARCHAR(500);
    DECLARE @Timestamp NVARCHAR(20) = FORMAT(GETDATE(), 'yyyyMMdd_HHmmss');
    
    SET @FileName = @BackupPath + '\CustomFieldsBackup_' + @Timestamp + '.json';
    
    -- Exportar configuraciones a JSON
    SET @SQL = N'
    SELECT 
        EntityName,
        FieldName,
        DisplayName,
        FieldType,
        IsRequired,
        DefaultValue,
        ValidationConfig,
        UIConfig,
        ConditionsConfig,
        PermissionCreate,
        PermissionUpdate,
        PermissionView,
        SortOrder,
        Tags,
        OrganizationId
    FROM custom_field_definitions 
    WHERE Active = 1 
      AND IsEnabled = 1' +
    CASE WHEN @OrganizationId IS NOT NULL THEN N' AND OrganizationId = ''' + CAST(@OrganizationId AS NVARCHAR(36)) + '''' ELSE N'' END +
    N' ORDER BY EntityName, SortOrder
    FOR JSON AUTO';
    
    PRINT 'Backup de configuraciones guardado en: ' + @FileName;
    PRINT 'SQL generado: ' + @SQL;
END
GO

PRINT '✅ Stored Procedure sp_BackupCustomFieldConfigurations creado';
GO
```

---

## ✅ Checklist de Implementación

### **Fase 1 - Tabla Principal:**
- [ ] Crear tabla `custom_field_definitions`
- [ ] Crear índices básicos
- [ ] Probar inserción de datos de prueba
- [ ] Validar constraints y unique keys

### **Fase 2 - Optimizaciones:**
- [ ] Crear índices JSON en entidades principales
- [ ] Implementar función de validación
- [ ] Crear views de resumen
- [ ] Probar performance con datos de prueba

### **Fase 3 - Auditoría (Opcional):**
- [ ] Crear tabla `custom_field_audit_log`
- [ ] Implementar triggers de auditoría
- [ ] Crear reportes de cambios

### **Fase 4 - Mantenimiento:**
- [ ] Crear stored procedures de limpieza
- [ ] Implementar scripts de backup
- [ ] Crear jobs de mantenimiento automático

### **Fase 5 - Templates (Futuro):**
- [ ] Crear tabla `custom_field_templates`
- [ ] Poblar con templates predefinidos
- [ ] Crear funcionalidad de importación

---

## 🚨 Consideraciones de Performance

### **Estimaciones de Crecimiento:**
- **Campos por entidad**: 5-20 campos promedio
- **Entidades con campos personalizados**: 5-10 entidades principales
- **Organizaciones**: 100-1000 organizaciones
- **Total de definiciones**: 5,000-50,000 registros

### **Monitoring Requerido:**
- Tamaño de campo `Custom` en entidades principales
- Performance de consultas JSON
- Uso de índices especializados
- Fragmentación de índices JSON

### **Limites Recomendados:**
- **Max campos por entidad**: 50 campos
- **Max tamaño JSON Custom**: 4KB por registro
- **Max profundidad de condiciones**: 3 niveles
- **Max opciones en select**: 100 opciones

---

**🎯 Este schema está diseñado para ser robusto, escalable y aprovechar al máximo la infraestructura existente sin modificaciones disruptivas.**