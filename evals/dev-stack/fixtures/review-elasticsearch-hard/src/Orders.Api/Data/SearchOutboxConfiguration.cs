using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Search;

namespace Orders.Api.Data;

internal sealed class SearchOutboxConfiguration : IEntityTypeConfiguration<ProductSearchOutboxEntry>
{
    public void Configure(EntityTypeBuilder<ProductSearchOutboxEntry> builder)
    {
        builder.ToTable("product_search_outbox");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).UseIdentityAlwaysColumn();
    }
}
