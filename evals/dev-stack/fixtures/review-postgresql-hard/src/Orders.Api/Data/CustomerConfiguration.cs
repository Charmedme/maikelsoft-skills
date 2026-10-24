using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Customers;

namespace Orders.Api.Data;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.Id).ValueGeneratedOnAdd();
        builder.Property(customer => customer.Name).HasMaxLength(200);
        builder.Property(customer => customer.Email).HasMaxLength(320);
        builder.HasIndex(customer => customer.Email).IsUnique();
    }
}
