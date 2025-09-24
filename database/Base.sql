

-- ========================================
-- 🏢 TABLA: system_organization
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_organization' AND xtype='U')
BEGIN
    CREATE TABLE system_organization (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        Nombre NVARCHAR(255) NOT NULL,
        Rut NVARCHAR(50) NULL,
        CustomData NVARCHAR(MAX) NULL,
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        Active BIT DEFAULT 1 NOT NULL
    );
    
    -- Índices para system_organization
    CREATE NONCLUSTERED INDEX IX_system_organization_Nombre ON system_organization(Nombre);
    CREATE NONCLUSTERED INDEX IX_system_organization_Rut ON system_organization(Rut);
    CREATE NONCLUSTERED INDEX IX_system_organization_Active ON system_organization(Active);
    CREATE NONCLUSTERED INDEX IX_system_organization_FechaCreacion ON system_organization(FechaCreacion);
    
    PRINT '✅ Tabla system_organization creada con índices';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_organization ya existe';
END

-- ========================================
-- 👤 TABLA: system_users
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_users' AND xtype='U')
BEGIN
    CREATE TABLE system_users (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        Nombre NVARCHAR(255) NOT NULL,
        Password NVARCHAR(255) NOT NULL,
        Email NVARCHAR(255) NOT NULL UNIQUE,
        CustomData NVARCHAR(MAX) NULL,
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NULL,
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        Active BIT DEFAULT 1 NOT NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_system_users_OrganizationId 
            FOREIGN KEY (OrganizationId) REFERENCES system_organization(Id),
        CONSTRAINT FK_system_users_CreadorId 
            FOREIGN KEY (CreadorId) REFERENCES system_users(Id),
        CONSTRAINT FK_system_users_ModificadorId 
            FOREIGN KEY (ModificadorId) REFERENCES system_users(Id)
    );
    
    -- Índices para system_users
    CREATE NONCLUSTERED INDEX IX_system_users_Nombre ON system_users(Nombre);
    CREATE NONCLUSTERED INDEX IX_system_users_Email ON system_users(Email);
    CREATE NONCLUSTERED INDEX IX_system_users_OrganizationId ON system_users(OrganizationId);
    CREATE NONCLUSTERED INDEX IX_system_users_Active ON system_users(Active);
    CREATE NONCLUSTERED INDEX IX_system_users_FechaCreacion ON system_users(FechaCreacion);
    
    PRINT '✅ Tabla system_users creada con índices y FK';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_users ya existe';
END

-- ========================================
-- 🔐 TABLA: system_permissions
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_permissions' AND xtype='U')
BEGIN
    CREATE TABLE system_permissions (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        Nombre NVARCHAR(255) NOT NULL,
        Descripcion NVARCHAR(500) NULL,
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NULL,
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        Active BIT DEFAULT 1 NOT NULL,
        ActionKey NVARCHAR(255) NULL,
        GroupKey NVARCHAR(255) NULL,
        GrupoNombre NVARCHAR(255) NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_system_permissions_OrganizationId 
            FOREIGN KEY (OrganizationId) REFERENCES system_organization(Id),
        CONSTRAINT FK_system_permissions_CreadorId 
            FOREIGN KEY (CreadorId) REFERENCES system_users(Id),
        CONSTRAINT FK_system_permissions_ModificadorId 
            FOREIGN KEY (ModificadorId) REFERENCES system_users(Id)
    );
    
    -- Índices para system_permissions
    CREATE NONCLUSTERED INDEX IX_system_permissions_Nombre ON system_permissions(Nombre);
    CREATE NONCLUSTERED INDEX IX_system_permissions_OrganizationId ON system_permissions(OrganizationId);
    CREATE NONCLUSTERED INDEX IX_system_permissions_Active ON system_permissions(Active);
    CREATE NONCLUSTERED INDEX IX_system_permissions_FechaCreacion ON system_permissions(FechaCreacion);
    CREATE NONCLUSTERED INDEX IX_system_permissions_ActionKey ON system_permissions(ActionKey);
    CREATE NONCLUSTERED INDEX IX_system_permissions_GroupKey ON system_permissions(GroupKey);
    CREATE NONCLUSTERED INDEX IX_system_permissions_GrupoNombre ON system_permissions(GrupoNombre);
    
    PRINT '✅ Tabla system_permissions creada con índices y FK';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_permissions ya existe';
END

-- ========================================
-- 👥 TABLA: system_roles
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_roles' AND xtype='U')
BEGIN
    CREATE TABLE system_roles (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        Nombre NVARCHAR(255) NOT NULL,
        Descripcion NVARCHAR(500) NULL,
        TypeRole NVARCHAR(100) DEFAULT 'Access' NOT NULL,
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NULL,
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        Active BIT DEFAULT 1 NOT NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_system_roles_OrganizationId 
            FOREIGN KEY (OrganizationId) REFERENCES system_organization(Id),
        CONSTRAINT FK_system_roles_CreadorId 
            FOREIGN KEY (CreadorId) REFERENCES system_users(Id),
        CONSTRAINT FK_system_roles_ModificadorId 
            FOREIGN KEY (ModificadorId) REFERENCES system_users(Id)
    );
    
    -- Índices para system_roles
    CREATE NONCLUSTERED INDEX IX_system_roles_Nombre ON system_roles(Nombre);
    CREATE NONCLUSTERED INDEX IX_system_roles_TypeRole ON system_roles(TypeRole);
    CREATE NONCLUSTERED INDEX IX_system_roles_OrganizationId ON system_roles(OrganizationId);
    CREATE NONCLUSTERED INDEX IX_system_roles_Active ON system_roles(Active);
    CREATE NONCLUSTERED INDEX IX_system_roles_FechaCreacion ON system_roles(FechaCreacion);
    
    PRINT '✅ Tabla system_roles creada con índices y FK';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_roles ya existe';
END

-- ========================================
-- 🔗 TABLA: system_users_roles (Many-to-Many)
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_users_roles' AND xtype='U')
BEGIN
    CREATE TABLE system_users_roles (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        system_roles_id UNIQUEIDENTIFIER NOT NULL,
        system_users_id UNIQUEIDENTIFIER NOT NULL,
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NULL,
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        Active BIT DEFAULT 1 NOT NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_system_users_roles_RoleId 
            FOREIGN KEY (system_roles_id) REFERENCES system_roles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_system_users_roles_UserId 
            FOREIGN KEY (system_users_id) REFERENCES system_users(Id) ON DELETE CASCADE,
        CONSTRAINT FK_system_users_roles_OrganizationId 
            FOREIGN KEY (OrganizationId) REFERENCES system_organization(Id),
        CONSTRAINT FK_system_users_roles_CreadorId 
            FOREIGN KEY (CreadorId) REFERENCES system_users(Id),
        CONSTRAINT FK_system_users_roles_ModificadorId 
            FOREIGN KEY (ModificadorId) REFERENCES system_users(Id),
            
        -- Constraint único para evitar duplicados
        CONSTRAINT UK_system_users_roles_UserRole 
            UNIQUE (system_users_id, system_roles_id)
    );
    
    -- Índices para system_users_roles
    CREATE NONCLUSTERED INDEX IX_system_users_roles_UserId ON system_users_roles(system_users_id);
    CREATE NONCLUSTERED INDEX IX_system_users_roles_RoleId ON system_users_roles(system_roles_id);
    CREATE NONCLUSTERED INDEX IX_system_users_roles_OrganizationId ON system_users_roles(OrganizationId);
    CREATE NONCLUSTERED INDEX IX_system_users_roles_Active ON system_users_roles(Active);
    
    PRINT '✅ Tabla system_users_roles creada con índices y FK';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_users_roles ya existe';
END

-- ========================================
-- 🔗 TABLA: system_users_permissions (Many-to-Many)
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_users_permissions' AND xtype='U')
BEGIN
    CREATE TABLE system_users_permissions (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        system_permissions_id UNIQUEIDENTIFIER NOT NULL,
        system_users_id UNIQUEIDENTIFIER NOT NULL,
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NULL,
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        Active BIT DEFAULT 1 NOT NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_system_users_permissions_PermissionId 
            FOREIGN KEY (system_permissions_id) REFERENCES system_permissions(Id) ON DELETE CASCADE,
        CONSTRAINT FK_system_users_permissions_UserId 
            FOREIGN KEY (system_users_id) REFERENCES system_users(Id) ON DELETE CASCADE,
        CONSTRAINT FK_system_users_permissions_OrganizationId 
            FOREIGN KEY (OrganizationId) REFERENCES system_organization(Id),
        CONSTRAINT FK_system_users_permissions_CreadorId 
            FOREIGN KEY (CreadorId) REFERENCES system_users(Id),
        CONSTRAINT FK_system_users_permissions_ModificadorId 
            FOREIGN KEY (ModificadorId) REFERENCES system_users(Id),
            
        -- Constraint único para evitar duplicados
        CONSTRAINT UK_system_users_permissions_UserPermission 
            UNIQUE (system_users_id, system_permissions_id)
    );
    
    -- Índices para system_users_permissions
    CREATE NONCLUSTERED INDEX IX_system_users_permissions_UserId ON system_users_permissions(system_users_id);
    CREATE NONCLUSTERED INDEX IX_system_users_permissions_PermissionId ON system_users_permissions(system_permissions_id);
    CREATE NONCLUSTERED INDEX IX_system_users_permissions_OrganizationId ON system_users_permissions(OrganizationId);
    CREATE NONCLUSTERED INDEX IX_system_users_permissions_Active ON system_users_permissions(Active);
    
    PRINT '✅ Tabla system_users_permissions creada con índices y FK';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_users_permissions ya existe';
END

