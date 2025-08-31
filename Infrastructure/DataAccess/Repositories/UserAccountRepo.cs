using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccess.Repositories;

public class UserAccountsRepo
{
    private readonly TradingDatabase _dbContext;

    public UserAccountsRepo(TradingDatabase dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserAccountEntity?> GetById(Guid id)
    {
        return await _dbContext.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.AccountId == id);
    }
    
    public async Task<List<UserAccountEntity>> GetByFilter(string userName)
    {
        var query = _dbContext.UserAccounts.AsNoTracking();
        
        query = query.Where(u => u.UserName.Contains(userName));
        
        return await query.ToListAsync();

    }
}