using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shared.Models.DTOs.Auth;
using Shared.Models.Security;
using Shared.Models.Entities;
using Backend.Utils.Data;
using Backend.Utils.Security;

namespace Backend.Modules.Auth.Login
{
    public class LoginService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly TokenCacheService _tokenCache;
        private readonly PermissionService _permissionService;
        private readonly TokenEncryptionService _tokenEncryption;
        private readonly string _secretKey;
        private readonly string _secondKey;

        public LoginService(AppDbContext context, IConfiguration configuration, TokenCacheService tokenCache, PermissionService permissionService, TokenEncryptionService tokenEncryption)
        {
            _context = context;
            _configuration = configuration;
            _tokenCache = tokenCache;
            _permissionService = permissionService;
            _tokenEncryption = tokenEncryption;
            _secretKey = Environment.GetEnvironmentVariable("SECRETKEY") ?? throw new InvalidOperationException("SECRETKEY no está configurada");
            _secondKey = Environment.GetEnvironmentVariable("SECONDKEY") ?? throw new InvalidOperationException("SECONDKEY no está configurada");
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var encryptedPassword = await EncryptWithSecretKeyAsync(request.Password);
            
            var user = await _context.SystemUsers
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Password == encryptedPassword && u.Active);
            
            if (user == null)
            {
                throw new UnauthorizedAccessException("Credenciales inválidas");
            }

            var organizations = await GetUserOrganizationsAsync(user.Id);

