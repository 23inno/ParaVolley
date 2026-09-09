using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models
{
    public class PlayerApprovalInput
    {
        [Required]
        public string Position { get; set; } = string.Empty;

        [Required]
        public string Team { get; set; } = string.Empty;
    }
}