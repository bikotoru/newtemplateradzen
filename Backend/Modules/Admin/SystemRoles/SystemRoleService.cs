using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;
using Backend.Utils.Services;
using Shared.Models.Entities;

namespace Backend.Modules.Admin.SystemRoles
{
    public class SystemRoleService : BaseQueryService<Shared.Models.Entities.SystemEntities.SystemRoles>
    {
        public SystemRoleService(AppDbContext context, ILogger<SystemRoleService> logger) 
            : base(context, logger)
        {
        }

    }
}