-- ========================================
-- 🔐 AGREGAR PERMISOS DE GESTIÓN
-- Sistema de Roles y Usuarios - Permisos Avanzados
-- ========================================

USE NewPOC;

PRINT '🔐 Agregando permisos de gestión avanzada...';

-- Obtener OrganizationId de la organización base
DECLARE @OrgId UNIQUEIDENTIFIER;
SELECT @OrgId = Id FROM system_organization WHERE Nombre = 'Organización Base';

-- ========================================
-- 🛡️ PERMISOS PARA SYSTEMROLE - GESTIÓN DE PERMISOS
-- ========================================

PRINT '📊 Insertando permisos SYSTEMROLE - Gestión de Permisos...';

-- Verificar si ya existen los permisos de gestión
IF NOT EXISTS (SELECT 1 FROM system_permissions WHERE ActionKey = 'SYSTEMROLE.MANAGEPERMISSIONS')
BEGIN
    INSERT INTO system_permissions (Nombre, Descripcion, OrganizationId, Active, ActionKey, GroupKey, GrupoNombre)
    VALUES 
    ('SYSTEMROLE.MANAGEPERMISSIONS', 'Gestionar permisos de roles', @OrgId, 1, 'SYSTEMROLE.MANAGEPERMISSIONS', 'SYSTEMROLE', 'SystemRole'),
    ('SYSTEMROLE.ADDPERMISSIONS', 'Agregar permisos a roles', @OrgId, 1, 'SYSTEMROLE.ADDPERMISSIONS', 'SYSTEMROLE', 'SystemRole'),
    ('SYSTEMROLE.REMOVEPERMISSIONS', 'Remover permisos de roles', @OrgId, 1, 'SYSTEMROLE.REMOVEPERMISSIONS', 'SYSTEMROLE', 'SystemRole');
    
    PRINT '✅ Permisos SYSTEMROLE - Gestión de Permisos creados (3)';
END
ELSE
BEGIN
    PRINT '📄 Permisos SYSTEMROLE - Gestión de Permisos ya existen';
END

-- ========================================
-- 👤 PERMISOS PARA SYSTEMUSER - GESTIÓN DE ROLES
-- ========================================

PRINT '📊 Insertando permisos SYSTEMUSER - Gestión de Roles...';

-- Verificar si ya existen los permisos de gestión de roles
IF NOT EXISTS (SELECT 1 FROM system_permissions WHERE ActionKey = 'SYSTEMUSER.MANAGEROLES')
BEGIN
    INSERT INTO system_permissions (Nombre, Descripcion, OrganizationId, Active, ActionKey, GroupKey, GrupoNombre)
    VALUES 
    ('SYSTEMUSER.MANAGEROLES', 'Gestionar roles de usuarios', @OrgId, 1, 'SYSTEMUSER.MANAGEROLES', 'SYSTEMUSER', 'SystemUser'),
    ('SYSTEMUSER.ADDROLES', 'Agregar roles a usuarios', @OrgId, 1, 'SYSTEMUSER.ADDROLES', 'SYSTEMUSER', 'SystemUser'),
    ('SYSTEMUSER.REMOVEROLES', 'Remover roles de usuarios', @OrgId, 1, 'SYSTEMUSER.REMOVEROLES', 'SYSTEMUSER', 'SystemUser');
    
    PRINT '✅ Permisos SYSTEMUSER - Gestión de Roles creados (3)';
END
ELSE
BEGIN
    PRINT '📄 Permisos SYSTEMUSER - Gestión de Roles ya existen';
END

-- ========================================
-- 👥 PERMISOS PARA SYSTEMUSER - GESTIÓN DE PERMISOS DIRECTOS
-- ========================================

PRINT '📊 Insertando permisos SYSTEMUSER - Gestión de Permisos Directos...';

-- Verificar si ya existen los permisos de gestión de permisos directos
IF NOT EXISTS (SELECT 1 FROM system_permissions WHERE ActionKey = 'SYSTEMUSER.MANAGEPERMISSIONS')
BEGIN
    INSERT INTO system_permissions (Nombre, Descripcion, OrganizationId, Active, ActionKey, GroupKey, GrupoNombre)
    VALUES 
    ('SYSTEMUSER.MANAGEPERMISSIONS', 'Gestionar permisos directos de usuarios', @OrgId, 1, 'SYSTEMUSER.MANAGEPERMISSIONS', 'SYSTEMUSER', 'SystemUser'),
    ('SYSTEMUSER.ADDPERMISSIONS', 'Agregar permisos directos a usuarios', @OrgId, 1, 'SYSTEMUSER.ADDPERMISSIONS', 'SYSTEMUSER', 'SystemUser'),
    ('SYSTEMUSER.REMOVEPERMISSIONS', 'Remover permisos directos de usuarios', @OrgId, 1, 'SYSTEMUSER.REMOVEPERMISSIONS', 'SYSTEMUSER', 'SystemUser');
    
    PRINT '✅ Permisos SYSTEMUSER - Gestión de Permisos Directos creados (3)';