-- ========================================
-- 🔗 TABLA: system_roles_permissions (Many-to-Many)
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_roles_permissions' AND xtype='U')
BEGIN
    CREATE TABLE system_roles_permissions (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        system_roles_id UNIQUEIDENTIFIER NOT NULL,
        system_permissions_id UNIQUEIDENTIFIER NOT NULL,
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NULL,
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        Active BIT DEFAULT 1 NOT NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_system_roles_permissions_RoleId 
            FOREIGN KEY (system_roles_id) REFERENCES system_roles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_system_roles_permissions_PermissionId 
            FOREIGN KEY (system_permissions_id) REFERENCES system_permissions(Id) ON DELETE CASCADE,
        CONSTRAINT FK_system_roles_permissions_OrganizationId 
            FOREIGN KEY (OrganizationId) REFERENCES system_organization(Id),
        CONSTRAINT FK_system_roles_permissions_CreadorId 
            FOREIGN KEY (CreadorId) REFERENCES system_users(Id),
        CONSTRAINT FK_system_roles_permissions_ModificadorId 
            FOREIGN KEY (ModificadorId) REFERENCES system_users(Id),
            
        -- Constraint único para evitar duplicados
        CONSTRAINT UK_system_roles_permissions_RolePermission 
            UNIQUE (system_roles_id, system_permissions_id)
    );
    
    -- Índices para system_roles_permissions
    CREATE NONCLUSTERED INDEX IX_system_roles_permissions_RoleId ON system_roles_permissions(system_roles_id);
    CREATE NONCLUSTERED INDEX IX_system_roles_permissions_PermissionId ON system_roles_permissions(system_permissions_id);
    CREATE NONCLUSTERED INDEX IX_system_roles_permissions_OrganizationId ON system_roles_permissions(OrganizationId);
    CREATE NONCLUSTERED INDEX IX_system_roles_permissions_Active ON system_roles_permissions(Active);
    
    PRINT '✅ Tabla system_roles_permissions creada con índices y FK';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_roles_permissions ya existe';
END

-- ========================================
-- ⚙️ TABLA: system_config
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_config' AND xtype='U')
BEGIN
    CREATE TABLE system_config (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        Field NVARCHAR(255) NOT NULL,
        TypeField NVARCHAR(255) NOT NULL,
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NULL,
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        Active BIT DEFAULT 1 NOT NULL,
        
        -- No Foreign Keys - Global configuration records
    );
    
    -- Índices para system_config
    CREATE NONCLUSTERED INDEX IX_system_config_Field ON system_config(Field);
    CREATE NONCLUSTERED INDEX IX_system_config_TypeField ON system_config(TypeField);
    CREATE NONCLUSTERED INDEX IX_system_config_OrganizationId ON system_config(OrganizationId);
    CREATE NONCLUSTERED INDEX IX_system_config_Active ON system_config(Active);
    CREATE NONCLUSTERED INDEX IX_system_config_FechaCreacion ON system_config(FechaCreacion);
    
    PRINT '✅ Tabla system_config creada con índices y FK';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_config ya existe';
END

-- ========================================
-- 📊 TABLA: system_config_values
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_config_values' AND xtype='U')
BEGIN
    CREATE TABLE system_config_values (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        SystemConfigId UNIQUEIDENTIFIER NOT NULL,
        Value NVARCHAR(MAX) NOT NULL,
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NULL,
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        Active BIT DEFAULT 1 NOT NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_system_config_values_SystemConfigId 
            FOREIGN KEY (SystemConfigId) REFERENCES system_config(Id) ON DELETE CASCADE
        -- No other Foreign Keys - Global configuration records
    );
    
    -- Índices para system_config_values
    CREATE NONCLUSTERED INDEX IX_system_config_values_SystemConfigId ON system_config_values(SystemConfigId);
    CREATE NONCLUSTERED INDEX IX_system_config_values_OrganizationId ON system_config_values(OrganizationId);
    CREATE NONCLUSTERED INDEX IX_system_config_values_Active ON system_config_values(Active);
    CREATE NONCLUSTERED INDEX IX_system_config_values_FechaCreacion ON system_config_values(FechaCreacion);
    
    PRINT '✅ Tabla system_config_values creada con índices y FK';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_config_values ya existe';
END

-- ========================================
-- 🔑 TABLA: z_token
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='z_token' AND xtype='U')
BEGIN
    CREATE TABLE z_token (
        id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        data VARCHAR(MAX) NULL,
        organizationid UNIQUEIDENTIFIER NULL,
        refresh BIT NULL,
        logout BIT NULL,
        
        -- Foreign Key
        CONSTRAINT FK_z_token_OrganizationId 
            FOREIGN KEY (organizationid) REFERENCES system_organization(Id)
    );
    
    -- Índices para z_token
    CREATE NONCLUSTERED INDEX IX_z_token_OrganizationId ON z_token(organizationid);
    CREATE NONCLUSTERED INDEX IX_z_token_Refresh ON z_token(refresh);
    CREATE NONCLUSTERED INDEX IX_z_token_Logout ON z_token(logout);
    
    PRINT '✅ Tabla z_token creada con índices y FK';
END
ELSE
BEGIN
    PRINT '📄 Tabla z_token ya existe';
END

-- ========================================
-- 📋 TABLA: system_auditoria (Header)
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_auditoria' AND xtype='U')
BEGIN
    CREATE TABLE system_auditoria (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        OrganizationId UNIQUEIDENTIFIER NULL,
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        Active BIT DEFAULT 1 NOT NULL,
        
        -- Campos específicos de auditoría header
        Tabla NVARCHAR(255) NOT NULL,
        RegistroId UNIQUEIDENTIFIER NOT NULL,
        Action NVARCHAR(50) NOT NULL,
        Comentario NVARCHAR(1000) NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_system_auditoria_OrganizationId 
            FOREIGN KEY (OrganizationId) REFERENCES system_organization(Id),
        CONSTRAINT FK_system_auditoria_CreadorId 
            FOREIGN KEY (CreadorId) REFERENCES system_users(Id),
        CONSTRAINT FK_system_auditoria_ModificadorId 
            FOREIGN KEY (ModificadorId) REFERENCES system_users(Id)
    );
    
    -- Índices para system_auditoria
    CREATE NONCLUSTERED INDEX IX_system_auditoria_Tabla ON system_auditoria(Tabla);
    CREATE NONCLUSTERED INDEX IX_system_auditoria_RegistroId ON system_auditoria(RegistroId);
    CREATE NONCLUSTERED INDEX IX_system_auditoria_Action ON system_auditoria(Action);
    CREATE NONCLUSTERED INDEX IX_system_auditoria_OrganizationId ON system_auditoria(OrganizationId);
    CREATE NONCLUSTERED INDEX IX_system_auditoria_FechaCreacion ON system_auditoria(FechaCreacion);
    
    PRINT '✅ Tabla system_auditoria creada con índices y FK';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_auditoria ya existe';
END

-- ========================================
-- 📋 TABLA: system_auditoria_detalle (Details)
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_auditoria_detalle' AND xtype='U')
BEGIN
    CREATE TABLE system_auditoria_detalle (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        OrganizationId UNIQUEIDENTIFIER NULL,
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        Active BIT DEFAULT 1 NOT NULL,
        
        -- Campos específicos de auditoría detalle
        AuditoriaId UNIQUEIDENTIFIER NOT NULL,
        Campo NVARCHAR(255) NOT NULL,
        ValorAnterior NVARCHAR(MAX) NULL,
        NuevoValor NVARCHAR(MAX) NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_system_auditoria_detalle_OrganizationId 
            FOREIGN KEY (OrganizationId) REFERENCES system_organization(Id),
        CONSTRAINT FK_system_auditoria_detalle_CreadorId 
            FOREIGN KEY (CreadorId) REFERENCES system_users(Id),
        CONSTRAINT FK_system_auditoria_detalle_ModificadorId 
            FOREIGN KEY (ModificadorId) REFERENCES system_users(Id),
        CONSTRAINT FK_system_auditoria_detalle_AuditoriaId 
            FOREIGN KEY (AuditoriaId) REFERENCES system_auditoria(Id) ON DELETE CASCADE
    );
    
    -- Índices para system_auditoria_detalle
    CREATE NONCLUSTERED INDEX IX_system_auditoria_detalle_AuditoriaId ON system_auditoria_detalle(AuditoriaId);
    CREATE NONCLUSTERED INDEX IX_system_auditoria_detalle_Campo ON system_auditoria_detalle(Campo);
    CREATE NONCLUSTERED INDEX IX_system_auditoria_detalle_OrganizationId ON system_auditoria_detalle(OrganizationId);
    CREATE NONCLUSTERED INDEX IX_system_auditoria_detalle_FechaCreacion ON system_auditoria_detalle(FechaCreacion);
    
    PRINT '✅ Tabla system_auditoria_detalle creada con índices y FK';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_auditoria_detalle ya existe';
END

-- ========================================
-- 📊 DATOS INICIALES
-- ========================================

PRINT '📊 Insertando datos iniciales...';

-- Variables para IDs
DECLARE @OrgId UNIQUEIDENTIFIER = NEWID();
DECLARE @AdminUserId UNIQUEIDENTIFIER = NEWID();
DECLARE @AdminRoleId UNIQUEIDENTIFIER = NEWID();
DECLARE @SuperAdminPermissionId UNIQUEIDENTIFIER = NEWID();

-- 1. Insertar Organización Base
IF NOT EXISTS (SELECT 1 FROM system_organization WHERE Nombre = 'Organización Base')
BEGIN
    INSERT INTO system_organization (Id, Nombre, Rut, CustomData, Active)
    VALUES (@OrgId, 'Organización Base', '12345678-9', '{"tipo": "base", "sistema": "inicial"}', 1);
    PRINT '✅ Organización Base creada';
END
ELSE
BEGIN
    SELECT @OrgId = Id FROM system_organization WHERE Nombre = 'Organización Base';
    PRINT '📄 Organización Base ya existe';
END

-- 2. Insertar Permiso Super Admin
IF NOT EXISTS (SELECT 1 FROM system_permissions WHERE Nombre = 'SuperAdmin')
BEGIN
    INSERT INTO system_permissions (Id, Nombre, Descripcion, OrganizationId, Active, CreadorId, ModificadorId)
    VALUES (@SuperAdminPermissionId, 'SuperAdmin', 'Acceso completo al sistema', NULL, 1, NULL, NULL);
    PRINT '✅ Permiso SuperAdmin creado';
