using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models.Api
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(128)]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public AppUserResponse User { get; set; } = new();
    }

    public class AppUserResponse
    {
        public int Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public int? PlayerId { get; set; }

        public string? PlayerName { get; set; }
    }
}
