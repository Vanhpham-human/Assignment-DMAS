using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VnExpressCrawler.Shared.Models;

namespace VnExpressCrawler.Data.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("Articles");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(a => a.Content)
            .IsRequired()
            .HasColumnType("longtext");

        builder.Property(a => a.Url)
            .IsRequired()
            .HasMaxLength(2048);

        builder.HasIndex(a => a.Url)
            .IsUnique();

        builder.Property(a => a.CrawledAt)
            .IsRequired();
    }
}
