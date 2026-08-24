using WorkOrderService.Api.Contracts;

namespace WorkOrderService.Api.BackgroundProcessing;

public interface IProgressEventQueue
{
    ValueTask QueueAsync(
        ProgressEventRequest progressEvent,
        CancellationToken cancellationToken = default);

    ValueTask<ProgressEventRequest> DequeueAsync(
        CancellationToken cancellationToken);
}