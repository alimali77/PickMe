using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using PickMe.Application.Abstractions;

namespace PickMe.Infrastructure.Email;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string FromEmail { get; set; } = "no-reply@pickme.local";
    public string FromName { get; set; } = "Pick Me";
    public bool EnableSsl { get; set; }
}

public sealed class MailKitEmailSender(IOptions<SmtpOptions> opt, ILogger<MailKitEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _opt = opt.Value;
    private readonly ILogger<MailKitEmailSender> _logger = logger;

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_opt.FromName, _opt.FromEmail));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;

        var builder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.PlainBody,
        };
        mime.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        var secure = _opt.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
        await client.ConnectAsync(_opt.Host, _opt.Port, secure, ct);

        if (!string.IsNullOrWhiteSpace(_opt.User))
        {
            await client.AuthenticateAsync(_opt.User, _opt.Password ?? string.Empty, ct);
        }

        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(true, ct);

        _logger.LogInformation("Mail {Template} gönderildi: {To}", message.TemplateKey, message.To);
    }
}

/// <summary>
/// Basit in-memory kuyruk fallback'i. Faz 2'de çalışır; Faz 4'te Hangfire ile değiştirilecek.
/// </summary>
public sealed class BackgroundEmailQueue(IEmailSender sender, ILogger<BackgroundEmailQueue> logger) : IEmailQueue
{
    private readonly IEmailSender _sender = sender;
    private readonly ILogger<BackgroundEmailQueue> _logger = logger;

    public Task EnqueueAsync(EmailMessage message, CancellationToken ct = default)
    {
        _ = Task.Run(async () =>
        {
            try { await _sender.SendAsync(message, CancellationToken.None); }
            catch (Exception ex) { _logger.LogError(ex, "Mail gönderilemedi: {To}", message.To); }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
