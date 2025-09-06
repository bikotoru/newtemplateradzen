using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Models.DTOs.Auth;
using Shared.Models.Security;

namespace Backend.Utils.Security
{
    /// <summary>
    /// Servicio para encriptar y desencriptar tokens con doble verificación usando UnifiedEncryption
    /// </summary>
    public class TokenEncryptionService
    {
        private readonly string _secretKey;
        private readonly ILogger<TokenEncryptionService> _logger;

        public TokenEncryptionService(IConfiguration configuration, ILogger<TokenEncryptionService> logger)
        {
            _secretKey = configuration["SECRETKEY"] ?? throw new InvalidOperationException("SECRETKEY not configured");
            _logger = logger;
        }

        /// <summary>
        /// Genera un token encriptado a partir de los datos del token
        /// </summary>
        public string GenerateEncryptedToken(Guid tokenId, Guid organizationId, DateTime expiredDate)
        {
            try
            {
                var tokenData = new EncryptedTokenDto
                {
                    Id = tokenId.ToString(),
                    Expired = expiredDate,
                    OrganizationId = organizationId.ToString()
                };

                var json = JsonSerializer.Serialize(tokenData);
                var encryptedToken = UnifiedEncryption.EncryptAesCbcWithKey(json, _secretKey);

                _logger.LogDebug("Generated encrypted token for user token {TokenId}", tokenId);
                return encryptedToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating encrypted token for {TokenId}", tokenId);
                throw new InvalidOperationException("Error generating encrypted token", ex);
            }
        }

        /// <summary>
        /// Desencripta y valida un token
        /// </summary>
        public EncryptedTokenDto? DecryptAndValidateToken(string encryptedToken)
        {
            try
            {
                var json = UnifiedEncryption.DecryptAesCbcWithKey(encryptedToken, _secretKey);
                var tokenData = JsonSerializer.Deserialize<EncryptedTokenDto>(json);

                if (tokenData == null)
                {
                    _logger.LogWarning("Token deserialization failed");
                    return null;
                }

                // Validar que no esté expirado
                if (tokenData.Expired <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Token {TokenId} is expired. Expired: {ExpiredDate}, Now: {Now}", 
                        tokenData.Id, tokenData.Expired, DateTime.UtcNow);
                    return null;
                }

                // Validar que tenga datos válidos
                if (string.IsNullOrEmpty(tokenData.Id) || 
                    string.IsNullOrEmpty(tokenData.OrganizationId) ||
                    !Guid.TryParse(tokenData.Id, out _) ||
                    !Guid.TryParse(tokenData.OrganizationId, out _))
                {
                    _logger.LogWarning("Token contains invalid data");
                    return null;
                }

                _logger.LogDebug("Token {TokenId} validated successfully", tokenData.Id);
                return tokenData;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error decrypting/validating token");
                return null;
            }
        }
    }
}