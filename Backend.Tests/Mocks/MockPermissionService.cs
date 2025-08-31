using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Backend.Utils.Security;
using Backend.Utils.Data;
using Shared.Models.DTOs.Auth;

namespace Backend.Tests.Mocks
{
    /// <summary>
    /// Mock del PermissionService que siempre permite acceso para las pruebas
    /// </summary>
    public class MockPermissionService : PermissionService
    {
        public MockPermissionService(AppDbContext context, ILogger<PermissionService> logger, TokenCacheService tokenCache, TokenEncryptionService tokenEncryption) 
            : base(context, logger, tokenCache, tokenEncryption)
        {
        }

        /// <summary>
        /// Override del método ValidateUserFromHeadersAsync para tests
        /// </summary>
        public new async Task<SessionDataDto?> ValidateUserFromHeadersAsync(IHeaderDictionary headers)
        {
            // Retornar un usuario mock para las pruebas
            await Task.Delay(1); // Para evitar warnings de async
            return new SessionDataDto
            {
                Id = "11111111-1111-1111-1111-111111111111", // Usuario mock
                Nombre = "Usuario Test",
                Permisos = new List<string>
                {
                    // Permisos completos para todas las entidades de prueba
                    "CATEGORIA.CREATE", "CATEGORIA.VIEW", "CATEGORIA.UPDATE", "CATEGORIA.DELETE",
                    "SYSTEMUSER.CREATE", "SYSTEMUSER.VIEW", "SYSTEMUSER.UPDATE", "SYSTEMUSER.DELETE",
                    "ADMIN.ALL" // Permiso de administrador general
                },
                Organization = new OrganizationDto
                {
                    Id = "22222222-2222-2222-2222-222222222222", // Organización mock
                    Nombre = "Organización Test"
                },
                Roles = new List<RoleDto>()
            };
        }
    }
}