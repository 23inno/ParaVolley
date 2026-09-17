using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models
{
    public class PlayerProfilePhoto
    {
        [Key]
        public int PlayerId { get; set; }

        [Required]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        [Required, StringLength(50)]
        public string ContentType { get; set; } = string.Empty;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public Player Player { get; set; } = null!;
    }
}
