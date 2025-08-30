-- ========================================
-- 🗄️  BASE DATABASE SCHEMA
-- Sistema de Usuarios, Roles y Permisos
-- ========================================

-- Verificar si la base de datos existe, si no, crearla
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'NewPOC')
BEGIN
    CREATE DATABASE NewPOC;
    PRINT '✅ Base de datos NewPOC creada exitosamente';
END
ELSE
BEGIN
    PRINT '📄 Base de datos NewPOC ya existe';
END

-- Usar la base de datos
USE NewPOC;

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
    INSERT INTO system_permissions (Id, Nombre, Descripcion, OrganizationId, Active)
    VALUES (@SuperAdminPermissionId, 'SuperAdmin', 'Acceso completo al sistema', @OrgId, 1);
    PRINT '✅ Permiso SuperAdmin creado';
END
ELSE
BEGIN
    SELECT @SuperAdminPermissionId = Id FROM system_permissions WHERE Nombre = 'SuperAdmin';
    PRINT '📄 Permiso SuperAdmin ya existe';
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
    VALUES (@AdminUserId, 'Administrador Sistema', 'Soporte.2019', 'admin@admin.cl', @OrgId, 1);
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

-- 6. Asignar Permiso al Rol
IF NOT EXISTS (SELECT 1 FROM system_roles_permissions WHERE system_roles_id = @AdminRoleId AND system_permissions_id = @SuperAdminPermissionId)
BEGIN
    INSERT INTO system_roles_permissions (system_roles_id, system_permissions_id, OrganizationId, CreadorId, Active)
    VALUES (@AdminRoleId, @SuperAdminPermissionId, @OrgId, @AdminUserId, 1);
    PRINT '✅ Permiso SuperAdmin asignado al rol Administrador';
END
ELSE
BEGIN
    PRINT '📄 Permiso ya asignado al rol';
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
PRINT '🔑 Password: Soporte.2019';
PRINT '👥 Rol: Administrador';
PRINT '🔐 Permiso: SuperAdmin';
PRINT '';
PRINT '📊 Tablas creadas:';
PRINT '   • system_organization';
PRINT '   • system_users';
PRINT '   • system_permissions';
PRINT '   • system_roles';
PRINT '   • system_users_roles';
PRINT '   • system_users_permissions';
PRINT '   • system_roles_permissions';
PRINT '';
PRINT '🎯 Listo para usar con el generador de modelos Python!';
PRINT '========================================';

-- Mostrar información de conexión
SELECT 
    'admin@admin.cl' as Usuario,
    'Soporte.2019' as Password,
    'Organización Base' as Organizacion,
    'Administrador' as Rol,
    'SuperAdmin' as Permiso;