END
ELSE
BEGIN
    SELECT @SuperAdminPermissionId = Id FROM system_permissions WHERE Nombre = 'SuperAdmin';
    PRINT '📄 Permiso SuperAdmin ya existe';
END

-- 2.1. Insertar Permisos del Sistema - SYSTEMPERMISSION
PRINT '📊 Insertando permisos SYSTEMPERMISSION...';
IF NOT EXISTS (SELECT 1 FROM system_permissions WHERE ActionKey = 'SYSTEMPERMISSION.CREATE')
BEGIN
    INSERT INTO system_permissions (Nombre, Descripcion, OrganizationId, Active, ActionKey, GroupKey, GrupoNombre, CreadorId, ModificadorId)
    VALUES
    ('SYSTEMPERMISSION.CREATE', 'Crear permisos del sistema', NULL, 1, 'SYSTEMPERMISSION.CREATE', 'SYSTEMPERMISSION', 'SystemPermission', NULL, NULL),
    ('SYSTEMPERMISSION.DELETE', 'Eliminar permisos del sistema', NULL, 1, 'SYSTEMPERMISSION.DELETE', 'SYSTEMPERMISSION', 'SystemPermission', NULL, NULL),
    ('SYSTEMPERMISSION.RESTORE', 'Restaurar permisos del sistema', NULL, 1, 'SYSTEMPERMISSION.RESTORE', 'SYSTEMPERMISSION', 'SystemPermission', NULL, NULL),
    ('SYSTEMPERMISSION.UPDATE', 'Actualizar permisos del sistema', NULL, 1, 'SYSTEMPERMISSION.UPDATE', 'SYSTEMPERMISSION', 'SystemPermission', NULL, NULL),
    ('SYSTEMPERMISSION.VIEW', 'Ver permisos del sistema', NULL, 1, 'SYSTEMPERMISSION.VIEW', 'SYSTEMPERMISSION', 'SystemPermission', NULL, NULL),
    ('SYSTEMPERMISSION.VIEWMENU', 'Ver menú de permisos', NULL, 1, 'SYSTEMPERMISSION.VIEWMENU', 'SYSTEMPERMISSION', 'SystemPermission', NULL, NULL);
    PRINT '✅ Permisos SYSTEMPERMISSION creados (6)';
END
ELSE
BEGIN
    PRINT '📄 Permisos SYSTEMPERMISSION ya existen';
END

-- 2.2. Insertar Permisos del Sistema - SYSTEMUSER
PRINT '📊 Insertando permisos SYSTEMUSER...';
IF NOT EXISTS (SELECT 1 FROM system_permissions WHERE ActionKey = 'SYSTEMUSER.CREATE')
BEGIN
    INSERT INTO system_permissions (Nombre, Descripcion, OrganizationId, Active, ActionKey, GroupKey, GrupoNombre, CreadorId, ModificadorId)
    VALUES
    ('SYSTEMUSER.CREATE', 'Crear usuarios del sistema', NULL, 1, 'SYSTEMUSER.CREATE', 'SYSTEMUSER', 'SystemUser', NULL, NULL),
    ('SYSTEMUSER.DELETE', 'Eliminar usuarios del sistema', NULL, 1, 'SYSTEMUSER.DELETE', 'SYSTEMUSER', 'SystemUser', NULL, NULL),
    ('SYSTEMUSER.RESTORE', 'Restaurar usuarios del sistema', NULL, 1, 'SYSTEMUSER.RESTORE', 'SYSTEMUSER', 'SystemUser', NULL, NULL),
    ('SYSTEMUSER.UPDATE', 'Actualizar usuarios del sistema', NULL, 1, 'SYSTEMUSER.UPDATE', 'SYSTEMUSER', 'SystemUser', NULL, NULL),
    ('SYSTEMUSER.VIEW', 'Ver usuarios del sistema', NULL, 1, 'SYSTEMUSER.VIEW', 'SYSTEMUSER', 'SystemUser', NULL, NULL),
    ('SYSTEMUSER.VIEWMENU', 'Ver menú de usuarios', NULL, 1, 'SYSTEMUSER.VIEWMENU', 'SYSTEMUSER', 'SystemUser', NULL, NULL),
    ('SYSTEMUSER.MANAGEROLES', 'Gestionar roles de usuarios', NULL, 1, 'SYSTEMUSER.MANAGEROLES', 'SYSTEMUSER', 'SystemUser', NULL, NULL),
    ('SYSTEMUSER.ADDROLES', 'Agregar roles a usuarios', NULL, 1, 'SYSTEMUSER.ADDROLES', 'SYSTEMUSER', 'SystemUser', NULL, NULL),
    ('SYSTEMUSER.REMOVEROLES', 'Remover roles de usuarios', NULL, 1, 'SYSTEMUSER.REMOVEROLES', 'SYSTEMUSER', 'SystemUser', NULL, NULL),
    ('SYSTEMUSER.MANAGEPERMISSIONS', 'Gestionar permisos directos de usuarios', NULL, 1, 'SYSTEMUSER.MANAGEPERMISSIONS', 'SYSTEMUSER', 'SystemUser', NULL, NULL),
    ('SYSTEMUSER.ADDPERMISSIONS', 'Agregar permisos directos a usuarios', NULL, 1, 'SYSTEMUSER.ADDPERMISSIONS', 'SYSTEMUSER', 'SystemUser', NULL, NULL),
    ('SYSTEMUSER.REMOVEPERMISSIONS', 'Remover permisos directos de usuarios', NULL, 1, 'SYSTEMUSER.REMOVEPERMISSIONS', 'SYSTEMUSER', 'SystemUser', NULL, NULL);
    PRINT '✅ Permisos SYSTEMUSER creados (12)';
END
ELSE
BEGIN
    PRINT '📄 Permisos SYSTEMUSER ya existen';
END

-- 2.5. Insertar Permisos del Sistema - SAVEDQUERIES
PRINT '📊 Insertando permisos SAVEDQUERIES...';
IF NOT EXISTS (SELECT 1 FROM system_permissions WHERE ActionKey = 'SAVEDQUERIES.CREATE')
BEGIN
    INSERT INTO system_permissions (Nombre, Descripcion, OrganizationId, Active, ActionKey, GroupKey, GrupoNombre, CreadorId, ModificadorId)
    VALUES
    ('SAVEDQUERIES.CREATE', 'Crear búsquedas guardadas', NULL, 1, 'SAVEDQUERIES.CREATE', 'SAVEDQUERIES', 'Búsquedas Guardadas', NULL, NULL),
    ('SAVEDQUERIES.VIEW', 'Ver búsquedas guardadas', NULL, 1, 'SAVEDQUERIES.VIEW', 'SAVEDQUERIES', 'Búsquedas Guardadas', NULL, NULL);
    PRINT '✅ Permisos SAVEDQUERIES creados (2)';
END
ELSE
BEGIN
    PRINT '📄 Permisos SAVEDQUERIES ya existen';
END

-- 2.3. Insertar Permisos del Sistema - SYSTEMROLE
PRINT '📊 Insertando permisos SYSTEMROLE...';
IF NOT EXISTS (SELECT 1 FROM system_permissions WHERE ActionKey = 'SYSTEMROLE.CREATE')
BEGIN
    INSERT INTO system_permissions (Nombre, Descripcion, OrganizationId, Active, ActionKey, GroupKey, GrupoNombre, CreadorId, ModificadorId)
    VALUES
    ('SYSTEMROLE.CREATE', 'Crear roles del sistema', NULL, 1, 'SYSTEMROLE.CREATE', 'SYSTEMROLE', 'SystemRole', NULL, NULL),
    ('SYSTEMROLE.DELETE', 'Eliminar roles del sistema', NULL, 1, 'SYSTEMROLE.DELETE', 'SYSTEMROLE', 'SystemRole', NULL, NULL),
    ('SYSTEMROLE.RESTORE', 'Restaurar roles del sistema', NULL, 1, 'SYSTEMROLE.RESTORE', 'SYSTEMROLE', 'SystemRole', NULL, NULL),
    ('SYSTEMROLE.UPDATE', 'Actualizar roles del sistema', NULL, 1, 'SYSTEMROLE.UPDATE', 'SYSTEMROLE', 'SystemRole', NULL, NULL),
    ('SYSTEMROLE.VIEW', 'Ver roles del sistema', NULL, 1, 'SYSTEMROLE.VIEW', 'SYSTEMROLE', 'SystemRole', NULL, NULL),
    ('SYSTEMROLE.VIEWMENU', 'Ver menú de roles', NULL, 1, 'SYSTEMROLE.VIEWMENU', 'SYSTEMROLE', 'SystemRole', NULL, NULL),
    ('SYSTEMROLE.MANAGEPERMISSIONS', 'Gestionar permisos de roles', NULL, 1, 'SYSTEMROLE.MANAGEPERMISSIONS', 'SYSTEMROLE', 'SystemRole', NULL, NULL),
    ('SYSTEMROLE.ADDPERMISSIONS', 'Agregar permisos a roles', NULL, 1, 'SYSTEMROLE.ADDPERMISSIONS', 'SYSTEMROLE', 'SystemRole', NULL, NULL),
    ('SYSTEMROLE.REMOVEPERMISSIONS', 'Remover permisos de roles', NULL, 1, 'SYSTEMROLE.REMOVEPERMISSIONS', 'SYSTEMROLE', 'SystemRole', NULL, NULL),
    ('SYSTEMROLE.MANAGEUSERS', 'Gestionar usuarios de roles', NULL, 1, 'SYSTEMROLE.MANAGEUSERS', 'SYSTEMROLE', 'SystemRole', NULL, NULL);
    PRINT '✅ Permisos SYSTEMROLE creados (10)';
END
ELSE
BEGIN
    PRINT '📄 Permisos SYSTEMROLE ya existen';
END

