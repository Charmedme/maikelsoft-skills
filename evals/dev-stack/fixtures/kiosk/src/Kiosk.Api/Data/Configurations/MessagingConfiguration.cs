using Kiosk.Api.Chatbot;
using Kiosk.Api.Mail;
using Kiosk.Api.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kiosk.Api.Data.Configurations;

internal sealed class SupportMessageConfiguration : IEntityTypeConfiguration<SupportMessage>
{
    public void Configure(EntityTypeBuilder<SupportMessage> builder)
    {
        builder.Property(m => m.ConversationId).HasMaxLength(80);
        builder.HasIndex(m => m.ConversationId);
    }
}

internal sealed class OutboxEmailConfiguration : IEntityTypeConfiguration<OutboxEmail>
{
    public void Configure(EntityTypeBuilder<OutboxEmail> builder)
    {
        builder.ToTable("OutboxEmails");
        builder.Property(m => m.To).HasMaxLength(256);
        builder.Property(m => m.Subject).HasMaxLength(200);
    }
}

internal sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.Property(c => c.Code).HasMaxLength(32);
        builder.HasIndex(c => c.Code).IsUnique();
    }
}
