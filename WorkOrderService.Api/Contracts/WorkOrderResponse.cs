namespace WorkOrderService.Api.Contracts;

public class WorkOrderResponse
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = null!;
    public string SiteCode { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<WorkOrderStatusHistoryResponse> StatusHistory { get; set; } = [];
}

public class WorkOrderStatusHistoryResponse
{
    public string FromStatus { get; set; } = null!;
    public string ToStatus { get; set; } = null!;
    public DateTime ChangedAt { get; set; }
}