-- 2.4. Insertar Permisos del Sistema - FORMDESIGNER
PRINT '📊 Insertando permisos FORMDESIGNER...';
IF NOT EXISTS (SELECT 1 FROM system_permissions WHERE ActionKey = 'FORMDESIGNER.CREATE')
BEGIN
    INSERT INTO system_permissions (Nombre, Descripcion, OrganizationId, Active, ActionKey, GroupKey, GrupoNombre, CreadorId, ModificadorId)
    VALUES
    ('FORMDESIGNER.CREATE', 'Crear diseños de formularios', NULL, 1, 'FORMDESIGNER.CREATE', 'FORMDESIGNER', 'Diseñador de Formularios', NULL, NULL),
    ('FORMDESIGNER.DELETE', 'Eliminar diseños de formularios', NULL, 1, 'FORMDESIGNER.DELETE', 'FORMDESIGNER', 'Diseñador de Formularios', NULL, NULL),
    ('FORMDESIGNER.UPDATE', 'Editar diseños de formularios', NULL, 1, 'FORMDESIGNER.UPDATE', 'FORMDESIGNER', 'Diseñador de Formularios', NULL, NULL),
    ('FORMDESIGNER.VIEW', 'Ver diseñador de formularios', NULL, 1, 'FORMDESIGNER.VIEW', 'FORMDESIGNER', 'Diseñador de Formularios', NULL, NULL),
    ('FORMDESIGNER.VIEWMENU', 'Ver menú de diseñador de formularios', NULL, 1, 'FORMDESIGNER.VIEWMENU', 'FORMDESIGNER', 'Diseñador de Formularios', NULL, NULL);
    PRINT '✅ Permisos FORMDESIGNER creados';
END
ELSE
BEGIN
    PRINT '📄 Permisos FORMDESIGNER ya existen';
END

-- 3. Insertar Rol Admin
IF NOT EXISTS (SELECT 1 FROM system_roles WHERE Nombre = 'Administrador')
BEGIN
    INSERT INTO system_roles (Id, Nombre, Descripcion, TypeRole, OrganizationId, Active)
    VALUES (@AdminRoleId, 'Administrador', 'Rol de administrador del sistema', 'Admin', @OrgId, 1);
    PRINT '✅ Rol Administrador creado';
END
ELSE
BEGIN
    SELECT @AdminRoleId = Id FROM system_roles WHERE Nombre = 'Administrador';
    PRINT '📄 Rol Administrador ya existe';
END

-- 4. Insertar Usuario Admin
IF NOT EXISTS (SELECT 1 FROM system_users WHERE Email = 'admin@admin.cl')
BEGIN
    INSERT INTO system_users (Id, Nombre, Password, Email, OrganizationId, Active)
    VALUES (@AdminUserId, 'Administrador Sistema', 'U29wb3J0ZS4yMDE5UiZEbVNZdUwzQSM3NXR3NGlCa0BOcVJVI2pXISNabTM4TkJ6YTRKa3dlcHRZN2ZWaDRFVkBaRzdMTnhtOEs2VGY0dUhyUyR6UWNYQ1h2VHJAOE1kJDR4IyYkOSZaSmt0Qk4mYzk4VF5WNHE3UnpXNktVV3Ikc1Z5', 'admin@admin.cl', @OrgId, 1);
    PRINT '✅ Usuario admin@admin.cl creado';
END
ELSE
BEGIN
    SELECT @AdminUserId = Id FROM system_users WHERE Email = 'admin@admin.cl';
    PRINT '📄 Usuario admin@admin.cl ya existe';
END

-- 5. Asignar Rol al Usuario
IF NOT EXISTS (SELECT 1 FROM system_users_roles WHERE system_users_id = @AdminUserId AND system_roles_id = @AdminRoleId)
BEGIN
    INSERT INTO system_users_roles (system_users_id, system_roles_id, OrganizationId, CreadorId, Active)
    VALUES (@AdminUserId, @AdminRoleId, @OrgId, @AdminUserId, 1);
    PRINT '✅ Rol Administrador asignado al usuario';
END
ELSE
BEGIN
    PRINT '📄 Rol ya asignado al usuario';
END

-- 6. Asignar Permiso SuperAdmin directamente al Usuario admin@admin.cl
IF NOT EXISTS (SELECT 1 FROM system_users_permissions WHERE system_users_id = @AdminUserId AND system_permissions_id = @SuperAdminPermissionId)
BEGIN
    INSERT INTO system_users_permissions (system_users_id, system_permissions_id, OrganizationId, CreadorId, Active)
    VALUES (@AdminUserId, @SuperAdminPermissionId, @OrgId, @AdminUserId, 1);
    PRINT '✅ Permiso SuperAdmin asignado directamente al usuario admin@admin.cl';
END
ELSE
BEGIN
    PRINT '📄 Permiso SuperAdmin ya asignado directamente al usuario';
END

-- 7. Asignar Permiso SuperAdmin al Rol (adicional)
IF NOT EXISTS (SELECT 1 FROM system_roles_permissions WHERE system_roles_id = @AdminRoleId AND system_permissions_id = @SuperAdminPermissionId)
BEGIN
    INSERT INTO system_roles_permissions (system_roles_id, system_permissions_id, OrganizationId, CreadorId, Active)
    VALUES (@AdminRoleId, @SuperAdminPermissionId, @OrgId, @AdminUserId, 1);
    PRINT '✅ Permiso SuperAdmin asignado al rol Administrador';
END
ELSE
BEGIN
    PRINT '📄 Permiso SuperAdmin ya asignado al rol';
END

-- ========================================
-- ✅ RESUMEN FINAL
-- ========================================

PRINT '';
PRINT '========================================';
PRINT '✅ SCHEMA BASE CREADO EXITOSAMENTE';
PRINT '========================================';
PRINT '';
PRINT '🏢 Organización: Organización Base';
PRINT '👤 Usuario Admin: admin@admin.cl';
PRINT '🔑 Password: U29wb3J0ZS4yMDE5UiZEbVNZdUwzQSM3NXR3NGlCa0BOcVJVI2pXISNabTM4TkJ6YTRKa3dlcHRZN2ZWaDRFVkBaRzdMTnhtOEs2VGY0dUhyUyR6UWNYQ1h2VHJAOE1kJDR4IyYkOSZaSmt0Qk4mYzk4VF5WNHE3UnpXNktVV3Ikc1Z5';
PRINT '👥 Rol: Administrador';
PRINT '🔐 Permisos: SuperAdmin + Sistema (28 permisos CRUD + Gestión)';
PRINT '';
PRINT '📋 Permisos del sistema incluidos:';
PRINT '   🔐 SYSTEMPERMISSION.* (CREATE, DELETE, RESTORE, UPDATE, VIEW, VIEWMENU)';
PRINT '   👤 SYSTEMUSER.* (CREATE, DELETE, RESTORE, UPDATE, VIEW, VIEWMENU, MANAGEROLES, ADDROLES, REMOVEROLES, MANAGEPERMISSIONS, ADDPERMISSIONS, REMOVEPERMISSIONS)';
PRINT '   🛡️  SYSTEMROLE.* (CREATE, DELETE, RESTORE, UPDATE, VIEW, VIEWMENU, MANAGEPERMISSIONS, ADDPERMISSIONS, REMOVEPERMISSIONS, MANAGEUSERS)';
PRINT '';
PRINT '📊 Tablas creadas:';
PRINT '   • system_organization';
PRINT '   • system_users';
PRINT '   • system_permissions';
PRINT '   • system_roles';
PRINT '   • system_users_roles';
PRINT '   • system_users_permissions';
PRINT '   • system_roles_permissions';
PRINT '   • system_config';
PRINT '   • system_config_values';
PRINT '   • z_token';
PRINT '   • system_auditoria';
PRINT '   • system_auditoria_detalle';
PRINT '   • system_tablas_auditables';
PRINT '   • system_campos_auditables';
PRINT '   • system_form_entities (FormDesigner)';
PRINT '   • system_form_layouts (Form Layouts)';
PRINT '';
PRINT '🎯 Listo para usar con el generador de modelos Python!';
PRINT '========================================';

-- ========================================
-- 📊 SISTEMA DE AUDITORÍA DINÁMICO
-- ========================================

-- 1. TABLA DE CONFIGURACIÓN DE TABLAS AUDITABLES
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_tablas_auditables' AND xtype='U')
BEGIN
    CREATE TABLE system_tablas_auditables (
        id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        organizacion_id UNIQUEIDENTIFIER NULL, -- NULL = aplica a todas las organizaciones
        tabla NVARCHAR(100) NOT NULL,
        activo BIT DEFAULT 1,
        trigger_creado BIT DEFAULT 0, -- Control de si ya se creó el trigger
        fecha_creacion DATETIME2 DEFAULT GETUTCDATE(),
        fecha_modificacion DATETIME2 DEFAULT GETUTCDATE(),
        creado_por UNIQUEIDENTIFIER NULL,
        UNIQUE(organizacion_id, tabla)
    );

    -- Índices optimizados para queries frecuentes
    CREATE INDEX IX_system_tablas_auditables_tabla_activo ON system_tablas_auditables(tabla, activo) INCLUDE(organizacion_id, trigger_creado);
    CREATE INDEX IX_system_tablas_auditables_activo ON system_tablas_auditables(activo) WHERE activo = 1;

    PRINT '✅ Tabla system_tablas_auditables creada con índices optimizados';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_tablas_auditables ya existe';
END

