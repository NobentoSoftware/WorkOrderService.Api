using WorkOrderService.Api.BackgroundProcessing;
using WorkOrderService.Api.Contracts;

namespace WorkOrderService.Api.Endpoints;

public static class ProgressEventEndpoints
{
    public static void MapProgressEventEndpoints(
        this WebApplication app)
    {
        app.MapPost("/api/progress-events", async (
            ProgressEventRequest request,
            IProgressEventQueue queue,
            CancellationToken cancellationToken) =>
        {
            await queue.QueueAsync(
                request,
                cancellationToken);

            return Results.Accepted();
        });
    }
}