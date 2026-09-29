namespace SportsManagementMVC.Models
{
    public sealed class WhatsAppSetupViewModel
    {
        public string PersonName { get; set; } = string.Empty;
        public string AccountLabel { get; set; } = string.Empty;
        public string LoginIdentifier { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string DesktopUrl { get; set; } = string.Empty;
        public string WebUrl { get; set; } = string.Empty;
        public string SetupUrl { get; set; } = string.Empty;
        public string BackController { get; set; } = string.Empty;
        public string BackAction { get; set; } = "Details";
        public int BackId { get; set; }
    }
}
