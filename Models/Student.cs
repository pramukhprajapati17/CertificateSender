using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailSender.Models
{
    public class Student
    {
        public int RowNumber { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        public DateTime? SentAt { get; set; }

        public string Error { get; set; } = string.Empty;
    }

}