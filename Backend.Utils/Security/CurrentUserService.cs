using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Models.DTOs.Auth;

namespace Backend.Utils.Security
{
    public interface ICurrentUserService
    {
        Task<SessionDataDto?> GetCurrentUserAsync();
        Task<List<string>> GetCurrentUserPermissionsAsync();
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly PermissionService _permissionService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CurrentUserService> _logger;
        
        // Cache por request (thread-safe dentro del mismo scope)
        private SessionDataDto? _cachedUser;
        private bool _userResolved = false;

        public CurrentUserService(
            PermissionService permissionService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CurrentUserService> logger)
        {
            _permissionService = permissionService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<SessionDataDto?> GetCurrentUserAsync()
        {
            if (_userResolved) return _cachedUser;

            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    _userResolved = true;
                    return null;
                }

                _cachedUser = await _permissionService.ValidateUserFromHeadersAsync(httpContext.Request.Headers);
                _userResolved = true;


                return _cachedUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando usuario desde headers");
                _userResolved = true;
                return null;
            }
        }

        public async Task<List<string>> GetCurrentUserPermissionsAsync()
        {
            var user = await GetCurrentUserAsync();
            return user?.Permisos ?? new List<string>();
        }
    }
}