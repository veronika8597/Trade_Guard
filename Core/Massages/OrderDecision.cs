namespace Core.Massages;

public record OrderDecision(Guid OrderId, bool Approved, string? Reason);