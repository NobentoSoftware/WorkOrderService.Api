namespace WorkOrderService.Api.Domain;

public class ProcessedProgressEvent
{
    public int Id { get; set; }

    public string EventId { get; set; } = null!;

    public int WorkOrderId { get; set; }

    public DateTime ProcessedAt { get; set; }
}