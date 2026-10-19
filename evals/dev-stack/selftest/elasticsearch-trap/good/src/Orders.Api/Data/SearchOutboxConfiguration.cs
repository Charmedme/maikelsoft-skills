using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Search;

namespace Orders.Api.Data;

internal sealed class SearchOutboxConfiguration : IEntityTypeConfiguration<SearchOutboxEntry>
{
    public void Configure(EntityTypeBuilder<SearchOutboxEntry> builder)
    {
        builder.ToTable("search_outbox");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).UseIdentityAlwaysColumn();
    }
}
