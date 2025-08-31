using Microsoft.EntityFrameworkCore;

namespace Backend.Utils.EFInterceptors.Core
{
    public abstract class BaseHandler
    {
        public virtual async Task<bool> CanHandleAsync<T>(T entity) where T : class
        {
            return await Task.FromResult(true);
        }
    }

    public abstract class AddHandler : BaseHandler
    {
        public abstract Task<bool> HandleAddAsync<T>(DbContext context, T entity) where T : class;
    }

    public abstract class UpdateHandler : BaseHandler
    {
        public abstract Task<bool> HandleUpdateAsync<T>(DbContext context, T entity, T originalEntity) where T : class;
    }

    public abstract class SaveHandler : BaseHandler
    {
        public abstract Task<bool> HandleBeforeSaveAsync(DbContext context);
        public abstract Task<bool> HandleAfterSaveAsync(DbContext context, int affectedRows);
    }
}