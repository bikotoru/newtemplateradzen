using Microsoft.AspNetCore.Mvc;
using Shared.Models.DTOs.Auth;

namespace Backend.Modules.Auth.Login
{
    [ApiController]
    [Route("api/auth")]
    public class LoginController : ControllerBase
    {
        private readonly LoginService _loginService;
        private readonly ILogger<LoginController> _logger;

        public LoginController(LoginService loginService, ILogger<LoginController> logger)
        {
            _loginService = loginService;
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
                var authHeader = Request.Headers.Authorization.FirstOrDefault();
                
                if (string.IsNullOrEmpty(authHeader))
                {
                    return Unauthorized(new { message = "Token de autorización requerido" });
                }

                var token = authHeader.StartsWith("Bearer ") ? authHeader.Substring(7) : authHeader;
                
                var response = await _loginService.ValidateAndRefreshTokenAsync(token);
                
                _logger.LogInformation("Token validado y refrescado exitosamente");
                
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Validación de token fallida: {Message}", ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante validación de token");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                _logger.LogInformation("Usuario cerró sesión");
                
                return Ok(new { message = "Sesión cerrada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante logout");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}