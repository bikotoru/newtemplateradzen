using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;
using Backend.Utils.Services;
using Shared.Models.Entities;

namespace Backend.Modules.Admin.SystemPermissions
{
    public class SystemPermissionService : BaseQueryService<Shared.Models.Entities.SystemEntities.SystemPermissions>
    {
        public SystemPermissionService(AppDbContext context, ILogger<SystemPermissionService> logger) 
            : base(context, logger)
        {
        }

    }
}