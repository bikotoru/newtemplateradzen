using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Backend.Controllers;

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

    }
}