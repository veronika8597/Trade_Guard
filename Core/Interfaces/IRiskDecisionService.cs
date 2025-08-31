using Core.Entities;
using Core.Massages;

namespace Core.Interfaces;

public interface IRiskDecisionService
{
    public (bool IsApproved, string? Reason) Decide(OrderSubmitted order, UserAccountEntity user ,CancellationToken ct);

    public bool IsExposureRiskPassed(OrderSubmitted order, UserAccountEntity user, CancellationToken ct);
    public bool IsMarginRiskPassed(OrderSubmitted order, UserAccountEntity user, CancellationToken ct);
    public bool IsVelocityRiskPassed(OrderSubmitted order, UserAccountEntity user, CancellationToken ct);
}