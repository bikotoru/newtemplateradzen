using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Backend.Controllers;

namespace Backend.Modules.Admin.SystemRoles
{
    [Route("api/admin/systemrole")]
    public class SystemRoleController : BaseQueryController<Shared.Models.Entities.SystemEntities.SystemRoles>
    {
        private readonly SystemRoleService _systemroleService;

        public SystemRoleController(SystemRoleService systemroleService, ILogger<SystemRoleController> logger, IServiceProvider serviceProvider)
            : base(systemroleService, logger, serviceProvider)
        {
            _systemroleService = systemroleService;
        }

    }
}