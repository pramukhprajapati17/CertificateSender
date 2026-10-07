using MailKit.Net.Smtp;
using MailKit.Security;
using MailSender.Interfaces;
using MailSender.Models;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CertificateMailer.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(
        IOptions<EmailSettings> options)
    {
        _settings =
            options.Value;
    }

    public async Task SendCertificateAsync(
        string recipientEmail,
        string studentName,
        string certificatePath)
    {
        if (string.IsNullOrWhiteSpace(
                _settings.Username))
        {
            throw new InvalidOperationException(
                "Sender email is not configured.");
        }

        if (string.IsNullOrWhiteSpace(
                _settings.Password))
        {
            throw new InvalidOperationException(
                "Email password/App Password is not configured.");
        }

        var message =
            new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _settings.FromName,
                _settings.Username));

        message.To.Add(
            MailboxAddress.Parse(
                recipientEmail));

        message.Subject =
            _settings.Subject;

        var body =
            new BodyBuilder();

        body.TextBody =
            $"""
            Dear {studentName},

            Greetings from Sardar Patel College of Engineering & Technology! 🙏

            Thank you for your enthusiastic participation in the 3D Ganesh Art Competition, organized by the Cultural Cell of Sardar Patel College of Engineering & Technology.

            Your creativity, dedication, and enthusiasm made the competition truly special. We sincerely appreciate the time and effort you put into showcasing your artistic talent. 🎨✨

            We are pleased to share your Certificate of Participation as a token of appreciation for your valuable participation.

            📜 Please find your certificate attached with this email.

            Once again, congratulations and thank you for being a part of the celebration. We hope to see your creativity and enthusiasm in many more events and activities in the future.

            With best wishes for your continued success and creativity! 🌟

            Warm Regards,
            Cultural Cell
            Sardar Patel College of Engineering & Technology
            Bakrol, Anand
            {_settings.FromName}
            """;

        body.Attachments.Add(
            certificatePath);

        message.Body =
            body.ToMessageBody();

        using var smtp =
            new SmtpClient();

        await smtp.ConnectAsync(
            _settings.Host,
            _settings.Port,
            SecureSocketOptions.StartTls);

        await smtp.AuthenticateAsync(
            _settings.Username,
            _settings.Password);

        await smtp.SendAsync(message);

        await smtp.DisconnectAsync(
            true);
    }
}
