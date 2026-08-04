using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models
{
    public enum AnnouncementCategory
    {
        Event,
        Announcement,
        News,
        Update
    }

    public class Announcement
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string Excerpt { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.MultilineText)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public string Author { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        public AnnouncementCategory Category { get; set; } = AnnouncementCategory.News;

        [Display(Name = "Pinned")]
        public bool IsPinned { get; set; }

        [Range(0, int.MaxValue)]
        public int Views { get; set; }
    }
}
