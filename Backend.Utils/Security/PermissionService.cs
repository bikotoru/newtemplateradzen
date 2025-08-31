using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;
using Shared.Models.DTOs.Auth;
using Shared.Models.Entities;

namespace Backend.Utils.Security
{
    public class PermissionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PermissionService> _logger;
        private readonly TokenCacheService _tokenCache;

        public PermissionService(AppDbContext context, ILogger<PermissionService> logger, TokenCacheService tokenCache)
        {
            _context = context;
            _logger = logger;
            _tokenCache = tokenCache;
        }

        /// <summary>
        /// Obtiene todos los permisos únicos del usuario (directos + por roles)
        /// </summary>
        public async Task<List<string>> GetUserPermissionsAsync(Guid userId, Guid organizationId)
        {
            try
            {
                // Permisos directos del usuario
                var directPermissions = await _context.SystemUsersPermissions
                    .Where(up => up.SystemUsersId == userId && up.Active)
                    .Include(up => up.SystemPermissions)
                    .Where(up => up.SystemPermissions.Active && 
                                up.SystemPermissions.OrganizationId == organizationId &&
                                !string.IsNullOrEmpty(up.SystemPermissions.ActionKey))
                    .Select(up => up.SystemPermissions.ActionKey!)
                    .ToListAsync();

                // Permisos por roles
                var rolePermissions = await _context.SystemUsersRoles
                    .Where(ur => ur.SystemUsersId == userId && ur.Active)
                    .Include(ur => ur.SystemRoles)
                    .ThenInclude(r => r.SystemRolesPermissions)
                    .ThenInclude(rp => rp.SystemPermissions)
                    .Where(ur => ur.SystemRoles.Active && ur.SystemRoles.OrganizationId == organizationId)
                    .SelectMany(ur => ur.SystemRoles.SystemRolesPermissions
                        .Where(rp => rp.Active && rp.SystemPermissions.Active && 
                                    !string.IsNullOrEmpty(rp.SystemPermissions.ActionKey))
                        .Select(rp => rp.SystemPermissions.ActionKey!))
                    .ToListAsync();

                // Combinar y deduplicar
                var allPermissions = directPermissions
                    .Concat(rolePermissions)
                    .Distinct()
                    .ToList();

                _logger.LogDebug("Usuario {UserId} tiene {Count} permisos únicos en organización {OrgId}", 
                    userId, allPermissions.Count, organizationId);

                return allPermissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo permisos para usuario {UserId} en organización {OrgId}", 
                    userId, organizationId);
                return new List<string>();
            }
        }

        /// <summary>
        /// Construye SessionDataDto completo con permisos
        /// </summary>
        public async Task<SessionDataDto> BuildSessionDataAsync(Guid userId, Guid organizationId)
        {
            try
            {
                // Obtener usuario
                var user = await _context.SystemUsers
                    .FirstOrDefaultAsync(u => u.Id == userId && u.Active);

                if (user == null)
                    throw new InvalidOperationException($"Usuario {userId} no encontrado");

                // Obtener organización
                var organization = await _context.SystemOrganization
                    .FirstOrDefaultAsync(o => o.Id == organizationId && o.Active);

                if (organization == null)
                    throw new InvalidOperationException($"Organización {organizationId} no encontrada");

                // Obtener roles del usuario en esta organización
                var roles = await _context.SystemUsersRoles
                    .Where(ur => ur.SystemUsersId == userId && ur.Active)
                    .Include(ur => ur.SystemRoles)
                    .Where(ur => ur.SystemRoles.Active && ur.SystemRoles.OrganizationId == organizationId)
                    .Select(ur => new RoleDto
                    {
                        Id = ur.SystemRoles.Id.ToString(),
                        Nombre = ur.SystemRoles.Nombre
                    })
                    .ToListAsync();

                // Obtener permisos únicos
                var permisos = await GetUserPermissionsAsync(userId, organizationId);

                return new SessionDataDto
                {
                    Nombre = user.Nombre,
                    Id = user.Id.ToString(),
                    Roles = roles,
                    Permisos = permisos,
                    Organization = new OrganizationDto
                    {
                        Id = organization.Id.ToString(),
                        Nombre = organization.Nombre
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error construyendo SessionData para usuario {UserId} en organización {OrgId}", 
                    userId, organizationId);
                throw;
            }
        }

        /// <summary>
        /// Valida usuario desde headers y retorna SessionDataDto
        /// </summary>
        public async Task<SessionDataDto?> ValidateUserFromHeadersAsync(Microsoft.AspNetCore.Http.IHeaderDictionary headers)
        {
            try
            {
                var tokenId = ExtractTokenFromHeaders(headers);
                if (tokenId == null) return null;

                var sessionData = await _tokenCache.GetTokenDataAsync(tokenId.Value);
                if (sessionData == null) return null;

                // Si necesita refresh, actualizar datos
                var userId = Guid.Parse(sessionData.Id);
                var organizationId = Guid.Parse(sessionData.Organization.Id);

                try
                {
                    // Intentar refrescar si es necesario (esto lanzará excepción si el token necesita refresh)
                    await _tokenCache.GetTokenDataAsync(tokenId.Value);
                }
                catch (SessionExpiredException)
                {
                    throw; // Re-lanzar para manejo específico
                }

                return sessionData;
            }
            catch (SessionExpiredException)
            {
                throw; // Re-lanzar excepciones de sesión
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando usuario desde headers");
                return null;
            }
        }

        /// <summary>
        /// Extrae el token ID desde los headers
        /// </summary>
        private Guid? ExtractTokenFromHeaders(Microsoft.AspNetCore.Http.IHeaderDictionary headers)
        {
            try
            {
                var authHeader = headers.Authorization.FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader))
                    return null;

                var token = authHeader.StartsWith("Bearer ") 
                    ? authHeader.Substring(7) 
                    : authHeader;

                return Guid.TryParse(token, out var tokenId) ? tokenId : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo token desde headers");
                return null;
            }
        }

        /// <summary>
        /// Refresca automáticamente un token que necesita actualización
        /// </summary>
        public async Task<bool> RefreshTokenIfNeededAsync(Guid tokenId)
        {
            try
            {
                var token = await _context.ZToken.FirstOrDefaultAsync(t => t.Id == tokenId);
                if (token?.Refresh != true) return true; // No necesita refresh

                if (token.Organizationid == null) return false;

                // Necesitamos reconstruir la sesión - pero necesitamos organizationId
                // Como el campo está mal nombrado, Organizationid contiene userId
                var userId = token.Organizationid.Value;
                
                // Buscar organización del usuario (asumiendo una por ahora)
                var userOrg = await _context.SystemUsers
                    .Include(u => u.Organization)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (userOrg?.Organization == null) return false;

                var newSessionData = await BuildSessionDataAsync(userId, userOrg.Organization.Id);
                return await _tokenCache.RefreshTokenDataAsync(tokenId, newSessionData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refrescando token {TokenId}", tokenId);
                return false;
            }
        }
    }
}