
namespace Taskmanagement_API.Models
{
    public class SmtpSettings
    {
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; }
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }


    public class SystemSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        // Public URL of the Angular app; used to build links in notification mails.
        public string ApplicationUrl { get; set; } = string.Empty;
    }
}

