using MailSender.Models;

namespace MailSender.Interfaces;

public interface ICertificateService
{
    Task<string> GenerateCertificateAsync(
        string templatePath,
        string studentName,
        CertificateSettings settings);

    Task<string> GeneratePreviewAsync(
        string templatePath,
        string studentName,
        CertificateSettings settings);
}
