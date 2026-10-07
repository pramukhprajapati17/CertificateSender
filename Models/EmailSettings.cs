using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailSender.Models
{
    public class EmailSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";

        public int Port { get; set; } = 587;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FromName { get; set; } = "Certificate Team";

        public string Subject { get; set; } = "Your Certificate";

    }
}