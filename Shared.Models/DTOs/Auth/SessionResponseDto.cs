namespace Shared.Models.DTOs.Auth
{
    public class SessionResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expired { get; set; }
        public string Data { get; set; } = string.Empty;
    }
}