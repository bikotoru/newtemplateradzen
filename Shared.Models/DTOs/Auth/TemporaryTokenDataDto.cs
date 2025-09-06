namespace Shared.Models.DTOs.Auth
{
    public class TemporaryTokenDataDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}