-- 2. TABLA DE CONFIGURACIÓN DE CAMPOS AUDITABLES (OPTIMIZADA)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_campos_auditables' AND xtype='U')
BEGIN
    CREATE TABLE system_campos_auditables (
        id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        organizacion_id UNIQUEIDENTIFIER NULL, -- NULL = aplica a todas las organizaciones
        tabla NVARCHAR(100) NOT NULL,
        campo NVARCHAR(200) NOT NULL, -- Para soportar Custom.Atributo
        activo BIT DEFAULT 1,
        is_custom BIT DEFAULT 0, -- 1 = campo Custom JSON, 0 = campo directo
        fecha_creacion DATETIME2 DEFAULT GETUTCDATE(),
        creado_por UNIQUEIDENTIFIER NULL,
        UNIQUE(organizacion_id, tabla, campo)
    );

    -- Índices optimizados con INCLUDE para evitar key lookups
    CREATE INDEX IX_system_campos_auditables_tabla_activo ON system_campos_auditables(tabla, activo)
        INCLUDE(organizacion_id, campo, is_custom) WHERE activo = 1;

    PRINT '✅ Tabla system_campos_auditables creada con índices optimizados';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_campos_auditables ya existe';
END

-- 3. STORED PROCEDURE PARA CONFIGURAR CAMPO AUDITABLE
GO
CREATE OR ALTER PROCEDURE sp_configurar_campo_auditable
    @tabla NVARCHAR(100),
    @campo NVARCHAR(200),
    @organizacion_id UNIQUEIDENTIFIER = NULL,
    @is_custom BIT = 0,
    @activo BIT = 1,
    @creado_por UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Insertar o actualizar configuración
    IF EXISTS (SELECT 1 FROM system_campos_auditables
               WHERE tabla = @tabla AND campo = @campo AND
                     (organizacion_id = @organizacion_id OR (organizacion_id IS NULL AND @organizacion_id IS NULL)))
    BEGIN
        UPDATE system_campos_auditables
        SET activo = @activo,
            is_custom = @is_custom,
            creado_por = @creado_por
        WHERE tabla = @tabla AND campo = @campo AND
              (organizacion_id = @organizacion_id OR (organizacion_id IS NULL AND @organizacion_id IS NULL));

        PRINT 'Campo auditable actualizado: ' + @tabla + '.' + @campo;
    END
    ELSE
    BEGIN
        INSERT INTO system_campos_auditables (organizacion_id, tabla, campo, activo, is_custom, creado_por)
        VALUES (@organizacion_id, @tabla, @campo, @activo, @is_custom, @creado_por);

        PRINT 'Campo auditable agregado: ' + @tabla + '.' + @campo;
    END

    -- Si se activó, asegurar que existe el trigger para la tabla
    IF @activo = 1
    BEGIN
        EXEC sp_activar_auditoria_tabla @tabla = @tabla, @organizacion_id = @organizacion_id;
    END
END
GO

-- 4. STORED PROCEDURE PARA ACTIVAR AUDITORÍA EN UNA TABLA
GO
CREATE OR ALTER PROCEDURE sp_activar_auditoria_tabla
    @tabla NVARCHAR(100),
    @organizacion_id UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Validar que la tabla existe
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = @tabla)
    BEGIN
        PRINT 'Error: La tabla ' + @tabla + ' no existe.';
        RETURN;
    END

    -- Validar que la tabla tenga un campo Id
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(@tabla) AND name = 'Id')
    BEGIN
        PRINT 'Error: La tabla ' + @tabla + ' debe tener un campo Id para ser auditable.';
        RETURN;
    END

    DECLARE @trigger_name NVARCHAR(200);
    DECLARE @sql NVARCHAR(MAX);

    -- Nombre del trigger
    IF @organizacion_id IS NOT NULL
        SET @trigger_name = 'trg_audit_' + @tabla + '_' + LEFT(REPLACE(CAST(@organizacion_id AS NVARCHAR(36)), '-', ''), 20);
    ELSE
        SET @trigger_name = 'trg_audit_' + @tabla + '_global';

    -- Eliminar trigger existente si existe
    IF EXISTS (SELECT * FROM sys.triggers WHERE name = @trigger_name)
    BEGIN
        SET @sql = 'DROP TRIGGER ' + @trigger_name;
        EXEC sp_executesql @sql;
    END

    -- Crear el trigger dinámico
    SET @sql = '
    CREATE TRIGGER ' + @trigger_name + '
    ON [' + @tabla + ']
    FOR UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;

        -- Verificar si hay registros en inserted
        IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;

        -- Verificar si hay campos auditables activos para esta tabla
        IF NOT EXISTS (
            SELECT 1 FROM system_campos_auditables
            WHERE tabla = ''' + @tabla + ''' AND activo = 1
        ) RETURN;

        -- Procesar cada registro modificado
        DECLARE @registro_id UNIQUEIDENTIFIER;
        DECLARE @org_id UNIQUEIDENTIFIER;
        DECLARE @modificador_id UNIQUEIDENTIFIER;

        -- Cursor para procesar registros modificados
        DECLARE cursor_registros CURSOR FOR
        SELECT i.Id
        FROM inserted i
        INNER JOIN deleted d ON i.Id = d.Id;

        OPEN cursor_registros;
        FETCH NEXT FROM cursor_registros INTO @registro_id;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Obtener OrganizationId y ModificadorId si existen en la tabla
            SET @org_id = NULL;
            SET @modificador_id = NULL;

            IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(''' + @tabla + ''') AND name = ''OrganizationId'')
            BEGIN
                DECLARE @sql_org NVARCHAR(MAX) = ''SELECT @org_id_out = OrganizationId FROM inserted WHERE Id = @registro_id_param'';
                EXEC sp_executesql @sql_org, N''@org_id_out UNIQUEIDENTIFIER OUTPUT, @registro_id_param UNIQUEIDENTIFIER'', @org_id_out = @org_id OUTPUT, @registro_id_param = @registro_id;
            END

            IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(''' + @tabla + ''') AND name = ''ModificadorId'')
            BEGIN
                DECLARE @sql_mod NVARCHAR(MAX) = ''SELECT @modificador_id_out = ModificadorId FROM inserted WHERE Id = @registro_id_param'';
                EXEC sp_executesql @sql_mod, N''@modificador_id_out UNIQUEIDENTIFIER OUTPUT, @registro_id_param UNIQUEIDENTIFIER'', @modificador_id_out = @modificador_id OUTPUT, @registro_id_param = @registro_id;
            END

            -- Crear registro principal de auditoría
            DECLARE @auditoria_id UNIQUEIDENTIFIER = NEWID();

            INSERT INTO system_auditoria (
                Id, OrganizationId, Tabla, RegistroId, Action,
                FechaCreacion, FechaModificacion, CreadorId, ModificadorId, Active
            )
            VALUES (
                @auditoria_id, @org_id, ''' + @tabla + ''', @registro_id, ''UPDATE'',
                GETUTCDATE(), GETUTCDATE(), @modificador_id, @modificador_id, 1
            );

            -- Procesar cada campo auditable
            DECLARE @campo NVARCHAR(200);
            DECLARE @is_custom BIT;

            DECLARE campos_cursor CURSOR FOR
            SELECT campo, is_custom
            FROM system_campos_auditables
            WHERE tabla = ''' + @tabla + ''' AND activo = 1
              AND (organizacion_id IS NULL OR organizacion_id = @org_id);

            OPEN campos_cursor;
            FETCH NEXT FROM campos_cursor INTO @campo, @is_custom;

            WHILE @@FETCH_STATUS = 0
            BEGIN
                IF @is_custom = 0
                BEGIN
                    -- Procesar campo directo
                    DECLARE @sql_campo NVARCHAR(MAX);
                    DECLARE @valor_anterior NVARCHAR(MAX);
                    DECLARE @valor_nuevo NVARCHAR(MAX);

                    SET @sql_campo = ''
                    SELECT TOP 1
                        @valor_anterior_out = CAST(d.['' + @campo + ''] AS NVARCHAR(MAX)),
                        @valor_nuevo_out = CAST(i.['' + @campo + ''] AS NVARCHAR(MAX))
                    FROM inserted i
                    INNER JOIN deleted d ON i.Id = d.Id
                    WHERE i.Id = '''''' + CAST(@registro_id AS NVARCHAR(36)) + ''''''
                      AND (
                          (d.['' + @campo + ''] IS NULL AND i.['' + @campo + ''] IS NOT NULL) OR
                          (d.['' + @campo + ''] IS NOT NULL AND i.['' + @campo + ''] IS NULL) OR
                          (CAST(d.['' + @campo + ''] AS NVARCHAR(MAX)) != CAST(i.['' + @campo + ''] AS NVARCHAR(MAX)))
                      )'';

                    EXEC sp_executesql @sql_campo,
                         N''@valor_anterior_out NVARCHAR(MAX) OUTPUT, @valor_nuevo_out NVARCHAR(MAX) OUTPUT'',
                         @valor_anterior_out = @valor_anterior OUTPUT,
                         @valor_nuevo_out = @valor_nuevo OUTPUT;

                    IF @valor_anterior IS NOT NULL OR @valor_nuevo IS NOT NULL
                    BEGIN
                        INSERT INTO system_auditoria_detalle (
                            Id, OrganizationId, AuditoriaId, Campo, ValorAnterior, NuevoValor,
                            FechaCreacion, FechaModificacion, CreadorId, ModificadorId, Active
                        )
                        VALUES (
                            NEWID(), @org_id, @auditoria_id, @campo, @valor_anterior, @valor_nuevo,
                            GETUTCDATE(), GETUTCDATE(), @modificador_id, @modificador_id, 1
                        );
                    END
                END
                ELSE
                BEGIN
                    -- Procesar campo Custom JSON
                    DECLARE @json_path NVARCHAR(200);
                    SET @json_path = ''$.'' + SUBSTRING(@campo, CHARINDEX(''.'', @campo) + 1, LEN(@campo));
                    SET @valor_anterior = NULL;
                    SET @valor_nuevo = NULL;

                    DECLARE @sql_json NVARCHAR(MAX);
                    SET @sql_json = ''
                    SELECT TOP 1
                        @valor_anterior_out = JSON_VALUE(d.Custom, '''''' + @json_path + ''''''),
                        @valor_nuevo_out = JSON_VALUE(i.Custom, '''''' + @json_path + '''''')
                    FROM inserted i
                    INNER JOIN deleted d ON i.Id = d.Id
                    WHERE i.Id = '''''' + CAST(@registro_id AS NVARCHAR(36)) + ''''''
                      AND (
                          ISNULL(JSON_VALUE(d.Custom, '''''' + @json_path + ''''''), '''''''') !=
                          ISNULL(JSON_VALUE(i.Custom, '''''' + @json_path + ''''''), '''''''')
                      )'';

                    EXEC sp_executesql @sql_json,
                         N''@valor_anterior_out NVARCHAR(MAX) OUTPUT, @valor_nuevo_out NVARCHAR(MAX) OUTPUT'',
                         @valor_anterior_out = @valor_anterior OUTPUT,
                         @valor_nuevo_out = @valor_nuevo OUTPUT;

                    IF @valor_anterior IS NOT NULL OR @valor_nuevo IS NOT NULL
                    BEGIN
                        INSERT INTO system_auditoria_detalle (
                            Id, OrganizationId, AuditoriaId, Campo, ValorAnterior, NuevoValor,
                            FechaCreacion, FechaModificacion, CreadorId, ModificadorId, Active
                        )
                        VALUES (
                            NEWID(), @org_id, @auditoria_id, @campo, @valor_anterior, @valor_nuevo,
                            GETUTCDATE(), GETUTCDATE(), @modificador_id, @modificador_id, 1
                        );
                    END
                END

                FETCH NEXT FROM campos_cursor INTO @campo, @is_custom;
            END

            CLOSE campos_cursor;
            DEALLOCATE campos_cursor;

            FETCH NEXT FROM cursor_registros INTO @registro_id;
        END

        CLOSE cursor_registros;
        DEALLOCATE cursor_registros;
    END';

    -- Ejecutar la creación del trigger
    EXEC sp_executesql @sql;

    -- Actualizar configuración
    MERGE system_tablas_auditables AS target
    USING (SELECT @tabla AS tabla, @organizacion_id AS organizacion_id) AS source
    ON target.tabla = source.tabla AND
       (target.organizacion_id = source.organizacion_id OR (target.organizacion_id IS NULL AND source.organizacion_id IS NULL))
    WHEN MATCHED THEN
        UPDATE SET activo = 1, trigger_creado = 1, fecha_modificacion = GETUTCDATE()
    WHEN NOT MATCHED THEN
        INSERT (organizacion_id, tabla, activo, trigger_creado)
        VALUES (source.organizacion_id, source.tabla, 1, 1);

    PRINT 'Trigger de auditoría creado: ' + @trigger_name;
