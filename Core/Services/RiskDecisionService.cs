using System.ComponentModel.DataAnnotations;
using Core.Entities;
using Core.Interfaces;
using Core.Massages;

namespace Core.Services;

public class RiskDecisionService : IRiskDecisionService
{
    
    public RiskDecisionService()
    {
        
    }
    
    public (bool IsApproved, string? Reason) Decide(OrderSubmitted order, UserAccountEntity user, CancellationToken ct)
    {
        Console.WriteLine($"Starting deciding if order{order} approved ... ");
        if (!IsExposureRiskPassed(order, user, ct)) 
            return (false, "Exposure limit exceeded");
        
        if (!IsMarginRiskPassed(order, user, ct)) 
            return (false, "Insufficient margin");
        
        if (!IsVelocityRiskPassed(order, user, ct)) 
            return (false, "Velocity limit exceeded");

        return (true, null);
        
    }

    public bool IsExposureRiskPassed(OrderSubmitted order, UserAccountEntity user, CancellationToken ct)
    {
        if(order.NumberOfShares <= 0m || order.PricePerShare <= 0m) 
            return false;
        
        // NLV = Net Liquidation Value
        // Exposure cap = max( NLV * k , BuyingPower + TotalCash )
        const decimal k = 1.50m; //exposure-to-NLV multiplier 
        
        // order size
        var notional = order.NumberOfShares * order.PricePerShare;
        
        var delta = order.Action == OrderAction.Buy ? notional : -notional;
        
        var exposureCapFromNlv  = Math.Max(0m, user.NetLiquidationValue * k);
        var exposureCapFromCash = Math.Max(0m, user.BuyingPower + user.TotalCash);
        var exposureCap         = Math.Max(exposureCapFromNlv, exposureCapFromCash);
        
        // - Sells reduce exposure → allow them.
        // - Buys increase exposure → must fit under the cap by themselves.
        if (order.Action == OrderAction.Sell)
            return true;

        return notional <= exposureCap;
    }

    public bool IsMarginRiskPassed(OrderSubmitted order, UserAccountEntity user, CancellationToken ct)
    {
        if (order.NumberOfShares <= 0m || order.PricePerShare <= 0m)
            return false;

        // sells don't consume margin in this simple policy
        if (order.Action == OrderAction.Sell)
            return true;

        // order size
        decimal notional = order.NumberOfShares * order.PricePerShare;

        // simple fixed initial margin rate (no utilization because we don't have exposure)
        const decimal initialMarginRate = 0.25m; // 25% — adjust if you want stricter

        decimal required = initialMarginRate * notional;
        decimal available = user.BuyingPower; // using BP as your "available margin" proxy.

        return available >= required;
    }

    
    //TODO: change the check
    public bool IsVelocityRiskPassed(OrderSubmitted order, UserAccountEntity user, CancellationToken ct)
    {
        int baseAllowed = 5 + (int)Math.Floor(user.BuyingPower / 10_000m); // +1 per 10k BuyingPower
        int velocityMaxAllowed = Math.Clamp(baseAllowed, 5, 50);

        // With no window/count available, a single order is always within the allowance.
        return velocityMaxAllowed >= 1;
    }
}