namespace SportsManagementMVC.Models
{
    public class AnnouncementReadReceipt
    {
        public int AppUserId { get; set; }
        public int AnnouncementId { get; set; }
        public DateTime ReadAtUtc { get; set; } = DateTime.UtcNow;

        public AppUser AppUser { get; set; } = null!;
        public Announcement Announcement { get; set; } = null!;
    }
}
