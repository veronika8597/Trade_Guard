// API/Workers/TradeGuardWorker.cs
using System.Text.Json;
using Core.Entities;
using Core.Interfaces;
using Core.Massages;
using Infrastructure.DataAccess;
using Infrastructure.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

public class TradeGuardWorker : BackgroundService
{
    private readonly IMessageBusService _busService; // typically Singleton
    private readonly IServiceScopeFactory _scopeFactory; // create Scoped services per message
    private readonly ILogger<TradeGuardWorker> _logger;

    public TradeGuardWorker(
        IMessageBusService busService,
        IServiceScopeFactory scopeFactory,
        ILogger<TradeGuardWorker> logger
        )
    {
        _busService = busService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TradeGuardWorker starting…");

        await _busService.SubscribeAsync<OrderSubmitted>(
            queueName: "orders_submitted_q",
            routingKey: "orders.submitted",
            handler: OnOrderAsync, // our scoped handler
            ct: stoppingToken);

        _logger.LogInformation("Subscribed to orders_submitted_q (orders.submitted)");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TradeGuardWorker stopping (cancellation requested).");
        }
    }

    //TODO: fetch accountm by ID and pass to the riskDecide func 
    //funcc Decision shpuld return the decision itself too 
    private async Task OnOrderAsync(OrderSubmitted msg)
    {
using var scope = _scopeFactory.CreateScope();

        // resolve per-message services
        var db    = scope.ServiceProvider.GetRequiredService<TradingDatabase>();
        var risk  = scope.ServiceProvider.GetRequiredService<IRiskDecisionService>();

        // 1) load the account for this message
        var user = await db.UserAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.AccountId == msg.AccountId);

        if (user is null)
        {
            _logger.LogWarning("Account {AccountId} not found; rejecting order {OrderId}", msg.AccountId, msg.OrderId);
            await _busService.PublishAsync("orders.decided",
                new OrderDecision(msg.OrderId, false, "Account not found"),
                CancellationToken.None);
            return;
        }

        // 2) decide (your simplified signature that uses only user fields)
        var decision = risk.Decide(msg, user, CancellationToken.None);
        var finalStatus = decision.IsApproved ? OrderStatus.Approved : OrderStatus.Rejected;

        // 3) persist (simple upsert)
        var entity = await db.Orders.SingleOrDefaultAsync(o => o.OrderId == msg.OrderId);
        var notional = msg.NumberOfShares * msg.PricePerShare;

        if (entity is null)
        {
            entity = new OrderEntity
            {
                OrderId        = msg.OrderId,
                AccountId      = msg.AccountId,
                Ticker         = msg.Ticker.ToUpperInvariant(),
                Action         = msg.Action,
                ActionMode     = msg.ActionMode,
                NumberOfShares = msg.NumberOfShares,
                PricePerShare  = msg.PricePerShare,
                StopLossPrice  = msg.StopLossPrice,
                TotalCost      = notional,
                Status         = finalStatus,
                SubmittedAtUtc = msg.SubmittedAtUtc
            };
            db.Orders.Add(entity);
        }
        else
        {
            entity.Ticker         = msg.Ticker.ToUpperInvariant();
            entity.Action         = msg.Action;
            entity.ActionMode     = msg.ActionMode;
            entity.NumberOfShares = msg.NumberOfShares;
            entity.PricePerShare  = msg.PricePerShare;
            entity.StopLossPrice  = msg.StopLossPrice;
            entity.TotalCost      = notional;
            entity.Status         = finalStatus;
            entity.SubmittedAtUtc = msg.SubmittedAtUtc;
        }

        await db.SaveChangesAsync();

        // 4) log + publish outcome
        _logger.LogInformation("Order {OrderId} approved={Approved} reason={Reason} payload={Payload}",
            msg.OrderId, decision.IsApproved, decision.Reason, JsonSerializer.Serialize(msg));

        await _busService.PublishAsync(
            routingKey: "orders.decided",
            message: new OrderDecision(msg.OrderId, decision.IsApproved, decision.Reason),
            ct: CancellationToken.None);
    }
}
