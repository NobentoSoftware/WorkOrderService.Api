using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using WorkOrderService.Api.Domain;

namespace WorkOrderService.Api.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();

    public DbSet<WorkOrderStatusHistory> WorkOrderStatusHistories
    => Set<WorkOrderStatusHistory>();

    public DbSet<ProcessedProgressEvent> ProcessedProgressEvents
    => Set<ProcessedProgressEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ExternalId)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => x.ExternalId)
                .IsUnique();

            entity.Property(x => x.SiteCode)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<WorkOrderStatusHistory>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FromStatus)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(x => x.ToStatus)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasOne(x => x.WorkOrder)
                .WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProcessedProgressEvent>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.EventId)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => x.EventId)
                .IsUnique();
        });
    }
}