using System.Linq.Expressions;

namespace Shared.Models.Requests
{
    public abstract class BaseRequest<T> where T : class
    {
        public T Entity { get; set; } = default!;
        public Expression<Func<T, object>>[]? IncludeRelations { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}