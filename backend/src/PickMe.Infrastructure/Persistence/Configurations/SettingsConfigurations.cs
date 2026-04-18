using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PickMe.Domain.Entities;

namespace PickMe.Infrastructure.Persistence.Configurations;

public sealed class AdminNotificationRecipientConfiguration : IEntityTypeConfiguration<AdminNotificationRecipient>
{
    public void Configure(EntityTypeBuilder<AdminNotificationRecipient> b)
    {
        b.ToTable("AdminNotificationRecipients");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).IsRequired().HasMaxLength(256);
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
    }
}

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> b)
    {
        b.ToTable("SystemSettings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Key).IsRequired().HasMaxLength(128);
        b.HasIndex(x => x.Key).IsUnique();
        b.Property(x => x.Value).IsRequired().HasMaxLength(2000);
        b.Property(x => x.IsSensitive).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
    }
}

public sealed class FaqConfiguration : IEntityTypeConfiguration<Faq>
{
    public void Configure(EntityTypeBuilder<Faq> b)
    {
        b.ToTable("Faqs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Question).IsRequired().HasMaxLength(500);
        b.Property(x => x.Answer).IsRequired().HasMaxLength(4000);
        b.Property(x => x.DisplayOrder).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
        b.HasIndex(x => new { x.IsActive, x.DisplayOrder });
    }
}

public sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> b)
    {
        b.ToTable("ContactMessages");
        b.HasKey(x => x.Id);
        b.Property(x => x.FirstName).IsRequired().HasMaxLength(50);
        b.Property(x => x.Email).IsRequired().HasMaxLength(256);
        b.Property(x => x.Phone).IsRequired().HasMaxLength(25);
        b.Property(x => x.Subject).IsRequired().HasMaxLength(120);
        b.Property(x => x.Message).IsRequired().HasMaxLength(2000);
        b.Property(x => x.IsRead).IsRequired();
        b.Property(x => x.ReadAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
        b.HasIndex(x => x.IsRead);
    }
}

public sealed class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> b)
    {
        b.ToTable("EmailLogs");
        b.HasKey(x => x.Id);
        b.Property(x => x.ToEmail).IsRequired().HasMaxLength(256);
        b.Property(x => x.Subject).IsRequired().HasMaxLength(256);
        b.Property(x => x.TemplateKey).IsRequired().HasMaxLength(128);
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.AttemptCount).IsRequired();
        b.Property(x => x.LastAttemptAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.SentAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.LastError).HasMaxLength(2000);
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
        b.HasIndex(x => x.Status);
    }
}
