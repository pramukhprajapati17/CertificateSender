using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailSender.Interfaces
{
    public interface IEmailService
    {
        Task SendCertificateAsync(
        string recipientEmail,
        string studentName,
        string certificatePath);
    }
}