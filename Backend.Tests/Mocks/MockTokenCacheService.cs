using Microsoft.Extensions.Logging;
using Backend.Utils.Security;
using Backend.Utils.Data;
using Shared.Models.DTOs.Auth;

namespace Backend.Tests.Mocks
{
    /// <summary>
    /// Mock del TokenCacheService para las pruebas
    /// </summary>
    public class MockTokenCacheService : TokenCacheService
    {
        public MockTokenCacheService(AppDbContext context, ILogger<TokenCacheService> logger) 
            : base(context, logger)
        {
        }

        public new async Task<SessionDataDto?> GetTokenDataAsync(Guid tokenId)
        {
            // Retornar siempre sesión válida para pruebas
            await Task.Delay(1); // Para evitar warnings de async
            return new SessionDataDto
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Nombre = "Usuario Test",
                Permisos = new List<string>
                {
                    "CATEGORIA.CREATE", "CATEGORIA.VIEW", "CATEGORIA.UPDATE", "CATEGORIA.DELETE",
                    "SYSTEMUSER.CREATE", "SYSTEMUSER.VIEW", "SYSTEMUSER.UPDATE", "SYSTEMUSER.DELETE",
                    "ADMIN.ALL"
                },
                Organization = new OrganizationDto
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Nombre = "Organización Test"
                },
                Roles = new List<RoleDto>()
            };
        }

        public new async Task<Guid> CreateOrUpdateTokenAsync(Guid userId, Guid organizationId, SessionDataDto sessionData)
        {
            // Retornar token mock para pruebas
            await Task.Delay(1);
            return Guid.Parse("33333333-3333-3333-3333-333333333333");
        }

        public new async Task<bool> RefreshTokenDataAsync(Guid tokenId, SessionDataDto newSessionData)
        {
            // Siempre éxito en pruebas
            await Task.Delay(1);
            return true;
        }
    }
}