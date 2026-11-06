namespace Ice_Cream_Parlour_Eproject.Services.Email
{
    public class SmtpOptions
    {
        public const string SectionName = "Smtp";

        public string? Host { get; set; }
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string FromAddress { get; set; } = "no-reply@icream.com";
        public string FromName { get; set; } = "iCREAM";

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
    }
}