END
GO

-- 5. STORED PROCEDURE PARA DESACTIVAR AUDITORÍA
GO
CREATE OR ALTER PROCEDURE sp_desactivar_auditoria_tabla
    @tabla NVARCHAR(100),
    @organizacion_id UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @trigger_name NVARCHAR(200);
    DECLARE @sql NVARCHAR(MAX);

    -- Nombre del trigger
    IF @organizacion_id IS NOT NULL
        SET @trigger_name = 'trg_audit_' + @tabla + '_' + LEFT(REPLACE(CAST(@organizacion_id AS NVARCHAR(36)), '-', ''), 20);
    ELSE
        SET @trigger_name = 'trg_audit_' + @tabla + '_global';

    -- Eliminar el trigger si existe
    IF EXISTS (SELECT * FROM sys.triggers WHERE name = @trigger_name)
    BEGIN
        SET @sql = 'DROP TRIGGER ' + @trigger_name;
        EXEC sp_executesql @sql;
        PRINT 'Trigger eliminado: ' + @trigger_name;
    END

    -- Actualizar configuración
    UPDATE system_tablas_auditables
    SET activo = 0, trigger_creado = 0, fecha_modificacion = GETUTCDATE()
    WHERE tabla = @tabla
      AND (organizacion_id = @organizacion_id OR (organizacion_id IS NULL AND @organizacion_id IS NULL));

    PRINT 'Auditoría desactivada para tabla: ' + @tabla;
END
GO

-- 6. FUNCIÓN PARA VERIFICAR SI UNA TABLA ESTÁ SIENDO AUDITADA
GO
CREATE OR ALTER FUNCTION fn_tabla_es_auditable
(
    @tabla NVARCHAR(100),
    @organizacion_id UNIQUEIDENTIFIER = NULL
)
RETURNS BIT
AS
BEGIN
    DECLARE @es_auditable BIT = 0;

    SELECT @es_auditable = 1
    FROM system_tablas_auditables
    WHERE tabla = @tabla
      AND activo = 1
      AND trigger_creado = 1
      AND (organizacion_id IS NULL OR organizacion_id = @organizacion_id);

    RETURN ISNULL(@es_auditable, 0);
END
GO

-- 7. VISTA PARA CONSULTAR AUDITORÍAS
GO
CREATE OR ALTER VIEW vw_auditoria_completa
AS
SELECT
    a.Id AS AuditoriaId,
    a.OrganizationId,
    a.Tabla,
    a.RegistroId,
    a.Action,
    a.FechaCreacion AS FechaAuditoria,
    a.CreadorId AS UsuarioModificacion,
    d.Campo,
    d.ValorAnterior,
    d.NuevoValor,
    d.FechaCreacion AS FechaDetalle
FROM system_auditoria a
INNER JOIN system_auditoria_detalle d ON a.Id = d.AuditoriaId
WHERE a.Active = 1 AND d.Active = 1;
GO

PRINT '✅ Sistema de auditoría dinámico implementado exitosamente';
PRINT '';
PRINT '📋 Ejemplos de uso:';
PRINT '   -- Configurar auditoría para una tabla:';
PRINT '   EXEC sp_configurar_campo_auditable @tabla = ''empleado'', @campo = ''sueldo_base'', @activo = 1;';
PRINT '   ';
PRINT '   -- Configurar campo Custom JSON:';
PRINT '   EXEC sp_configurar_campo_auditable @tabla = ''empleado'', @campo = ''Custom.Bonificacion'', @is_custom = 1, @activo = 1;';
PRINT '   ';
PRINT '   -- Desactivar auditoría:';
PRINT '   EXEC sp_desactivar_auditoria_tabla @tabla = ''empleado'';';
PRINT '   ';
PRINT '   -- Consultar auditorías:';
PRINT '   SELECT * FROM vw_auditoria_completa WHERE Tabla = ''empleado'';';
PRINT '';

-- ========================================
-- 🎨 TABLA: system_form_entities (FormDesigner)
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_form_entities' AND xtype='U')
BEGIN
    CREATE TABLE system_form_entities (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        OrganizationId UNIQUEIDENTIFIER NULL,           -- NULL = Global, GUID = Específica
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        Active BIT DEFAULT 1 NOT NULL,

        -- Campos específicos de FormDesigner
        EntityName NVARCHAR(100) NOT NULL,              -- Nombre técnico (ej: "Empleado")
        DisplayName NVARCHAR(200) NOT NULL,             -- Nombre amigable (ej: "Empleados")
        Description NVARCHAR(500) NULL,                 -- Descripción de la entidad
        TableName NVARCHAR(100) NOT NULL,               -- Nombre de tabla (ej: "empleados")
        IconName NVARCHAR(50) NULL,                     -- Icono Material Design
        Category NVARCHAR(100) NULL,                    -- Categoría (ej: "RRHH", "Core")
        AllowCustomFields BIT DEFAULT 1 NOT NULL,       -- Permite campos personalizados
        SortOrder INT DEFAULT 100 NOT NULL,             -- Orden de presentación
        BackendApi NVARCHAR(200) NULL,                  -- API Backend (ej: "MainBackend", "FormBackend")

        -- Foreign Keys
        CONSTRAINT FK_system_form_entities_OrganizationId
            FOREIGN KEY (OrganizationId) REFERENCES system_organization(Id),
        CONSTRAINT FK_system_form_entities_CreadorId
            FOREIGN KEY (CreadorId) REFERENCES system_users(Id),
        CONSTRAINT FK_system_form_entities_ModificadorId
            FOREIGN KEY (ModificadorId) REFERENCES system_users(Id)
    );

    -- Índices para system_form_entities
    CREATE NONCLUSTERED INDEX IX_system_form_entities_OrganizationId ON system_form_entities(OrganizationId);
    CREATE NONCLUSTERED INDEX IX_system_form_entities_EntityName ON system_form_entities(EntityName);
    CREATE NONCLUSTERED INDEX IX_system_form_entities_TableName ON system_form_entities(TableName);
    CREATE NONCLUSTERED INDEX IX_system_form_entities_Category ON system_form_entities(Category);
    CREATE NONCLUSTERED INDEX IX_system_form_entities_SortOrder ON system_form_entities(SortOrder);

    PRINT '✅ Tabla system_form_entities creada con índices y FK';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_form_entities ya existe';
END

-- Insertar entidades base del sistema para FormDesigner
PRINT '📊 Insertando entidades base para FormDesigner...';

-- Variables para IDs (redeclaradas después de GO)
DECLARE @OrgId_FormEntities UNIQUEIDENTIFIER;
DECLARE @AdminUserId_FormEntities UNIQUEIDENTIFIER;

-- Obtener IDs existentes
SELECT @OrgId_FormEntities = Id FROM system_organization WHERE Nombre = 'Organización Base';
SELECT @AdminUserId_FormEntities = Id FROM system_users WHERE Email = 'admin@admin.cl';

-- ========================================
-- 🔐 PERMISOS ADICIONALES DE GESTIÓN
-- ========================================

PRINT '🔐 Agregando permisos de gestión avanzada...';

-- Variables para IDs (redeclaradas después de GO)
DECLARE @OrgId_Permisos UNIQUEIDENTIFIER;
DECLARE @AdminUserId_Permisos UNIQUEIDENTIFIER;
DECLARE @AdminRoleId_Permisos UNIQUEIDENTIFIER;

-- Obtener IDs existentes
SELECT @OrgId_Permisos = Id FROM system_organization WHERE Nombre = 'Organización Base';
SELECT @AdminUserId_Permisos = Id FROM system_users WHERE Email = 'admin@admin.cl';
SELECT @AdminRoleId_Permisos = Id FROM system_roles WHERE Nombre = 'Administrador';

