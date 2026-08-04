using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models
{
    public enum MatchStatus
    {
        Scheduled,
        [Display(Name = "In Progress")]
        InProgress,
        Completed,
        Cancelled
    }

    public class Match
    {
        public int Id { get; set; }

        [Required, Display(Name = "Team A")]
        public string TeamA { get; set; } = string.Empty;

        [Required, Display(Name = "Team B")]
        public string TeamB { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        public string Time { get; set; } = string.Empty;

        [Required]
        public string Venue { get; set; } = string.Empty;

        public string Tournament { get; set; } = string.Empty;

        public MatchStatus Status { get; set; } = MatchStatus.Scheduled;

        [Display(Name = "Score A")]
        [Range(0, int.MaxValue)]
        public int? ScoreA { get; set; }

        [Display(Name = "Score B")]
        [Range(0, int.MaxValue)]
        public int? ScoreB { get; set; }
    }
}
