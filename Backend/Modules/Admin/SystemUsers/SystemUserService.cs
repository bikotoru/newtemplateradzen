using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;
using Backend.Utils.Services;
using Shared.Models.Entities;

namespace Backend.Modules.Admin.SystemUsers
{
    public class SystemUserService : BaseQueryService<Shared.Models.Entities.SystemEntities.SystemUsers>
    {
        public SystemUserService(AppDbContext context, ILogger<SystemUserService> logger) 
            : base(context, logger)
        {
        }

    }
}