-- SYSTEMROLE.MANAGEUSERS ya está incluido en la sección de permisos SYSTEMROLE arriba

-- Asignar nuevo permiso al rol Administrador
DECLARE @NewManageUsersPermissionId UNIQUEIDENTIFIER;
SELECT @NewManageUsersPermissionId = Id FROM system_permissions WHERE ActionKey = 'SYSTEMROLE.MANAGEUSERS';

IF NOT EXISTS (SELECT 1 FROM system_roles_permissions WHERE system_roles_id = @AdminRoleId_Permisos AND system_permissions_id = @NewManageUsersPermissionId)
BEGIN
    INSERT INTO system_roles_permissions (system_roles_id, system_permissions_id, OrganizationId, CreadorId, Active)
    VALUES (@AdminRoleId_Permisos, @NewManageUsersPermissionId, @OrgId_Permisos, @AdminUserId_Permisos, 1);
    PRINT '✅ Permiso SYSTEMROLE.MANAGEUSERS asignado al rol Administrador';
END

-- ========================================
-- 📋 TABLA: system_form_layouts (Form Designer)
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_form_layouts' AND xtype='U')
BEGIN
    PRINT '📋 Creando tabla system_form_layouts...';

    CREATE TABLE system_form_layouts (
        -- Identificador único
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,

        -- Información básica del layout
        EntityName NVARCHAR(100) NOT NULL,           -- "Region", "Empleado", etc.
        FormName NVARCHAR(255) NOT NULL,             -- "Formulario Region"
        Description NVARCHAR(500) NULL,              -- Descripción del layout

        -- Control de estado y versión
        IsDefault BIT DEFAULT 0 NOT NULL,            -- Si es el layout por defecto
        IsActive BIT DEFAULT 1 NOT NULL,             -- Si está activo
        Version INT DEFAULT 1 NOT NULL,              -- Control de versiones
        Active BIT DEFAULT 1 NOT NULL,             -- Si está activo

        -- Configuración completa del layout (JSON)
        LayoutConfig NVARCHAR(MAX) NOT NULL,         -- JSON con secciones y campos

        -- Auditoría estándar
        OrganizationId UNIQUEIDENTIFIER NULL,
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,

        -- Foreign Keys
        CONSTRAINT FK_system_form_layouts_OrganizationId
            FOREIGN KEY (OrganizationId) REFERENCES system_organization(Id),
        CONSTRAINT FK_system_form_layouts_CreadorId
            FOREIGN KEY (CreadorId) REFERENCES system_users(Id),
        CONSTRAINT FK_system_form_layouts_ModificadorId
            FOREIGN KEY (ModificadorId) REFERENCES system_users(Id),

        -- Constraints de negocio
        CONSTRAINT CHK_system_form_layouts_EntityName
            CHECK (EntityName IS NOT NULL AND LEN(LTRIM(RTRIM(EntityName))) > 0),
        CONSTRAINT CHK_system_form_layouts_FormName
            CHECK (FormName IS NOT NULL AND LEN(LTRIM(RTRIM(FormName))) > 0),
        CONSTRAINT CHK_system_form_layouts_LayoutConfig
            CHECK (LayoutConfig IS NOT NULL AND LEN(LTRIM(RTRIM(LayoutConfig))) > 0),
        CONSTRAINT CHK_system_form_layouts_Version
            CHECK (Version >= 1)
    );

    -- Índices para performance
    CREATE NONCLUSTERED INDEX IX_system_form_layouts_EntityName ON system_form_layouts(EntityName);
    CREATE NONCLUSTERED INDEX IX_system_form_layouts_OrganizationId ON system_form_layouts(OrganizationId);
    CREATE NONCLUSTERED INDEX IX_system_form_layouts_Active ON system_form_layouts(Active);
    CREATE NONCLUSTERED INDEX IX_system_form_layouts_Default ON system_form_layouts(EntityName, IsDefault)
        WHERE IsDefault = 1;
    CREATE NONCLUSTERED INDEX IX_system_form_layouts_FechaCreacion ON system_form_layouts(FechaCreacion);
    CREATE NONCLUSTERED INDEX IX_system_form_layouts_CreadorId ON system_form_layouts(CreadorId);

    -- Índice único para garantizar solo un layout default por entidad/organización
    CREATE UNIQUE NONCLUSTERED INDEX UX_system_form_layouts_DefaultUnique
        ON system_form_layouts(EntityName, OrganizationId)
        WHERE IsDefault = 1 AND IsActive = 1;

    -- Validación de JSON (SQL Server 2016+)
    ALTER TABLE system_form_layouts
        ADD CONSTRAINT CHK_system_form_layouts_ValidJSON
        CHECK (ISJSON(LayoutConfig) = 1);

    PRINT '✅ Tabla system_form_layouts creada con índices, FK y constraints';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_form_layouts ya existe';
END

-- ========================================
-- 🎨 SISTEMA DE CAMPOS PERSONALIZADOS
-- ========================================

PRINT '🚀 Iniciando creación del sistema de campos personalizados...';

-- ========================================
-- 📋 TABLA: system_custom_field_definitions
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

        -- Foreign Keys
        CONSTRAINT FK_system_custom_field_definitions_OrganizationId
            FOREIGN KEY (OrganizationId) REFERENCES system_organization(Id),
        CONSTRAINT FK_system_custom_field_definitions_CreadorId
            FOREIGN KEY (CreadorId) REFERENCES system_users(Id),
        CONSTRAINT FK_system_custom_field_definitions_ModificadorId
            FOREIGN KEY (ModificadorId) REFERENCES system_users(Id),

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

    -- Índices para system_custom_field_definitions
    CREATE NONCLUSTERED INDEX IX_system_custom_field_definitions_entity_org_active
        ON system_custom_field_definitions(EntityName, OrganizationId, Active, IsEnabled)
        INCLUDE (Id, FieldName, DisplayName, FieldType, SortOrder, IsRequired);
        
    CREATE NONCLUSTERED INDEX IX_system_custom_field_definitions_field_name
        ON system_custom_field_definitions(FieldName, EntityName, OrganizationId)
        WHERE Active = 1 AND IsEnabled = 1;
        
    CREATE NONCLUSTERED INDEX IX_system_custom_field_definitions_sort_order
        ON system_custom_field_definitions(EntityName, OrganizationId, SortOrder, Active)
        INCLUDE (FieldName, DisplayName, FieldType);

    PRINT '✅ Tabla system_custom_field_definitions creada con índices y FK';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_custom_field_definitions ya existe';
END

-- ========================================
-- 📋 TABLA: system_custom_field_audit_log
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

        -- Foreign Keys
        CONSTRAINT FK_system_custom_field_audit_OrganizationId
            FOREIGN KEY (OrganizationId) REFERENCES system_organization(Id),
        CONSTRAINT FK_system_custom_field_audit_CreadorId
            FOREIGN KEY (CreadorId) REFERENCES system_users(Id),

        -- Constraints
        CONSTRAINT CK_system_custom_field_audit_log_change_type
            CHECK (ChangeType IN ('CREATE', 'UPDATE', 'DELETE', 'ENABLE', 'DISABLE', 'MIGRATE')),

        -- Foreign Key
        CONSTRAINT FK_system_custom_field_audit_definition
            FOREIGN KEY (CustomFieldDefinitionId)
            REFERENCES system_custom_field_definitions(Id)
            ON DELETE CASCADE
    );

    -- Índices para system_custom_field_audit_log
    CREATE NONCLUSTERED INDEX IX_system_custom_field_audit_log_definition
        ON system_custom_field_audit_log(CustomFieldDefinitionId, FechaCreacion DESC);
    CREATE NONCLUSTERED INDEX IX_system_custom_field_audit_log_entity_date
        ON system_custom_field_audit_log(EntityName, OrganizationId, FechaCreacion DESC);

    PRINT '✅ Tabla system_custom_field_audit_log creada con índices y FK';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_custom_field_audit_log ya existe';
END

-- ========================================
-- 📋 TABLA: system_custom_field_templates
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
        Active BIT DEFAULT 1 NOT NULL,

        -- Foreign Keys
        CONSTRAINT FK_system_custom_field_templates_OrganizationId
            FOREIGN KEY (OrganizationId) REFERENCES system_organization(Id),
        CONSTRAINT FK_system_custom_field_templates_CreadorId
            FOREIGN KEY (CreadorId) REFERENCES system_users(Id),
        CONSTRAINT FK_system_custom_field_templates_ModificadorId
            FOREIGN KEY (ModificadorId) REFERENCES system_users(Id)
    );

    -- Índices para system_custom_field_templates
    CREATE NONCLUSTERED INDEX IX_system_custom_field_templates_target_entity
        ON system_custom_field_templates(TargetEntityName, OrganizationId, Active);
    CREATE NONCLUSTERED INDEX IX_system_custom_field_templates_category
        ON system_custom_field_templates(Category, Active);

    PRINT '✅ Tabla system_custom_field_templates creada con índices y FK';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_custom_field_templates ya existe';
END

-- ========================================
-- 🔧 FUNCIONES DE UTILIDAD
-- ========================================
PRINT '🔧 Creando funciones de utilidad...';

-- Función para validar JSON de configuración
GO
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

PRINT '✅ Sistema de campos personalizados implementado exitosamente';

-- ========================================
-- 🔍 SISTEMA DE BÚSQUEDAS AVANZADAS GUARDADAS
-- ========================================

PRINT '🚀 Iniciando creación del sistema de búsquedas avanzadas guardadas...';

