using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Invoices;
using Orders.Api.Orders;

namespace Orders.Api.Data;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");
        builder.HasKey(invoice => invoice.Id);
        builder.Property(invoice => invoice.Id).ValueGeneratedOnAdd();
        builder.Property(invoice => invoice.Number).HasMaxLength(16);
        builder.HasIndex(invoice => invoice.Number).IsUnique();
        builder.HasOne<Order>().WithMany().HasForeignKey(invoice => invoice.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(invoice => invoice.OrderId).IsUnique();
    }
}

internal sealed class InvoiceCounterConfiguration : IEntityTypeConfiguration<InvoiceCounter>
{
    public void Configure(EntityTypeBuilder<InvoiceCounter> builder)
    {
        builder.ToTable("invoice_counters");
        builder.HasKey(counter => counter.Year);
        builder.Property(counter => counter.Year).ValueGeneratedNever();
    }
}
