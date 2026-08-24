using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkOrderService.Api.Contracts;
using WorkOrderService.Api.Persistence;

namespace WorkOrderService.Tests;

public class IntegrationTests
    : IClassFixture<IntegrationTests.CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public IntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateWorkOrder_ShouldReturnCreated()
    {
        var request = new
        {
            externalId = "WO-TEST-001",
            siteCode = "JHB-TEST",
            description = "Integration test work order"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/work-orders",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<WorkOrderResponse>();

        Assert.NotNull(result);
        Assert.Equal("WO-TEST-001", result.ExternalId);
        Assert.Equal("JHB-TEST", result.SiteCode);
        Assert.Equal("Pending", result.Status);
    }

    public class CustomWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        private SqliteConnection? _connection;

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services
                    .SingleOrDefault(
                        d => d.ServiceType ==
                             typeof(DbContextOptions<AppDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                _connection = new SqliteConnection("DataSource=:memory:");
                _connection.Open();

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });

                var serviceProvider = services.BuildServiceProvider();

                using var scope = serviceProvider.CreateScope();

                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                db.Database.EnsureCreated();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _connection?.Dispose();
            }
        }
    }

}