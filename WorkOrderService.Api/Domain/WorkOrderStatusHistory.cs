namespace WorkOrderService.Api.Domain;
public class WorkOrderStatusHistory
{
    public int Id { get; set; }

    public int WorkOrderId { get; set; }

    public WorkOrder WorkOrder { get; set; } = null!;

    public WorkOrderStatus FromStatus { get; set; }

    public WorkOrderStatus ToStatus { get; set; }

    public DateTime ChangedAt { get; set; }
}
