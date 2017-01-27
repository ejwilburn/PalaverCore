namespace Palaver.Services
{
    public class SmtpOptions
    {
        public static readonly string CONFIG_SECTION_NAME = "Smtp";

        public string Server { get; set; } = "localhost";
        public int Port { get; set; } = 25;
        public bool RequireTls { get; set; } = false;
        public string Username { get; set; } = null;
        public string Password { get; set; } = null;
        public string FromName { get; set; }
        public string FromAddress { get; set; }

        public SmtpOptions()
        {
        }
    }
}
