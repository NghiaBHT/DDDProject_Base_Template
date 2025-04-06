using DDDProject.Domain.Entities;
using DDDProject.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DDDProject.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // Configure table name (optional, EF Core uses DbSet name by default)
        builder.ToTable("Products");

        // Configure Primary Key
        builder.HasKey(p => p.Id);

        // Configure properties
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100); // Match validator/domain constraints

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        // Configure Value Object: Sku
        // Map to a single column in the database
        builder.OwnsOne(p => p.Sku,
            skuBuilder =>
            {
                skuBuilder.Property(s => s.Value)
                    .HasColumnName("Sku") // Map Sku.Value to the "Sku" column
                    .IsRequired()
                    .HasMaxLength(50); // Match VO constraint
            });
        // Add unique index on Sku
        builder.HasIndex(p => p.Sku).IsUnique();

        // Configure Value Object: Price (Money)
        // Map to owned entity columns (Price_Amount, Price_Currency)
        builder.OwnsOne(p => p.Price,
            priceBuilder =>
            {
                priceBuilder.Property(m => m.Amount)
                    .HasColumnName("PriceAmount") // Explicit column name
                    .HasColumnType("decimal(18, 2)"); // Specify SQL type

                priceBuilder.Property(m => m.Currency)
                    .HasColumnName("PriceCurrency") // Explicit column name
                    .IsRequired()
                    .HasMaxLength(3); // Match VO constraint
            });

        // Configure AuditableEntity properties (optional, defaults are usually fine)
        builder.Property(p => p.CreatedOnUtc).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.ModifiedOnUtc);
        builder.Property(p => p.ModifiedBy).HasMaxLength(100);

        // Ignore DomainEvents (not persisted)
        builder.Ignore(p => p.DomainEvents);
    }
} 