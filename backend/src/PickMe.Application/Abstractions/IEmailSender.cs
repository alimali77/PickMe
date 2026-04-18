namespace PickMe.Application.Abstractions;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string PlainBody,
    string TemplateKey);

public interface IEmailQueue
{
    /// <summary>Hangfire'a (veya in-memory fallback'ine) mail işini sıraya koyar.</summary>
    Task EnqueueAsync(EmailMessage message, CancellationToken ct = default);
}
