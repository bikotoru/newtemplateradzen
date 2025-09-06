using Shared.Models.Requests;

namespace Shared.Models.Builders
{
    public static class EntityRequestBuilder<T> where T : class
    {
        public static CreateRequestBuilder<T> Create(T entity) => new(entity);
        public static UpdateRequestBuilder<T> Update(T entity) => new(entity);
        public static BatchRequestBuilder<T> CreateBatch(List<T> entities) => new(entities);
        public static BatchUpdateRequestBuilder<T> UpdateBatch(List<T> entities) => new(entities);
    }
}