-- ========================================
-- 📋 TABLA: system_saved_queries
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_saved_queries' AND xtype='U')
BEGIN
    PRINT '📋 Creando tabla system_saved_queries...';

    CREATE TABLE system_saved_queries (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,

        -- Información básica de la búsqueda guardada
        Name NVARCHAR(200) NOT NULL,                     -- "Empleados Activos Región Norte"
        Description NVARCHAR(500) NULL,                  -- Descripción de la búsqueda
        EntityName NVARCHAR(100) NOT NULL,               -- "Empleado", "Region", etc.

        -- Configuración JSON optimizada
        SelectedFields NVARCHAR(MAX) NOT NULL,           -- JSON array de field configurations
        FilterConfiguration NVARCHAR(MAX) NULL,         -- JSON object con filtros completos

        -- Configuración de consulta
        LogicalOperator TINYINT NOT NULL DEFAULT 0,      -- 0=And, 1=Or
        TakeLimit INT NOT NULL DEFAULT 50,               -- Límite de resultados

        -- Control de visibilidad y plantillas
        IsPublic BIT NOT NULL DEFAULT 0,                 -- Visible para toda la organización
        IsTemplate BIT NOT NULL DEFAULT 0,               -- Es una plantilla reutilizable

        -- Auditoría estándar (BaseEntity pattern)
        OrganizationId UNIQUEIDENTIFIER NULL,
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        Active BIT DEFAULT 1 NOT NULL,

        -- Foreign Keys
        CONSTRAINT FK_system_saved_queries_OrganizationId
            FOREIGN KEY (OrganizationId) REFERENCES system_organization(Id),
        CONSTRAINT FK_system_saved_queries_CreadorId
            FOREIGN KEY (CreadorId) REFERENCES system_users(Id),
        CONSTRAINT FK_system_saved_queries_ModificadorId
            FOREIGN KEY (ModificadorId) REFERENCES system_users(Id),

        -- Constraints de negocio
        CONSTRAINT CHK_system_saved_queries_Name
            CHECK (Name IS NOT NULL AND LEN(LTRIM(RTRIM(Name))) > 0),
        CONSTRAINT CHK_system_saved_queries_EntityName
            CHECK (EntityName IS NOT NULL AND LEN(LTRIM(RTRIM(EntityName))) > 0),
        CONSTRAINT CHK_system_saved_queries_SelectedFields
            CHECK (SelectedFields IS NOT NULL AND LEN(LTRIM(RTRIM(SelectedFields))) > 0),
        CONSTRAINT CHK_system_saved_queries_TakeLimit
            CHECK (TakeLimit > 0 AND TakeLimit <= 1000),
        CONSTRAINT CHK_system_saved_queries_LogicalOperator
            CHECK (LogicalOperator IN (0, 1))
    );

    -- Índices para system_saved_queries
    CREATE NONCLUSTERED INDEX IX_system_saved_queries_EntityName_Org_Active
        ON system_saved_queries(EntityName, OrganizationId, Active)
        INCLUDE (Id, Name, Description, IsPublic, IsTemplate);

    CREATE NONCLUSTERED INDEX IX_system_saved_queries_CreadorId_Active
        ON system_saved_queries(CreadorId, Active, OrganizationId)
        INCLUDE (Name, EntityName, FechaCreacion);

    CREATE NONCLUSTERED INDEX IX_system_saved_queries_Public_Active
        ON system_saved_queries(IsPublic, Active, OrganizationId, EntityName)
        WHERE IsPublic = 1;

    CREATE NONCLUSTERED INDEX IX_system_saved_queries_Template_Active
        ON system_saved_queries(IsTemplate, Active, EntityName)
        WHERE IsTemplate = 1;

    CREATE NONCLUSTERED INDEX IX_system_saved_queries_FechaCreacion
        ON system_saved_queries(FechaCreacion DESC)
        INCLUDE (Name, EntityName, CreadorId);

    -- Validación de JSON (SQL Server 2016+)
    ALTER TABLE system_saved_queries
        ADD CONSTRAINT CHK_system_saved_queries_ValidSelectedFieldsJSON
        CHECK (ISJSON(SelectedFields) = 1);

    ALTER TABLE system_saved_queries
        ADD CONSTRAINT CHK_system_saved_queries_ValidFilterJSON
        CHECK (FilterConfiguration IS NULL OR ISJSON(FilterConfiguration) = 1);

    PRINT '✅ Tabla system_saved_queries creada con índices, FK y constraints';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_saved_queries ya existe';
END

-- ========================================
-- 📋 TABLA: system_saved_query_shares
-- ========================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='system_saved_query_shares' AND xtype='U')
BEGIN
    PRINT '📋 Creando tabla system_saved_query_shares...';

    CREATE TABLE system_saved_query_shares (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,

        -- Referencia a la búsqueda guardada
        SavedQueryId UNIQUEIDENTIFIER NOT NULL,

        -- Destinatarios flexibles del compartido
        SharedWithUserId UNIQUEIDENTIFIER NULL,          -- Usuario específico
        SharedWithRoleId UNIQUEIDENTIFIER NULL,          -- Rol completo
        SharedWithOrganizationId UNIQUEIDENTIFIER NULL,  -- Organización cruzada (admin)

        -- Niveles de permisos granulares
        CanView BIT NOT NULL DEFAULT 1,                  -- Puede ver y ejecutar
        CanEdit BIT NOT NULL DEFAULT 0,                  -- Puede modificar
        CanExecute BIT NOT NULL DEFAULT 1,               -- Puede ejecutar consulta
        CanShare BIT NOT NULL DEFAULT 0,                 -- Puede compartir con otros

        -- Auditoría estándar (BaseEntity pattern)
        OrganizationId UNIQUEIDENTIFIER NULL,
        FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
        CreadorId UNIQUEIDENTIFIER NULL,
        ModificadorId UNIQUEIDENTIFIER NULL,
        Active BIT DEFAULT 1 NOT NULL,

        -- Foreign Keys
        CONSTRAINT FK_system_saved_query_shares_SavedQueryId
            FOREIGN KEY (SavedQueryId) REFERENCES system_saved_queries(Id) ON DELETE CASCADE,
        CONSTRAINT FK_system_saved_query_shares_SharedWithUserId
            FOREIGN KEY (SharedWithUserId) REFERENCES system_users(Id),
        CONSTRAINT FK_system_saved_query_shares_SharedWithRoleId
            FOREIGN KEY (SharedWithRoleId) REFERENCES system_roles(Id),
        CONSTRAINT FK_system_saved_query_shares_SharedWithOrganizationId
            FOREIGN KEY (SharedWithOrganizationId) REFERENCES system_organization(Id),
        CONSTRAINT FK_system_saved_query_shares_OrganizationId
            FOREIGN KEY (OrganizationId) REFERENCES system_organization(Id),
        CONSTRAINT FK_system_saved_query_shares_CreadorId
            FOREIGN KEY (CreadorId) REFERENCES system_users(Id),
        CONSTRAINT FK_system_saved_query_shares_ModificadorId
            FOREIGN KEY (ModificadorId) REFERENCES system_users(Id),

        -- Constraints de negocio
        CONSTRAINT CHK_system_saved_query_shares_OneTarget
            CHECK (
                (SharedWithUserId IS NOT NULL AND SharedWithRoleId IS NULL AND SharedWithOrganizationId IS NULL) OR
                (SharedWithUserId IS NULL AND SharedWithRoleId IS NOT NULL AND SharedWithOrganizationId IS NULL) OR
                (SharedWithUserId IS NULL AND SharedWithRoleId IS NULL AND SharedWithOrganizationId IS NOT NULL)
            )
    );

    -- Índices para system_saved_query_shares
    CREATE NONCLUSTERED INDEX IX_system_saved_query_shares_SavedQueryId_Active
        ON system_saved_query_shares(SavedQueryId, Active)
        INCLUDE (SharedWithUserId, SharedWithRoleId, CanView, CanEdit, CanExecute, CanShare);

    CREATE NONCLUSTERED INDEX IX_system_saved_query_shares_SharedWithUserId
        ON system_saved_query_shares(SharedWithUserId, Active)
        WHERE SharedWithUserId IS NOT NULL;

    CREATE NONCLUSTERED INDEX IX_system_saved_query_shares_SharedWithRoleId
        ON system_saved_query_shares(SharedWithRoleId, Active)
        WHERE SharedWithRoleId IS NOT NULL;

    CREATE NONCLUSTERED INDEX IX_system_saved_query_shares_CreadorId
        ON system_saved_query_shares(CreadorId, Active);

    -- Constraint único para evitar compartidos duplicados
    CREATE UNIQUE NONCLUSTERED INDEX UX_system_saved_query_shares_Unique
        ON system_saved_query_shares(SavedQueryId, SharedWithUserId, SharedWithRoleId, SharedWithOrganizationId)
        WHERE Active = 1;

    PRINT '✅ Tabla system_saved_query_shares creada con índices, FK y constraints';
END
ELSE
BEGIN
    PRINT '📄 Tabla system_saved_query_shares ya existe';
END

PRINT '✅ Sistema de búsquedas avanzadas guardadas implementado exitosamente';
PRINT '';
PRINT '🎯 SISTEMA COMPLETO CONSOLIDADO:';
PRINT '   • Sistema de usuarios, roles y permisos completo';
PRINT '   • Sistema de auditoría dinámico';
PRINT '   • Sistema de campos personalizados completo';
PRINT '   • FormDesigner con entidades base';
PRINT '   • Sistema de búsquedas avanzadas guardadas';
PRINT '   • Configuración completa de tokens y autenticación';
PRINT '   • Todos los permisos de gestión avanzada incluidos';
PRINT '';
PRINT '🚀 Base.sql consolidado - Listo para usar!';
PRINT '========================================';

-- Mostrar información de conexión
SELECT
    'admin@admin.cl' as Usuario,
    'U29wb3J0ZS4yMDE5UiZEbVNZdUwzQSM3NXR3NGlCa0BOcVJVI2pXISNabTM4TkJ6YTRKa3dlcHRZN2ZWaDRFVkBaRzdMTnhtOEs2VGY0dUhyUyR6UWNYQ1h2VHJAOE1kJDR4IyYkOSZaSmt0Qk4mYzk4VF5WNHE3UnpXNktVV3Ikc1Z5' as Password,
    'Organización Base' as Organizacion,
    'Administrador' as Rol,
    'SuperAdmin' as Permiso;

