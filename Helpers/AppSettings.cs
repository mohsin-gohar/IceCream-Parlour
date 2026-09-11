namespace Ice_Cream_Parlour_Eproject.Helpers
{
    /// <summary>
    /// Application configuration settings loaded from appsettings.json.
    /// </summary>
    public class AppSettings
    {
        /// <remarks>This section is configured in appsettings.json under 'AppSettings'</remarks>
        public const string SectionName = "AppSettings";

        /// <summary>
        /// Gets or sets the maximum allowed file upload size in bytes.
        /// </summary>
        public long MaxFileUploadSize { get; set; } = 5242880; // 5 MB default

        /// <summary>
        /// Gets or sets the allowed file extensions for uploads.
        /// </summary>
        public string[] AllowedFileExtensions { get; set; } = new[] { ".jpg", ".jpeg", ".png", ".gif" };

        /// <summary>
        /// Gets or sets the pagination page size.
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the application name.
        /// </summary>
        public string ApplicationName { get; set; } = "Ice Cream Parlour";

        /// <summary>
        /// Gets or sets the session timeout in minutes.
        /// </summary>
        public int SessionTimeoutMinutes { get; set; } = 30;

        /// <summary>
        /// Gets or sets the SMTP server configuration for email sending.
        /// </summary>
        public SmtpSettings? Smtp { get; set; }
    }

    /// <summary>
    /// SMTP configuration for email notifications.
    /// </summary>
    public class SmtpSettings
    {
        /// <summary>
        /// Gets or sets the SMTP server host.
        /// </summary>
        public string? Host { get; set; }

        /// <summary>
        /// Gets or sets the SMTP server port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the sender email address.
        /// </summary>
        public string? SenderEmail { get; set; }

        /// <summary>
        /// Gets or sets the sender name.
        /// </summary>
        public string? SenderName { get; set; }

        /// <summary>
        /// Gets or sets the SMTP username.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the SMTP password.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use SSL/TLS.
        /// </summary>
        public bool UseSsl { get; set; } = true;
    }
}
