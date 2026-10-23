using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Mail;

namespace Orders.Api.Data;

internal sealed class OutboxMailConfiguration : IEntityTypeConfiguration<OutboxMail>
{
    public void Configure(EntityTypeBuilder<OutboxMail> builder)
    {
        builder.ToTable("outbox_mails");
        builder.HasKey(mail => mail.Id);
        builder.Property(mail => mail.Id).ValueGeneratedOnAdd();
        builder.Property(mail => mail.ToAddress).HasMaxLength(254);
        builder.Property(mail => mail.ToName).HasMaxLength(200);
        builder.Property(mail => mail.Subject).HasMaxLength(200);
        builder.Property(mail => mail.TextBody).HasMaxLength(20000);
        builder.Property(mail => mail.HtmlBody).HasMaxLength(50000);
        builder.Property(mail => mail.LastError).HasMaxLength(200);
        builder.HasIndex(mail => mail.NextAttemptAt);
    }
}
