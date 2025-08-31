using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Configurations;

public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccountEntity>
{

    public void Configure(EntityTypeBuilder<UserAccountEntity> builder)
    {
        builder.ToTable("UserAccounts");
        builder.HasKey(u => u.AccountId);
        
        builder.Property(u => u.UserName).IsRequired().HasMaxLength(64);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);

        builder.Property(u => u.UserType)
            .HasConversion<short>()        // align with Postgres smallint like others
            .HasColumnType("smallint");

        builder.Property(u => u.NetLiquidationValue).HasPrecision(18, 2);
        builder.Property(u => u.TotalCash).HasPrecision(18, 2);
        builder.Property(u => u.BuyingPower).HasPrecision(18, 2);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.UserType);
        
    }
    
}