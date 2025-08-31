using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccess.Repositories;

public class OrdersRepo
{
    private readonly TradingDatabase _dbContext;

    public OrdersRepo(TradingDatabase dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<OrderEntity>> Get()
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .OrderBy(t => t.SubmittedAtUtc)
            .ToListAsync();
    }

    public async Task<List<OrderEntity>> GetWithRiskDecisions()
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .Include(t => t.Status)
            .Include(t => t.Ticker)
            .ToListAsync();
    }

    public async Task<OrderEntity?> GetById(Guid id)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(t=>t.OrderId == id);
    }

    public async Task<List<OrderEntity>> GetByFilter(string ticker)
    {
        var query = _dbContext.Orders.AsNoTracking();
        
        query = query.Where(t => t.Ticker.Contains(ticker));
        
        return await query.ToListAsync();
    }
    
    
    

}