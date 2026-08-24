namespace WorkOrderService.Api.Domain;

public class WorkOrder
{
    public int Id { get; set; }

    public string ExternalId { get; set; } = null!;

    public string SiteCode { get; set; } = null!;

    public string Description { get; set; } = null!;

    public WorkOrderStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<WorkOrderStatusHistory> StatusHistory { get; set; } = [];
}