using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shared.Models.DTOs.Auth;
using Shared.Models.Security;
using Shared.Models.Entities;
using Backend.Utils.Data;

namespace Backend.Modules.Auth.Login
{
    public class LoginService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly string _secondKey;

        public LoginService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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
            var selectedOrganization = organizations.FirstOrDefault(o => o.Id == request.OrganizationId);
            
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
            var tokenData = await DecryptTokenAsync(token);
            
            if (tokenData.Expired <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Token expirado");
            }

            var user = await _context.SystemUsers.FindAsync(Guid.Parse(tokenData.Id));
            if (user == null || !user.Active)
            {
                throw new UnauthorizedAccessException("Usuario no válido");
            }

            var organization = await _context.SystemOrganization.FindAsync(Guid.Parse(tokenData.OrganizationId));
            if (organization == null || !organization.Active)
            {
                throw new UnauthorizedAccessException("Organización no válida");
            }

            var organizationDto = new OrganizationDto
            {
                Id = organization.Id.ToString(),
                Nombre = organization.Nombre
            };

            var response = await CreateFullSessionResponseAsync(user, organizationDto);
            return new SessionResponseDto
            {
                Token = response.Token!,
                Expired = response.Expired!.Value,
                Data = response.Data!
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
                    Id = user.Organization.Id.ToString(),
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
            var sessionData = await BuildSessionDataAsync(user, organization);
            var tokenData = new TokenDataDto
            {
                Id = user.Id.ToString(),
                OrganizationId = organization.Id,
                Expired = DateTime.UtcNow.AddHours(24)
            };

            var encryptedToken = await EncryptWithSecretKeyAsync(JsonSerializer.Serialize(tokenData));
            var encryptedData = UnifiedEncryption.EncryptAesCbc(JsonSerializer.Serialize(sessionData));

            return new LoginResponseDto
            {
                RequiresOrganizationSelection = null, // No mostrar cuando es false
                Token = encryptedToken,
                Expired = tokenData.Expired,
                Data = encryptedData
            };
        }

        private async Task<SessionDataDto> BuildSessionDataAsync(SystemUsers user, OrganizationDto organization)
        {
            var roles = await _context.SystemUsersRoles
                .Where(ur => ur.SystemUsersId == user.Id && ur.Active)
                .Include(ur => ur.SystemRoles)
                .Where(ur => ur.SystemRoles!.OrganizationId == Guid.Parse(organization.Id) && ur.SystemRoles.Active)
                .Select(ur => new RoleDto
                {
                    Id = ur.SystemRoles!.Id.ToString(),
                    Nombre = ur.SystemRoles.Nombre
                })
                .ToListAsync();

            var roleIds = roles.Select(r => Guid.Parse(r.Id)).ToList();
            
            var rolePermissions = await _context.SystemRolesPermissions
                .Where(rp => roleIds.Contains(rp.SystemRolesId) && rp.Active)
                .Include(rp => rp.SystemPermissions)
                .Where(rp => rp.SystemPermissions!.Active)
                .Select(rp => rp.SystemPermissions!.ActionKey!)
                .ToListAsync();

            var userDirectPermissions = await _context.SystemUsersPermissions
                .Where(up => up.SystemUsersId == user.Id && up.Active)
                .Include(up => up.SystemPermissions)
                .Where(up => up.SystemPermissions!.OrganizationId == Guid.Parse(organization.Id) && up.SystemPermissions.Active)
                .Select(up => up.SystemPermissions!.ActionKey!)
                .ToListAsync();

            var allPermissions = rolePermissions.Concat(userDirectPermissions).Distinct().ToList();

            return new SessionDataDto
            {
                Nombre = user.Nombre,
                Id = user.Id.ToString(),
                Roles = roles,
                Permisos = allPermissions,
                Organization = organization
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

        private async Task<TokenDataDto> DecryptTokenAsync(string encryptedToken)
        {
            var decryptedToken = await DecryptWithSecretKeyAsync(encryptedToken);
            var tokenData = JsonSerializer.Deserialize<TokenDataDto>(decryptedToken);
            
            if (tokenData == null)
            {
                throw new UnauthorizedAccessException("Token inválido");
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