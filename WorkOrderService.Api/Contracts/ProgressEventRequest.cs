namespace WorkOrderService.Api.Contracts;

public class ProgressEventRequest
{
    public string EventId { get; set; } = null!;

    public string WorkOrderExternalId { get; set; } = null!;

    public string NewStatus { get; set; } = null!;

    public DateTime OccurredAt { get; set; }

    public string? Details { get; set; }
}