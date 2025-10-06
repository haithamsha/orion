using Orion.Application.Services.CQRS;
using Orion.Application.Services.CQRS.Commands;
using Orion.Infrastructure.Data;
using Orion.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Orion.Application.Services.CQRS.Handlers;

/// <summary>
/// Command handler for changing order status
/// </summary>
public class ChangeOrderStatusCommandHandler : ICommandHandler<ChangeOrderStatusCommand>
{
    private readonly OrionDbContext _dbContext;
    private readonly ILogger<ChangeOrderStatusCommandHandler> _logger;

    public ChangeOrderStatusCommandHandler(
        OrionDbContext dbContext,
        ILogger<ChangeOrderStatusCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(ChangeOrderStatusCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing ChangeOrderStatusCommand for order {OrderId} to status {Status}", 
            request.OrderId, request.NewStatus);

        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            throw new InvalidOperationException($"Order {request.OrderId} not found");
        }

        var oldStatus = order.Status;
        order.Status = request.NewStatus;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} status changed from {OldStatus} to {NewStatus}. Reason: {Reason}",
            request.OrderId, oldStatus, request.NewStatus, request.Reason ?? "No reason provided");
    }
}