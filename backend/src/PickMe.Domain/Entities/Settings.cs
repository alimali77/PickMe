using PickMe.Domain.Common;

namespace PickMe.Domain.Entities;

public sealed class AdminNotificationRecipient : Entity<Guid>
{
    public string Email { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    private AdminNotificationRecipient() { }

    public static AdminNotificationRecipient Create(Guid id, string email)
    {
        return new AdminNotificationRecipient
        {
            Id = id,
            Email = email.Trim().ToLowerInvariant(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class SystemSetting : Entity<Guid>
{
    public string Key { get; private set; } = default!;
    public string Value { get; private set; } = default!;
    public bool IsSensitive { get; private set; }

    private SystemSetting() { }

    public static SystemSetting Create(Guid id, string key, string value, bool isSensitive = false)
    {
        return new SystemSetting
        {
            Id = id,
            Key = key,
            Value = value,
            IsSensitive = isSensitive,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void UpdateValue(string value)
    {
        Value = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class Faq : Entity<Guid>
{
    public string Question { get; private set; } = default!;
    public string Answer { get; private set; } = default!;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Faq() { }

    public static Faq Create(Guid id, string question, string answer, int displayOrder)
    {
        return new Faq
        {
            Id = id,
            Question = question.Trim(),
            Answer = answer.Trim(),
            DisplayOrder = displayOrder,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Update(string question, string answer, int displayOrder, bool isActive)
    {
        Question = question.Trim();
        Answer = answer.Trim();
        DisplayOrder = displayOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ContactMessage : Entity<Guid>
{
    public string FirstName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public string Subject { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public bool IsRead { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }

    private ContactMessage() { }

    public static ContactMessage Create(
        Guid id,
        string firstName,
        string email,
        string phone,
        string subject,
        string message)
    {
        return new ContactMessage
        {
            Id = id,
            FirstName = firstName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Phone = phone.Trim(),
            Subject = subject.Trim(),
            Message = message.Trim(),
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void MarkRead()
    {
        IsRead = true;
        ReadAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class EmailLog : Entity<Guid>
{
    public string ToEmail { get; private set; } = default!;
    public string Subject { get; private set; } = default!;
    public string TemplateKey { get; private set; } = default!;
    public EmailLogStatus Status { get; private set; } = EmailLogStatus.Pending;
    public int AttemptCount { get; private set; }
    public DateTime? LastAttemptAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }
    public string? LastError { get; private set; }

    private EmailLog() { }

    public static EmailLog Create(Guid id, string toEmail, string subject, string templateKey)
    {
        return new EmailLog
        {
            Id = id,
            ToEmail = toEmail,
            Subject = subject,
            TemplateKey = templateKey,
            Status = EmailLogStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void MarkSent()
    {
        Status = EmailLogStatus.Sent;
        SentAtUtc = DateTime.UtcNow;
        LastAttemptAtUtc = DateTime.UtcNow;
        AttemptCount++;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string error, bool willRetry)
    {
        Status = willRetry ? EmailLogStatus.Retrying : EmailLogStatus.Failed;
        LastError = error;
        LastAttemptAtUtc = DateTime.UtcNow;
        AttemptCount++;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
