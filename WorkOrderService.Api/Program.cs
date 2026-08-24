using Microsoft.EntityFrameworkCore;
using WorkOrderService.Api.Endpoints;
using WorkOrderService.Api.Persistence;
using WorkOrderService.Api.BackgroundProcessing;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<IProgressEventQueue, ProgressEventQueue>();
builder.Services.AddHostedService<ProgressEventWorker>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Work Order Service is running");

app.MapWorkOrderEndpoints();
app.MapProgressEventEndpoints();

app.Run();

public partial class Program { }