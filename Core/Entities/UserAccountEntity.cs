namespace Core.Entities;
public enum UserType { Trader = 0, Admin = 1 }

public class UserAccountEntity
{
    public Guid AccountId { get; set; } // unique id for the account row
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public UserType UserType { get; set; }
    public decimal NetLiquidationValue { get; set; }
    public decimal TotalCash { get; set; }
    public decimal BuyingPower { get; set; }
}