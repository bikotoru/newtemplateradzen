using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;
using Shared.Models.Entities;
using Shared.Models.DTOs.Auth;

namespace Backend.Utils.Security
{
    public class TokenCacheService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TokenCacheService> _logger;

        public TokenCacheService(AppDbContext context, ILogger<TokenCacheService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Crea o actualiza un token de cache para el usuario
        /// </summary>
        public async Task<Guid> CreateOrUpdateTokenAsync(Guid userId, Guid organizationId, SessionDataDto sessionData)
        {
            try
            {
                var tokenId = Guid.NewGuid();
                var serializedData = JsonSerializer.Serialize(sessionData);

                // Eliminar tokens anteriores del usuario en esta organización
                var existingTokens = await _context.ZToken
                    .Where(t => t.Organizationid == organizationId && t.Data != null && t.Data.Contains($"\"Id\":\"{userId}\""))
                    .ToListAsync();

                if (existingTokens.Any())
                {
                    _context.ZToken.RemoveRange(existingTokens);
                }

                // Crear nuevo token
                var newToken = new ZToken
                {
                    Id = tokenId,
                    Organizationid = organizationId,
                    Data = serializedData,
                    Refresh = false,
                    Logout = false
                };

                _context.ZToken.Add(newToken);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Token creado para usuario {UserId} en organización {OrgId}", userId, organizationId);
                return tokenId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando token para usuario {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Obtiene los datos del token si es válido
        /// </summary>
        public async Task<SessionDataDto?> GetTokenDataAsync(Guid tokenId)
        {
            try
            {
                var token = await _context.ZToken
                    .FirstOrDefaultAsync(t => t.Id == tokenId);

                if (token == null)
                {
                    _logger.LogWarning("Token {TokenId} no encontrado", tokenId);
                    return null;
                }

                if (token.Logout == true)
                {
                    _logger.LogWarning("Token {TokenId} marcado para logout", tokenId);
                    throw new SessionExpiredException("SESSION_FORCE_LOGOUT");
                }

                if (token.Refresh == true)
                {
                    _logger.LogInformation("Token {TokenId} necesita refresh", tokenId);
                    // El servicio que llame debe manejar el refresh
                }

                if (string.IsNullOrEmpty(token.Data))
                {
                    _logger.LogWarning("Token {TokenId} sin datos", tokenId);
                    return null;
                }

                var sessionData = JsonSerializer.Deserialize<SessionDataDto>(token.Data);
                return sessionData;
            }
            catch (SessionExpiredException)
            {
                throw; // Re-lanzar excepciones específicas de sesión
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo datos del token {TokenId}", tokenId);
                return null;
            }
        }

        /// <summary>
        /// Marca usuarios para refresh cuando cambian permisos de rol
        /// </summary>
        public async Task MarkUsersForRefreshByRoleAsync(Guid roleId)
        {
            try
            {
                // Obtener usuarios que tienen este rol
                var userIds = await _context.SystemUsersRoles
                    .Where(ur => ur.SystemRolesId == roleId && ur.Active)
                    .Select(ur => ur.SystemUsersId)
                    .ToListAsync();

                if (!userIds.Any()) return;

                // Marcar tokens para refresh - buscar por userId en Data JSON
                var tokens = new List<ZToken>();
                foreach(var userId in userIds)
                {
                    var userTokens = await _context.ZToken
                        .Where(t => t.Data != null && t.Data.Contains($"\"Id\":\"{userId}\""))
                        .ToListAsync();
                    tokens.AddRange(userTokens);
                }

                foreach (var token in tokens)
                {
                    token.Refresh = true;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Marcados {Count} tokens para refresh por cambio en rol {RoleId}", tokens.Count, roleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando tokens para refresh por rol {RoleId}", roleId);
                throw;
            }
        }

        /// <summary>
        /// Marca usuario específico para refresh
        /// </summary>
        public async Task MarkUserForRefreshAsync(Guid userId)
        {
            try
            {
                var tokens = await _context.ZToken
                    .Where(t => t.Data != null && t.Data.Contains($"\"Id\":\"{userId}\""))
                    .ToListAsync();

                foreach (var token in tokens)
                {
                    token.Refresh = true;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Marcados {Count} tokens para refresh para usuario {UserId}", tokens.Count, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando tokens para refresh para usuario {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Marca usuario para logout forzado
        /// </summary>
        public async Task MarkUserForLogoutAsync(Guid userId)
        {
            try
            {
                var tokens = await _context.ZToken
                    .Where(t => t.Data != null && t.Data.Contains($"\"Id\":\"{userId}\""))
                    .ToListAsync();

                foreach (var token in tokens)
                {
                    token.Logout = true;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Marcados {Count} tokens para logout para usuario {UserId}", tokens.Count, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando tokens para logout para usuario {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Refresca los datos del token
        /// </summary>
        public async Task<bool> RefreshTokenDataAsync(Guid tokenId, SessionDataDto newSessionData)
        {
            try
            {
                var token = await _context.ZToken
                    .FirstOrDefaultAsync(t => t.Id == tokenId);

                if (token == null) return false;

                token.Data = JsonSerializer.Serialize(newSessionData);
                token.Refresh = false; // Limpiar flag de refresh

                await _context.SaveChangesAsync();
                _logger.LogInformation("Token {TokenId} refrescado exitosamente", tokenId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refrescando token {TokenId}", tokenId);
                return false;
            }
        }

        /// <summary>
        /// Limpia tokens expirados (llamar periódicamente)
        /// </summary>
        public async Task CleanupExpiredTokensAsync()
        {
            try
            {
                // Para implementar después - agregar campo de expiración real a la tabla
                // Por ahora, limpiar tokens marcados para logout hace más de 24 horas
                var cutoffDate = DateTime.UtcNow.AddHours(-24);
                
                // Esta query no funcionará sin un campo de fecha, pero es conceptual
                // await _context.ZToken.Where(t => t.Logout == true && t.UpdatedAt < cutoffDate).DeleteAsync();
                
                _logger.LogInformation("Limpieza de tokens completada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en limpieza de tokens");
            }
        }
    }

    /// <summary>
    /// Excepción específica para sesiones expiradas
    /// </summary>
    public class SessionExpiredException : Exception
    {
        public string ErrorCode { get; }
        
        public SessionExpiredException(string errorCode) : base(errorCode)
        {
            ErrorCode = errorCode;
        }
    }
}