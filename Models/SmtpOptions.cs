namespace Ice_Cream_Parlour_Eproject.Models
{
    /// <summary>
    /// Configuration model for SMTP email settings.
    /// </summary>
    public class SmtpOptions
    {
        public const string SectionName = "Smtp";

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = "no-reply@icream.com";
        public string FromName { get; set; } = "iCREAM";
    }
}
