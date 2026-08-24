using Microsoft.EntityFrameworkCore;
using WorkOrderService.Api.Domain;
using WorkOrderService.Api.Persistence;

namespace WorkOrderService.Api.BackgroundProcessing;

public class ProgressEventWorker : BackgroundService
{
    private readonly IProgressEventQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProgressEventWorker> _logger;

    public ProgressEventWorker(
        IProgressEventQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<ProgressEventWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var progressEvent = await _queue.DequeueAsync(stoppingToken);

            try
            {
                await ProcessEventAsync(progressEvent, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process progress event {EventId}",
                    progressEvent.EventId);
            }
        }
    }

    private async Task ProcessEventAsync(
        Contracts.ProgressEventRequest progressEvent,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var alreadyProcessed = await db.ProcessedProgressEvents
            .AnyAsync(
                x => x.EventId == progressEvent.EventId,
                cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Progress event {EventId} has already been processed.",
                progressEvent.EventId);

            return;
        }

        var workOrder = await db.WorkOrders
            .FirstOrDefaultAsync(
                x => x.ExternalId == progressEvent.WorkOrderExternalId,
                cancellationToken);

        if (workOrder == null)
        {
            _logger.LogWarning(
                "Work order {ExternalId} was not found.",
                progressEvent.WorkOrderExternalId);

            return;
        }

        if (!Enum.TryParse<WorkOrderStatus>(
                progressEvent.NewStatus,
                true,
                out var newStatus))
        {
            _logger.LogWarning(
                "Invalid work order status {Status}.",
                progressEvent.NewStatus);

            return;
        }

        var oldStatus = workOrder.Status;

        if (oldStatus == newStatus)
        {
            db.ProcessedProgressEvents.Add(
                new ProcessedProgressEvent
                {
                    EventId = progressEvent.EventId,
                    WorkOrderId = workOrder.Id,
                    ProcessedAt = DateTime.UtcNow
                });

            await db.SaveChangesAsync(cancellationToken);

            return;
        }

        workOrder.Status = newStatus;
        workOrder.UpdatedAt = DateTime.UtcNow;

        db.WorkOrderStatusHistories.Add(
            new WorkOrderStatusHistory
            {
                WorkOrderId = workOrder.Id,
                FromStatus = oldStatus,
                ToStatus = newStatus,
                ChangedAt = progressEvent.OccurredAt
            });

        db.ProcessedProgressEvents.Add(
            new ProcessedProgressEvent
            {
                EventId = progressEvent.EventId,
                WorkOrderId = workOrder.Id,
                ProcessedAt = DateTime.UtcNow
            });

        await db.SaveChangesAsync(cancellationToken);
    }
}