            if (organizations.Count > 1)
            {
                return await CreateTemporaryTokenResponseAsync(user, organizations);
            }
            else if (organizations.Count == 1)
            {
                return await CreateFullSessionResponseAsync(user, organizations.First());
            }
            else
            {
                throw new UnauthorizedAccessException("Usuario sin organizaciones asignadas");
            }
        }

        public async Task<SessionResponseDto> SelectOrganizationAsync(OrganizationSelectionDto request)
        {
            var userData = await ValidateTemporaryTokenAsync(request.TemporaryToken);
            
            var organizations = await GetUserOrganizationsAsync(Guid.Parse(userData.UserId));
            var selectedOrganization = organizations.FirstOrDefault(o => o.Id.ToString() == request.OrganizationId);
            
            if (selectedOrganization == null)
            {
                throw new UnauthorizedAccessException("Organización no válida para este usuario");
            }

            var user = await _context.SystemUsers.FindAsync(Guid.Parse(userData.UserId));
            if (user == null)
            {
                throw new UnauthorizedAccessException("Usuario no encontrado");
            }

            var response = await CreateFullSessionResponseAsync(user, selectedOrganization);
            return new SessionResponseDto
            {
                Token = response.Token!,
                Expired = response.Expired!.Value,
                Data = response.Data!
            };
        }

        public async Task<SessionResponseDto> ValidateAndRefreshTokenAsync(string token)
        {
            // Desencriptar y validar el token
            var tokenData = _tokenEncryption.DecryptAndValidateToken(token);
            if (tokenData == null)
            {
                throw new UnauthorizedAccessException("Token inválido o expirado");
            }

            var tokenId = Guid.Parse(tokenData.Id);
            var sessionData = await _tokenCache.GetTokenDataAsync(tokenId);
            if (sessionData == null)
            {
                throw new UnauthorizedAccessException("Sesión no válida o expirada");
            }

            // Doble verificación: la organización del token debe coincidir con la de la sesión
            if (tokenData.OrganizationId != sessionData.Organization.Id.ToString())
            {
                throw new UnauthorizedAccessException("Token no válido para esta organización");
            }

            // Verificar si el usuario y organización siguen activos
            var user = await _context.SystemUsers.FindAsync(sessionData.Id);
            if (user == null || !user.Active)
            {
                throw new UnauthorizedAccessException("Usuario no válido");
            }

            var organization = await _context.SystemOrganization.FindAsync(sessionData.Organization.Id);
            if (organization == null || !organization.Active)
            {
                throw new UnauthorizedAccessException("Organización no válida");
            }

            // Generar nuevo token encriptado para renovación
            var newExpirationDate = DateTime.UtcNow.AddHours(24);
            var newEncryptedToken = _tokenEncryption.GenerateEncryptedToken(tokenId, sessionData.Organization.Id, newExpirationDate);

            // Encriptar datos actuales para respuesta
            var encryptedData = UnifiedEncryption.EncryptAesCbc(JsonSerializer.Serialize(sessionData));

            return new SessionResponseDto
            {
                Token = newEncryptedToken, // Token renovado con nueva fecha de expiración
                Expired = newExpirationDate,
                Data = encryptedData
            };
        }

        private async Task<List<OrganizationDto>> GetUserOrganizationsAsync(Guid userId)
        {
            var user = await _context.SystemUsers
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Id == userId && u.Active);

            var userOrganizations = new List<OrganizationDto>();
            
            if (user?.Organization != null && user.Organization.Active)
            {
                userOrganizations.Add(new OrganizationDto
                {
                    Id = user.Organization.Id,
                    Nombre = user.Organization.Nombre
                });
            }

            return userOrganizations;
        }

        private async Task<LoginResponseDto> CreateTemporaryTokenResponseAsync(SystemUsers user, List<OrganizationDto> organizations)
        {
            var temporaryData = new TemporaryTokenDataDto
            {
                UserId = user.Id.ToString(),
                Email = user.Email,
                EncryptedPassword = user.Password,
                Timestamp = DateTime.UtcNow
            };

            var temporaryToken = await EncryptWithSecondKeyAsync(JsonSerializer.Serialize(temporaryData));

            return new LoginResponseDto
            {
                RequiresOrganizationSelection = true,
                TemporaryToken = temporaryToken,
                Organizations = organizations.Any() ? organizations : null
            };
        }

        private async Task<LoginResponseDto> CreateFullSessionResponseAsync(SystemUsers user, OrganizationDto organization)
        {
            // Usar PermissionService para construir SessionData completo
            var sessionData = await _permissionService.BuildSessionDataAsync(user.Id, organization.Id);
            
            // Crear token en cache z_token
            var tokenId = await _tokenCache.CreateOrUpdateTokenAsync(user.Id, organization.Id, sessionData);
            
            // Generar token encriptado con doble verificación
            var expirationDate = DateTime.UtcNow.AddHours(24);
            var encryptedToken = _tokenEncryption.GenerateEncryptedToken(tokenId, organization.Id, expirationDate);
            
            // Encriptar datos de sesión con UnifiedEncryption
            var encryptedData = UnifiedEncryption.EncryptAesCbc(JsonSerializer.Serialize(sessionData));

            return new LoginResponseDto
            {
                RequiresOrganizationSelection = null, // No mostrar cuando es false
                Token = encryptedToken, // Ahora es el token JSON encriptado con doble verificación
                Expired = expirationDate,
                Data = encryptedData
            };
        }


        private async Task<TemporaryTokenDataDto> ValidateTemporaryTokenAsync(string temporaryToken)
        {
            var decryptedData = await DecryptWithSecondKeyAsync(temporaryToken);
            var tokenData = JsonSerializer.Deserialize<TemporaryTokenDataDto>(decryptedData);
            
            if (tokenData == null)
            {
                throw new UnauthorizedAccessException("Token temporal inválido");
            }

            return tokenData;
        }


        private async Task<string> EncryptWithSecretKeyAsync(string data)
        {
            return await Task.FromResult(Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(data + _secretKey)
            ));
        }

        private async Task<string> DecryptWithSecretKeyAsync(string encryptedData)
        {
            try
            {
                var decoded = Convert.FromBase64String(encryptedData);
                var decryptedWithKey = System.Text.Encoding.UTF8.GetString(decoded);
                return await Task.FromResult(decryptedWithKey.Replace(_secretKey, ""));
            }
            catch
            {
                throw new UnauthorizedAccessException("Error al desencriptar token");
            }
        }

        private async Task<string> EncryptWithSecondKeyAsync(string data)
        {
            return await Task.FromResult(Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(data + _secondKey)
            ));
        }

        private async Task<string> DecryptWithSecondKeyAsync(string encryptedData)
        {
            try
            {
                var decoded = Convert.FromBase64String(encryptedData);
                var decryptedWithKey = System.Text.Encoding.UTF8.GetString(decoded);
                return await Task.FromResult(decryptedWithKey.Replace(_secondKey, ""));
            }
            catch
            {
                throw new UnauthorizedAccessException("Error al desencriptar token temporal");
            }
        }
    }
}