using Microsoft.EntityFrameworkCore;
using WorkOrderService.Api.Contracts;
using WorkOrderService.Api.Domain;
using WorkOrderService.Api.Persistence;

namespace WorkOrderService.Api.Endpoints;

public static class WorkOrderEndpoints
{
    public static void MapWorkOrderEndpoints(this WebApplication app)
    {
        app.MapPost("/api/work-orders", async (
            CreateWorkOrderRequest request,
            AppDbContext db) =>
        {
            var workOrder = new WorkOrder
            {
                ExternalId = request.ExternalId,
                SiteCode = request.SiteCode,
                Description = request.Description,
                Status = WorkOrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.WorkOrders.Add(workOrder);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/work-orders/{workOrder.Id}",
                workOrder);
        });

        app.MapGet("/api/work-orders/{id:int}", async (
            int id,
            AppDbContext db) =>
        {
            var workOrder = await db.WorkOrders
                .AsNoTracking()
                .Include(x => x.StatusHistory)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (workOrder == null)
            {
                return Results.NotFound(new
                {
                    message = "Work order not found."
                });
            }

            var response = new WorkOrderResponse
            {
                Id = workOrder.Id,
                ExternalId = workOrder.ExternalId,
                SiteCode = workOrder.SiteCode,
                Description = workOrder.Description,
                Status = workOrder.Status.ToString(),
                CreatedAt = workOrder.CreatedAt,
                UpdatedAt = workOrder.UpdatedAt,

                StatusHistory = workOrder.StatusHistory
            .OrderBy(x => x.ChangedAt)
            .Select(x => new WorkOrderStatusHistoryResponse
            {
                FromStatus = x.FromStatus.ToString(),
                ToStatus = x.ToStatus.ToString(),
                ChangedAt = x.ChangedAt
            })
            .ToList()
            };

            return Results.Ok(response);
        });

        app.MapPatch("/api/work-orders/{id:int}/status", async (
            int id,
            UpdateWorkOrderStatusRequest request,
            AppDbContext db) =>
        {


            var workOrder = await db.WorkOrders
                .FirstOrDefaultAsync(x => x.Id == id);

            if (workOrder == null)
            {
                return Results.NotFound(new
                {
                    message = "Work order not found."
                });
            }

            if (workOrder.Status == request.Status)
            {
                return Results.BadRequest(new
                {
                    message = "Work order is already in this status."
                });
            }

            if (!WorkOrderStatusTransitions.CanTransition(
                workOrder.Status,
                request.Status))          
            {
                return Results.Conflict(new
                {
                    message = $"Cannot change status from {workOrder.Status} to {request.Status}."
                });
            }

            var oldStatus = workOrder.Status;

            workOrder.Status = request.Status;
            workOrder.UpdatedAt = DateTime.UtcNow;

            var history = new WorkOrderStatusHistory
            {
                WorkOrderId = workOrder.Id,
                FromStatus = oldStatus,
                ToStatus = request.Status,
                ChangedAt = DateTime.UtcNow
            };

            db.WorkOrderStatusHistories.Add(history);

            await db.SaveChangesAsync();

            var response = new WorkOrderResponse
            {
                Id = workOrder.Id,
                ExternalId = workOrder.ExternalId,
                SiteCode = workOrder.SiteCode,
                Description = workOrder.Description,
                Status = workOrder.Status.ToString(),
                CreatedAt = workOrder.CreatedAt,
                UpdatedAt = workOrder.UpdatedAt,
                StatusHistory = new List<WorkOrderStatusHistoryResponse>
                {
                    new()
                    {
                        FromStatus = oldStatus.ToString(),
                        ToStatus = request.Status.ToString(),
                        ChangedAt = history.ChangedAt
                    }
                }
            };

            return Results.Ok(response);
        });

        app.MapGet("/api/work-orders", async (
            WorkOrderStatus? status,
            AppDbContext db) =>
        {
            var query = db.WorkOrders
                .AsNoTracking()
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            const int pageSize = 20;

            var workOrders = await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(workOrders);
        });
    }
}