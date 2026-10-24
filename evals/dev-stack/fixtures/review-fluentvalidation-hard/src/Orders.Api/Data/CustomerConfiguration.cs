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
        builder.Property(customer => customer.Name).HasMaxLength(100);
        builder.Property(customer => customer.AddressLine).HasMaxLength(120);
        builder.Property(customer => customer.PostalCode).HasMaxLength(10);
        builder.Property(customer => customer.City).HasMaxLength(80);
        builder.Property(customer => customer.CountryCode).HasMaxLength(2).IsFixedLength();
        builder.Ignore(customer => customer.Address);
        builder.HasIndex(customer => customer.Email).IsUnique();
    }
}
