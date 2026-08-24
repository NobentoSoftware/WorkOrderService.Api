namespace WorkOrderService.Api.Contracts;

public class CreateWorkOrderRequest
{
    public string ExternalId { get; set; } = null!;
    public string SiteCode { get; set; } = null!;
    public string Description { get; set; } = null!;
}