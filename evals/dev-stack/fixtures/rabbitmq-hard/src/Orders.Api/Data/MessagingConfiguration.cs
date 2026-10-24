using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Inventory;
using Orders.Api.Messaging;

namespace Orders.Api.Data;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).UseIdentityAlwaysColumn();
        builder.Property(message => message.MessageType).HasMaxLength(64);
        builder.Property(message => message.RoutingKey).HasMaxLength(64);
        builder.Property(message => message.CorrelationId).HasMaxLength(64);
        builder.Property(message => message.Payload).HasColumnType("text").Metadata.SetMaxLength(null);
        builder.HasIndex(message => message.MessageId).IsUnique();
        builder.HasIndex(message => message.Id).HasFilter("\"SentAt\" IS NULL").HasDatabaseName("IX_outbox_messages_unsent");
    }
}

internal sealed class StockReceiptConfiguration : IEntityTypeConfiguration<StockReceipt>
{
    public void Configure(EntityTypeBuilder<StockReceipt> builder)
    {
        builder.ToTable("stock_receipts");
        builder.HasKey(receipt => receipt.ReceiptId);
        builder.Property(receipt => receipt.ReceiptId).ValueGeneratedNever();
        builder.Property(receipt => receipt.Sku).HasMaxLength(32);
    }
}
