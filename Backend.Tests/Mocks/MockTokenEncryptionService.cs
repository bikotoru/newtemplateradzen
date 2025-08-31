using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Backend.Utils.Security;
using Shared.Models.DTOs.Auth;

namespace Backend.Tests.Mocks
{
    /// <summary>
    /// Mock del TokenEncryptionService para las pruebas
    /// </summary>
    public class MockTokenEncryptionService : TokenEncryptionService
    {
        public MockTokenEncryptionService(IConfiguration configuration, ILogger<TokenEncryptionService> logger) 
            : base(configuration, logger)
        {
        }

        public new string GenerateEncryptedToken(Guid tokenId, Guid organizationId, DateTime expiredDate)
        {
            // Para pruebas, retornar un token simple
            return $"mock-token-{tokenId}-{organizationId}-{expiredDate:yyyyMMddHHmmss}";
        }

        public new EncryptedTokenDto? DecryptAndValidateToken(string encryptedToken)
        {
            // Para pruebas, simular validación exitosa
            if (string.IsNullOrEmpty(encryptedToken) || !encryptedToken.StartsWith("mock-token-"))
            {
                return null;
            }

            // Extraer datos del token mock
            var parts = encryptedToken.Split('-');
            if (parts.Length >= 4)
            {
                return new EncryptedTokenDto
                {
                    Id = "11111111-1111-1111-1111-111111111111",
                    OrganizationId = "22222222-2222-2222-2222-222222222222",
                    Expired = DateTime.UtcNow.AddHours(24) // Siempre válido para pruebas
                };
            }

            return null;
        }
    }
}