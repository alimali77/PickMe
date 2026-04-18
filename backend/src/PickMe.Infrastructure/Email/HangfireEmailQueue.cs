using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PickMe.Application.Abstractions;
using PickMe.Domain;
using PickMe.Domain.Entities;
using PickMe.Infrastructure.Persistence;

namespace PickMe.Infrastructure.Email;

/// <summary>
/// Hangfire tabanlı dayanıklı mail kuyruğu.
/// - Job DB'de persist edilir (IIS app pool recycle sonrasında bile devam eder).
/// - Fail olursa otomatik retry: 5 dk, 30 dk, 2 saat (brief §13).
/// - Her mail EmailLogs tablosuna kaydedilir; admin panelinden görüntüleme + retry mümkün.
/// </summary>
public sealed class HangfireEmailQueue(IBackgroundJobClient jobs) : IEmailQueue
{
    private readonly IBackgroundJobClient _jobs = jobs;

    public Task EnqueueAsync(EmailMessage message, CancellationToken ct = default)
    {
        _jobs.Enqueue<EmailJobRunner>(r => r.SendAsync(message, default));
        return Task.CompletedTask;
    }
}

public sealed class EmailJobRunner(
    IEmailSender sender,
    ApplicationDbContext db,
    ILogger<EmailJobRunner> logger)
{
    private readonly IEmailSender _sender = sender;
    private readonly ApplicationDbContext _db = db;
    private readonly ILogger<EmailJobRunner> _logger = logger;

    // 3 retry: 5 dakika, 30 dakika, 2 saat (brief §13).
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 300, 1800, 7200 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        // İlk attempt için EmailLog kaydı oluştur (yoksa)
        var log = await _db.EmailLogs.FirstOrDefaultAsync(
            l => l.ToEmail == message.To && l.TemplateKey == message.TemplateKey
                 && l.Status != EmailLogStatus.Sent && l.CreatedAtUtc > DateTime.UtcNow.AddMinutes(-30),
            ct);

        if (log is null)
        {
            log = EmailLog.Create(Guid.NewGuid(), message.To, message.Subject, message.TemplateKey);
            _db.EmailLogs.Add(log);
            await _db.SaveChangesAsync(ct);
        }

        try
        {
            await _sender.SendAsync(message, ct);
            log.MarkSent();
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Mail gönderildi: {Template} → {To}", message.TemplateKey, message.To);
        }
        catch (Exception ex)
        {
            // AttemptCount === 3 olduğunda retry biter; Hangfire exception fırlatırsa otomatik retry tetiklenir.
            log.MarkFailed(ex.Message, willRetry: log.AttemptCount < 3);
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning(ex, "Mail gönderilemedi: {Template} → {To}", message.TemplateKey, message.To);
            throw; // Hangfire retry tetikle
        }
    }
}
