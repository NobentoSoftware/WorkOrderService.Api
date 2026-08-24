namespace WorkOrderService.Api.Domain;

public static class WorkOrderStatusTransitions
{
    public static bool CanTransition(
        WorkOrderStatus current,
        WorkOrderStatus next)
    {
        return current switch
        {
            WorkOrderStatus.Pending =>
                next is WorkOrderStatus.InProgress
                    or WorkOrderStatus.Cancelled,

            WorkOrderStatus.InProgress =>
                next is WorkOrderStatus.Completed
                    or WorkOrderStatus.Cancelled,

            _ => false
        };
    }
}