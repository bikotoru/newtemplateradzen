using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Security;
using Shared.Models.DTOs.Auth;
using Shared.Models.Responses;

namespace Backend.Modules.Auth.Login
{
    [ApiController]
    [Route("api/auth")]
    public class LoginController : ControllerBase
    {
        private readonly LoginService _loginService;
        private readonly PermissionService _permissionService;
        private readonly TokenCacheService _tokenCache;
        private readonly ILogger<LoginController> _logger;

        public LoginController(LoginService loginService, PermissionService permissionService, TokenCacheService tokenCache, ILogger<LoginController> logger)
        {
            _loginService = loginService;
            _permissionService = permissionService;
            _tokenCache = tokenCache;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _loginService.LoginAsync(request);
                
                _logger.LogInformation("Usuario {Email} intentó iniciar sesión", request.Email);
                
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Intento de login fallido para {Email}: {Message}", request.Email, ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el login para {Email}", request.Email);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("select-organization")]
        public async Task<IActionResult> SelectOrganization([FromBody] OrganizationSelectionDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _loginService.SelectOrganizationAsync(request);
                
                _logger.LogInformation("Usuario seleccionó organización {OrganizationId}", request.OrganizationId);
                
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Selección de organización fallida: {Message}", ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante selección de organización");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("me")]
        public async Task<IActionResult> ValidateSession()
        {
            try
            {
                var sessionData = await _permissionService.ValidateUserFromHeadersAsync(Request.Headers);
                
                if (sessionData == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token de autorización inválido"));
                }

                // Crear respuesta estándar como SessionResponseDto pero solo con datos actuales
                var response = new SessionResponseDto
                {
                    Token = ExtractTokenFromHeaders(), // Devolver el mismo token
                    Expired = DateTime.UtcNow.AddHours(24), // Renovar expiración
                    Data = Shared.Models.Security.UnifiedEncryption.EncryptAesCbc(System.Text.Json.JsonSerializer.Serialize(sessionData))
                };
                
                _logger.LogInformation("Token validado exitosamente para usuario {UserId}", sessionData.Id);
                
                return Ok(response);
            }
            catch (SessionExpiredException ex)
            {
                _logger.LogWarning("Sesión expirada: {ErrorCode}", ex.ErrorCode);
                return Unauthorized(ApiResponse<object>.ErrorResponse("Sesión expirada"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante validación de token");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Error interno del servidor"));
            }
        }

        private string ExtractTokenFromHeaders()
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader)) return string.Empty;
            
            return authHeader.StartsWith("Bearer ") ? authHeader.Substring(7) : authHeader;
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var token = ExtractTokenFromHeaders();
                
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Token requerido para logout"));
                }

                if (!Guid.TryParse(token, out var tokenId))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Token inválido"));
                }

                // Obtener datos de sesión para logging
                var sessionData = await _tokenCache.GetTokenDataAsync(tokenId);
                var userId = sessionData?.Id ?? "desconocido";

                // Marcar token para logout (esto invalidará inmediatamente el token)
                await _tokenCache.MarkUserForLogoutAsync(Guid.Parse(userId));
                
                _logger.LogInformation("Usuario {UserId} cerró sesión exitosamente", userId);
                
                return Ok(ApiResponse<object>.SuccessResponse(new { }, "Sesión cerrada exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante logout");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Error interno del servidor"));
            }
        }
    }
}