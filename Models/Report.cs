using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models
{
    public enum ReportType
    {
        Performance,
        Attendance,
        Tournament,
        Training,
        Financial,
        Analysis
    }

    public enum ReportStatus
    {
        Draft,
        Published
    }

    public class Report
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        public ReportType Type { get; set; } = ReportType.Performance;

        public ReportStatus Status { get; set; } = ReportStatus.Draft;

        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        /// <summary>Relative web path (under wwwroot) to the uploaded file, if any.</summary>
        public string? FilePath { get; set; }

        /// <summary>Original filename of the uploaded document, if any.</summary>
        public string? FileName { get; set; }

        public long SizeBytes { get; set; }

        public string SizeLabel =>
            SizeBytes >= 1024 * 1024
                ? $"{SizeBytes / 1024.0 / 1024.0:0.0} MB"
                : $"{SizeBytes / 1024.0:0.0} KB";
    }
}
