using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models
{
    public class PlayerProfileDetails
    {
        [Key]
        public int PlayerId { get; set; }

        [DataType(DataType.Date)]
        public DateTime JoinedDate { get; set; } = DateTime.UtcNow.Date;

        [StringLength(120)]
        [Display(Name = "Emergency Contact Name")]
        public string EmergencyContactName { get; set; } = string.Empty;

        [Phone]
        [StringLength(30)]
        [Display(Name = "Emergency Contact Number")]
        public string EmergencyContactPhone { get; set; } = string.Empty;

        public Player? Player { get; set; }
    }
}
