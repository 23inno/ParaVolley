using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models
{
    public enum SponsorTier
    {
        Gold,
        Silver,
        Bronze
    }

    public class Sponsor
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public SponsorTier Tier { get; set; } = SponsorTier.Bronze;
    }
}
