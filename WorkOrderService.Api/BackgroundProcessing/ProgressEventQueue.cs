using System.Threading.Channels;
using WorkOrderService.Api.Contracts;

namespace WorkOrderService.Api.BackgroundProcessing;

public class ProgressEventQueue : IProgressEventQueue
{
    private readonly Channel<ProgressEventRequest> _queue;

    public ProgressEventQueue()
    {
        var options = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        _queue = Channel.CreateBounded<ProgressEventRequest>(options);
    }

    public async ValueTask QueueAsync(
        ProgressEventRequest progressEvent,
        CancellationToken cancellationToken = default)
    {
        await _queue.Writer.WriteAsync(
            progressEvent,
            cancellationToken);
    }

    public async ValueTask<ProgressEventRequest> DequeueAsync(
        CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}