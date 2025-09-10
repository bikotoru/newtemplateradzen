-- ====================================
-- 🚀 AGREGAR PERMISO SYSTEMROLE.MANAGEUSERS
-- Script para agregar permiso de gestión de usuarios en roles
-- ====================================

DECLARE @OrgId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM system_organization WHERE Active = 1);

-- Verificar si ya existe el permiso
IF NOT EXISTS (SELECT 1 FROM system_permissions WHERE ActionKey = 'SYSTEMROLE.MANAGEUSERS')
BEGIN
    PRINT '👥 Agregando permiso SYSTEMROLE.MANAGEUSERS...';
    
    INSERT INTO system_permissions (Nombre, Descripcion, OrganizationId, Active, ActionKey, GroupKey, GrupoNombre)
    VALUES 
    ('SYSTEMROLE.MANAGEUSERS', 'Gestionar usuarios de roles', @OrgId, 1, 'SYSTEMROLE.MANAGEUSERS', 'SYSTEMROLE', 'SystemRole');
    
    PRINT '✅ Permiso SYSTEMROLE.MANAGEUSERS agregado exitosamente';
END
ELSE
BEGIN
    PRINT '⚠️  El permiso SYSTEMROLE.MANAGEUSERS ya existe';
END

-- Verificar si el SuperAdmin tiene este permiso
DECLARE @SuperAdminRoleId UNIQUEIDENTIFIER = (
    SELECT TOP 1 Id 
    FROM system_roles 
    WHERE LOWER(Nombre) LIKE '%superadmin%' OR LOWER(Nombre) LIKE '%super admin%' 
    ORDER BY FechaCreacion ASC
);

IF @SuperAdminRoleId IS NOT NULL
BEGIN
    DECLARE @PermissionId UNIQUEIDENTIFIER = (
        SELECT Id FROM system_permissions WHERE ActionKey = 'SYSTEMROLE.MANAGEUSERS'
    );
    
    -- Agregar permiso al SuperAdmin si no lo tiene
    IF NOT EXISTS (
        SELECT 1 FROM system_roles_permissions 
        WHERE SystemRoleId = @SuperAdminRoleId AND SystemPermissionId = @PermissionId
    )
    BEGIN
        PRINT '🛡️ Asignando permiso SYSTEMROLE.MANAGEUSERS al SuperAdmin...';
        
        INSERT INTO system_roles_permissions (SystemRoleId, SystemPermissionId, FechaCreacion, Active)
        VALUES (@SuperAdminRoleId, @PermissionId, GETDATE(), 1);
        
        PRINT '✅ Permiso asignado al SuperAdmin exitosamente';
    END
    ELSE
    BEGIN
        PRINT '⚠️  El SuperAdmin ya tiene el permiso SYSTEMROLE.MANAGEUSERS';
    END
END
ELSE
BEGIN
    PRINT '⚠️  No se encontró el rol SuperAdmin';
END

PRINT '';
PRINT '🎉 ¡Proceso completado!';
PRINT '   Ahora puedes gestionar usuarios desde el formulario de roles';
PRINT '';