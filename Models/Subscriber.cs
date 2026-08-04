using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models
{
    public class Subscriber
    {
        public int Id { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public DateTime SubscribedAt { get; set; } = DateTime.Now;
    }
}
