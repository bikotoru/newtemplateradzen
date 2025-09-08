using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Backend.Controllers;

namespace Backend.Modules.Admin.SystemPermissions
{
    [Route("api/admin/systempermission")]
    public class SystemPermissionController : BaseQueryController<Shared.Models.Entities.SystemEntities.SystemPermissions>
    {
        private readonly SystemPermissionService _systempermissionService;

        public SystemPermissionController(SystemPermissionService systempermissionService, ILogger<SystemPermissionController> logger, IServiceProvider serviceProvider)
            : base(systempermissionService, logger, serviceProvider)
        {
            _systempermissionService = systempermissionService;
        }

    }
}