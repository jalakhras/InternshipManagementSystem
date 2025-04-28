namespace InternshipManagementSystem.Settings.DTOs
{
    public class UpdateGeneralSettingsDto
    {
        public string SiteName { get; set; }
        public string DefaultLanguage { get; set; }
        public int MaxExamAttempts { get; set; }
        public string LogoUrl { get; set; }
    }
}