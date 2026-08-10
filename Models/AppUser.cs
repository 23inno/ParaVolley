using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models
{
    public enum AppUserRole
    {
        Admin,
        Coach,
        Player
    }

    public class AppUser
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public AppUserRole Role { get; set; } = AppUserRole.Player;

        public bool IsActive { get; set; } = true;

        public int? PlayerId { get; set; }

        public Player? Player { get; set; }
    }
}