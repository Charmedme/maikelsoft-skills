using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders.Api.Mail;

internal sealed class OutboxMailConfiguration : IEntityTypeConfiguration<OutboxMail>
{
    public void Configure(EntityTypeBuilder<OutboxMail> builder)
    {
        builder.ToTable("outbox_mails");
        builder.HasKey(mail => mail.Id);
        builder.Property(mail => mail.Id).ValueGeneratedOnAdd();
        builder.Property(mail => mail.ToAddress).HasMaxLength(320);
        builder.Property(mail => mail.ToName).HasMaxLength(200);
        builder.Property(mail => mail.Subject).HasMaxLength(200);
        builder.Property(mail => mail.TextBody).HasMaxLength(100_000);
        builder.Property(mail => mail.HtmlBody).HasMaxLength(200_000);
        builder.Property(mail => mail.LastError).HasMaxLength(1000);
        builder.HasIndex(mail => mail.NextAttemptAt);
    }
}
