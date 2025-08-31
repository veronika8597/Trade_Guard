using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<OrderEntity>
{
    public void Configure(EntityTypeBuilder<OrderEntity> builder)
    {
        builder.ToTable("Orders");
 
        builder.HasKey(o => o.OrderId); //Primary key 

        // Required fields / precision
        builder.Property(o => o.Ticker)
            .IsRequired()
            .HasMaxLength(5);

        builder.Property(o => o.Action)     // enum -> int (explicit for clarity)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(o => o.ActionMode) // enum -> int
            .HasConversion<int>()
            .IsRequired();

        builder.Property(o => o.Status)     // enum -> int
            .HasConversion<int>()
            .IsRequired();

        builder.Property(o => o.NumberOfShares)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(o => o.PricePerShare)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(o => o.TotalCost)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(o => o.StopLossPrice)
            .HasPrecision(18, 4)
            .IsRequired(false);

        builder.Property(o => o.SubmittedAtUtc)
            .IsRequired();

        // Indexes for your common queries
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => new { o.AccountId, o.SubmittedAtUtc });
        builder.HasIndex(o => new { o.AccountId, o.Status, o.SubmittedAtUtc });
        builder.HasIndex(o => new { o.Ticker, o.SubmittedAtUtc });

        // FK-only relation: one UserAccount -> many Orders (no collection nav on User)
        builder.HasOne<UserAccountEntity>()        // no nav on UserAccountEntity
            .WithMany()                         // no collection nav
            .HasForeignKey(o => o.AccountId)
            .OnDelete(DeleteBehavior.Restrict); // safer for audit/history
    }
}
//docker exec -it tradeguard-db psql -U trade -d tradeguard