using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailSender.Models
{
    public class SendResult
    {
        public int Total { get; set; }

        public int Success { get; set; }

        public int Failed { get; set; }

        public List<Student> Students { get; set; } = new List<Student>();

    }
}