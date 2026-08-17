using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Dtos
{
    public class QrCheckInRequest
    {
        [Required]
        [StringLength(64, MinimumLength = 64)]
        public string Token { get; set; } = string.Empty;
    }
}