END
ELSE
BEGIN
    PRINT '📄 Permisos SYSTEMUSER - Gestión de Permisos Directos ya existen';
END

-- ========================================
-- 🔗 ASIGNAR NUEVOS PERMISOS AL ROL ADMINISTRADOR
-- ========================================

PRINT '📊 Asignando nuevos permisos al rol Administrador...';

-- Obtener IDs necesarios
DECLARE @AdminRoleId UNIQUEIDENTIFIER;
DECLARE @AdminUserId UNIQUEIDENTIFIER;

SELECT @AdminRoleId = Id FROM system_roles WHERE Nombre = 'Administrador';
SELECT @AdminUserId = Id FROM system_users WHERE Email = 'admin@admin.cl';

-- Lista de permisos nuevos a asignar
DECLARE @NewPermissions TABLE (ActionKey NVARCHAR(255));

INSERT INTO @NewPermissions VALUES 
('SYSTEMROLE.MANAGEPERMISSIONS'),
('SYSTEMROLE.ADDPERMISSIONS'),
('SYSTEMROLE.REMOVEPERMISSIONS'),
('SYSTEMUSER.MANAGEROLES'),
('SYSTEMUSER.ADDROLES'),
('SYSTEMUSER.REMOVEROLES'),
('SYSTEMUSER.MANAGEPERMISSIONS'),
('SYSTEMUSER.ADDPERMISSIONS'),
('SYSTEMUSER.REMOVEPERMISSIONS');

-- Asignar permisos al rol Administrador
DECLARE @PermissionId UNIQUEIDENTIFIER;
DECLARE @ActionKey NVARCHAR(255);
DECLARE @AssignedCount INT = 0;

DECLARE permission_cursor CURSOR FOR 
SELECT p.Id, np.ActionKey
FROM @NewPermissions np
INNER JOIN system_permissions p ON p.ActionKey = np.ActionKey
WHERE NOT EXISTS (
    SELECT 1 FROM system_roles_permissions srp 
    WHERE srp.system_roles_id = @AdminRoleId 
    AND srp.system_permissions_id = p.Id
    AND srp.Active = 1
);

OPEN permission_cursor;
FETCH NEXT FROM permission_cursor INTO @PermissionId, @ActionKey;

WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO system_roles_permissions (system_roles_id, system_permissions_id, OrganizationId, CreadorId, Active)
    VALUES (@AdminRoleId, @PermissionId, @OrgId, @AdminUserId, 1);
    
    SET @AssignedCount = @AssignedCount + 1;
    PRINT '✅ Permiso ' + @ActionKey + ' asignado al rol Administrador';
    
    FETCH NEXT FROM permission_cursor INTO @PermissionId, @ActionKey;
END

CLOSE permission_cursor;
DEALLOCATE permission_cursor;

PRINT '✅ Total de nuevos permisos asignados: ' + CAST(@AssignedCount AS NVARCHAR(10));

-- ========================================
-- 📊 RESUMEN DE PERMISOS CREADOS
-- ========================================

PRINT '';
PRINT '========================================';
PRINT '✅ PERMISOS DE GESTIÓN AGREGADOS';
PRINT '========================================';
PRINT '';

-- Contar permisos por grupo
SELECT 
    GroupKey as 'Grupo',
    GrupoNombre as 'Nombre del Grupo',
    COUNT(*) as 'Total Permisos'
FROM system_permissions 
WHERE Active = 1 
AND OrganizationId = @OrgId
GROUP BY GroupKey, GrupoNombre
ORDER BY GroupKey;

PRINT '';
PRINT '🛡️ NUEVOS PERMISOS SYSTEMROLE:';
PRINT '   • SYSTEMROLE.MANAGEPERMISSIONS - Gestionar permisos de roles';
PRINT '   • SYSTEMROLE.ADDPERMISSIONS - Agregar permisos a roles';
PRINT '   • SYSTEMROLE.REMOVEPERMISSIONS - Remover permisos de roles';
PRINT '';
PRINT '👤 NUEVOS PERMISOS SYSTEMUSER (Roles):';
PRINT '   • SYSTEMUSER.MANAGEROLES - Gestionar roles de usuarios';
PRINT '   • SYSTEMUSER.ADDROLES - Agregar roles a usuarios';
PRINT '   • SYSTEMUSER.REMOVEROLES - Remover roles de usuarios';
PRINT '';
PRINT '👥 NUEVOS PERMISOS SYSTEMUSER (Permisos Directos):';
PRINT '   • SYSTEMUSER.MANAGEPERMISSIONS - Gestionar permisos directos';
PRINT '   • SYSTEMUSER.ADDPERMISSIONS - Agregar permisos directos';
PRINT '   • SYSTEMUSER.REMOVEPERMISSIONS - Remover permisos directos';
PRINT '';
PRINT '🎯 Todos los permisos asignados al rol Administrador';
PRINT '========================================';