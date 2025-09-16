using Microsoft.EntityFrameworkCore;
using Backend.Utils.Data;
using Shared.Models.Entities.SystemEntities;

namespace CustomFields.API.Services;

/// <summary>
/// Servicio para gestionar permisos de campos personalizados
/// </summary>
public class CustomFieldPermissionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CustomFieldPermissionService> _logger;

    public CustomFieldPermissionService(AppDbContext context, ILogger<CustomFieldPermissionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Genera permisos automáticamente para un campo personalizado
    /// Patrón: {EntityName}.{FieldName}.{Action}
    /// </summary>
    public async Task<bool> GeneratePermissionsForFieldAsync(SystemCustomFieldDefinitions field)
    {
        try
        {
            var entityName = field.EntityName;
            var fieldName = field.FieldName;
            var organizationId = field.OrganizationId;

            var permissions = new List<(string action, string description)>
            {
                ("VIEW", $"Ver campo '{field.DisplayName}' en {entityName}"),
                ("CREATE", $"Crear valores para campo '{field.DisplayName}' en {entityName}"),
                ("UPDATE", $"Actualizar valores del campo '{field.DisplayName}' en {entityName}")
            };

            foreach (var (action, description) in permissions)
            {
                var permissionName = $"{entityName}.{fieldName}.{action}";

                // Verificar si el permiso ya existe
                var existingPermission = await _context.SystemPermissions
                    .FirstOrDefaultAsync(p =>
                        p.Nombre == permissionName &&
                        p.OrganizationId == organizationId &&
                        p.Active);

                if (existingPermission == null)
                {
                    var newPermission = new SystemPermissions
                    {
                        Id = Guid.NewGuid(),
                        Nombre = permissionName,
                        Descripcion = description,
                        ActionKey = $"CUSTOM_FIELD.{fieldName}.{action}",
                        GroupKey = "CUSTOM_FIELDS",
                        GrupoNombre = "Campos Personalizados",
                        FechaCreacion = DateTime.UtcNow,
                        FechaModificacion = DateTime.UtcNow,
                        OrganizationId = organizationId,
                        Active = true
                    };

                    _context.SystemPermissions.Add(newPermission);
                    _logger.LogInformation("Creado permiso: {PermissionName}", permissionName);
                }
                else
                {
                    _logger.LogDebug("Permiso ya existe: {PermissionName}", permissionName);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando permisos para campo {FieldName}", field.FieldName);
            return false;
        }
    }

    /// <summary>
    /// Actualiza los permisos almacenados en el campo personalizado
    /// </summary>
    public async Task UpdateFieldPermissionsAsync(SystemCustomFieldDefinitions field)
    {
        try
        {
            var entityName = field.EntityName;
            var fieldName = field.FieldName;

            // Generar nombres de permisos
            field.PermissionView = $"{entityName}.{fieldName}.VIEW";
            field.PermissionCreate = $"{entityName}.{fieldName}.CREATE";
            field.PermissionUpdate = $"{entityName}.{fieldName}.UPDATE";

            _context.SystemCustomFieldDefinitions.Update(field);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Permisos actualizados para campo {FieldName}", fieldName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando permisos del campo {FieldName}", field.FieldName);
        }
    }

    /// <summary>
    /// Elimina permisos cuando se elimina un campo personalizado
    /// </summary>
    public async Task DeletePermissionsForFieldAsync(string entityName, string fieldName, Guid? organizationId)
    {
        try
        {
            var permissionNames = new[]
            {
                $"{entityName}.{fieldName}.VIEW",
                $"{entityName}.{fieldName}.CREATE",
                $"{entityName}.{fieldName}.UPDATE"
            };

            var permissions = await _context.SystemPermissions
                .Where(p => permissionNames.Contains(p.Nombre) &&
                           p.OrganizationId == organizationId &&
                           p.Active)
                .ToListAsync();

            foreach (var permission in permissions)
            {
                permission.Active = false;
                permission.FechaModificacion = DateTime.UtcNow;
            }

            if (permissions.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Eliminados {Count} permisos para campo {FieldName}",
                    permissions.Count, fieldName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando permisos para campo {FieldName}", fieldName);
        }
    }

    /// <summary>
    /// Verifica si un usuario tiene permiso para una acción específica en un campo
    /// </summary>
    public async Task<bool> HasFieldPermissionAsync(Guid userId, string entityName, string fieldName,
        string action, Guid organizationId)
    {
        try
        {
            var permissionName = $"{entityName}.{fieldName}.{action}";

            // Verificar permisos directos del usuario
            var hasDirectPermission = await _context.SystemUsersPermissions
                .AnyAsync(up => up.SystemUsersId == userId &&
                               up.SystemPermissions.Nombre == permissionName &&
                               up.SystemPermissions.OrganizationId == organizationId &&
                               up.Active && up.SystemPermissions.Active);

            if (hasDirectPermission) return true;

            // Verificar permisos por roles
            var hasRolePermission = await _context.SystemUsersRoles
                .Where(ur => ur.SystemUsersId == userId && ur.Active)
                .SelectMany(ur => ur.SystemRoles.SystemRolesPermissions)
                .AnyAsync(rp => rp.SystemPermissions.Nombre == permissionName &&
                               rp.SystemPermissions.OrganizationId == organizationId &&
                               rp.Active && rp.SystemPermissions.Active);

            return hasRolePermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando permiso {Permission} para usuario {UserId}",
                $"{entityName}.{fieldName}.{action}", userId);
            return false;
        }
    }

    /// <summary>
    /// Obtiene todos los campos personalizados visibles para un usuario
    /// </summary>
    public async Task<List<SystemCustomFieldDefinitions>> GetVisibleFieldsForUserAsync(
        Guid userId, string entityName, Guid organizationId)
    {
        try
        {
            var query = _context.SystemCustomFieldDefinitions
                .Where(f => f.EntityName == entityName && f.Active && f.IsEnabled);

            if (organizationId != Guid.Empty)
            {
                query = query.Where(f => f.OrganizationId == organizationId || f.OrganizationId == null);
            }

            var fields = await query.ToListAsync();
            var visibleFields = new List<SystemCustomFieldDefinitions>();

            foreach (var field in fields)
            {
                // Si no tiene permisos configurados, es visible por defecto
                if (string.IsNullOrEmpty(field.PermissionView))
                {
                    visibleFields.Add(field);
                    continue;
                }

                // Verificar permiso VIEW
                var hasPermission = await HasFieldPermissionAsync(userId, entityName, field.FieldName, "VIEW", organizationId);
                if (hasPermission)
                {
                    visibleFields.Add(field);
                }
            }

            return visibleFields;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo campos visibles para usuario {UserId}", userId);
            return new List<SystemCustomFieldDefinitions>();
        }
    }
}