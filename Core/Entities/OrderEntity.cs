namespace Core.Entities;

public enum OrderAction { Buy = 0, Sell = 1 }
public enum ActionMode { Market = 0, Limit  = 1, StopLoss = 2 }
public enum OrderStatus { Rejected = 0, Approved = 1}

public sealed class OrderEntity
{
    public Guid OrderId { get; set; } // unique id for the trade row
    public Guid AccountId { get; set; } // FK to UserAccountEntity.AccountId
    public string Ticker { get; set; } = null!; // Stock ticker, e.g. "PLTR" 
    public OrderAction Action { get; set; }   // Buy | Sell
    public ActionMode ActionMode { get; set; }  // Market | Limit | StopLoss
    public decimal NumberOfShares { get; set; }
    public decimal PricePerShare { get; set; } // Required only when PriceMode == Limit; otherwise null
    public decimal? StopLossPrice { get; set; } // Required only when StopLossMode == StopLoss; otherwise null
    public decimal TotalCost { get; set; }
    public OrderStatus Status { get; set; }   // Approved | Rejected
    public DateTime SubmittedAtUtc { get; set; }
}