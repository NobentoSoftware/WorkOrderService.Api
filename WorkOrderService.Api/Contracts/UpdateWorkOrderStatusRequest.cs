using WorkOrderService.Api.Domain;

namespace WorkOrderService.Api.Contracts;

public class UpdateWorkOrderStatusRequest
{
    public WorkOrderStatus Status { get; set; }
}