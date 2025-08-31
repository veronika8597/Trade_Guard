using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Infrastructure.DataAccess.Configurations;

namespace Infrastructure.DataAccess;

public class TradingDatabase(DbContextOptions<TradingDatabase> options) 
    : DbContext(options)
{
    public DbSet<OrderEntity> Orders { get; set; }
    public DbSet<UserAccountEntity> UserAccounts { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new UserAccountConfiguration());
        
        base.OnModelCreating(modelBuilder);
        
    }
}