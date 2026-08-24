using WorkOrderService.Api.Domain;
using Xunit;

namespace WorkOrderService.Tests;

public class UnitTests
{
    [Fact]
    public void Pending_To_InProgress_Should_Be_Allowed()
    {
        var result = WorkOrderStatusTransitions.CanTransition(
            WorkOrderStatus.Pending,
            WorkOrderStatus.InProgress);

        Assert.True(result);
    }

    [Fact]
    public void Completed_To_Pending_Should_Not_Be_Allowed()
    {
        var result = WorkOrderStatusTransitions.CanTransition(
            WorkOrderStatus.Completed,
            WorkOrderStatus.Pending);

        Assert.False(result);
    }
}