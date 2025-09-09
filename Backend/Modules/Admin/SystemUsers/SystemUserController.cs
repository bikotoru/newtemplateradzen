using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Backend.Controllers;
using Shared.Models.QueryModels;
using Shared.Models.Responses;

namespace Backend.Modules.Admin.SystemUsers
{
    [Route("api/admin/systemuser")]
    public class SystemUserController : BaseQueryController<Shared.Models.Entities.SystemEntities.SystemUsers>
    {
        private readonly SystemUserService _systemuserService;

        public SystemUserController(SystemUserService systemuserService, ILogger<SystemUserController> logger, IServiceProvider serviceProvider)
            : base(systemuserService, logger, serviceProvider)
        {
            _systemuserService = systemuserService;
        }

        /// <summary>
        /// Obtener usuarios filtrados: Global (OrganizationId = null) + Mi Organización
        /// </summary>
        [HttpPost("view-filtered")]
        public async Task<IActionResult> GetFilteredUsers([FromBody] QueryRequest queryRequest)
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                var result = await _systemuserService.GetFilteredUsersPagedAsync(queryRequest, user);
                return Ok(ApiResponse<Shared.Models.QueryModels.PagedResult<Shared.Models.Entities.SystemEntities.SystemUsers>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios filtrados");
                return StatusCode(500, ApiResponse<PagedResponse<Shared.Models.Entities.SystemEntities.SystemUsers>>.ErrorResponse("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Validar que Username sea único en Global + Mi Organización
        /// </summary>
        [HttpPost("validate-username")]
        public async Task<IActionResult> ValidateUsername([FromBody] ValidateUsernameRequest request)
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("*");
            if (errorResult != null) return errorResult;

            try
            {
                var isValid = await _systemuserService.ValidateUsernameAsync(request.Username, user.OrganizationId, request.ExcludeId);
                return Ok(ApiResponse<bool>.SuccessResponse(isValid));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar username");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Validar que Email sea único en Global + Mi Organización
        /// </summary>
        [HttpPost("validate-email")]
        public async Task<IActionResult> ValidateEmail([FromBody] ValidateEmailRequest request)
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("*");
            if (errorResult != null) return errorResult;

            try
            {
                var isValid = await _systemuserService.ValidateEmailAsync(request.Email, user.OrganizationId, request.ExcludeId);
                return Ok(ApiResponse<bool>.SuccessResponse(isValid));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar email");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Error interno del servidor"));
            }
        }
    }

    // DTOs para validación
    public class ValidateUsernameRequest
    {
        public string Username { get; set; } = string.Empty;
        public Guid? ExcludeId { get; set; }
    }

    public class ValidateEmailRequest
    {
        public string Email { get; set; } = string.Empty;
        public Guid? ExcludeId { get; set; }
    }
}