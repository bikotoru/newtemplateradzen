namespace Shared.Models.QueryModels
{
    public class QueryRequest
    {
        public string? Filter { get; set; }
        public string? OrderBy { get; set; }
        public string[]? Include { get; set; }
        public string? Select { get; set; }
        public int? Skip { get; set; }
        public int? Take { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}