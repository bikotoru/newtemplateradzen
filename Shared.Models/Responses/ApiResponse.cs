namespace Shared.Models.Responses
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string? message = null)
            => new() { Success = true, Data = data, Message = message };

        public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
            => new() { Success = false, Message = message, Errors = errors ?? new List<string>() };

        public static ApiResponse<T> ErrorResponse(List<string> errors)
            => new() { Success = false, Errors = errors, Message = "Multiple errors occurred" };
    }
}