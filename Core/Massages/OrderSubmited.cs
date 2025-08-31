using Core.Entities;

namespace Core.Massages;

public record OrderSubmitted(
    Guid OrderId,
    Guid AccountId,
    string Ticker,
    OrderAction Action,     // Buy/Sell
    ActionMode ActionMode,  // Market/Limit/StopLoss
    decimal NumberOfShares,
    decimal PricePerShare,
    decimal? StopLossPrice,
    DateTime SubmittedAtUtc
);