using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Dtos
{
    public class RecordAttendanceRequest
    {
        [Range(1, int.MaxValue)]
        public int PlayerId { get; set; }

        [Range(1, int.MaxValue)]
        public int EventId { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
